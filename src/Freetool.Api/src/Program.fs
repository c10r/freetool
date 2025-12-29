open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.EntityFrameworkCore
open Microsoft.AspNetCore.StaticFiles
open System.Text.Json.Serialization
open System.Diagnostics
open OpenTelemetry.Resources
open OpenTelemetry.Trace
open OpenTelemetry.Exporter
open Freetool.Infrastructure.Database
open Freetool.Infrastructure.Database.Repositories
open Freetool.Infrastructure.Services
open Freetool.Application.Interfaces
open Freetool.Application.Handlers
open Freetool.Application.Commands
open Freetool.Application.DTOs
open Freetool.Application.Services
open Freetool.Domain.ValueObjects
open Freetool.Api.Tracing
open Freetool.Api.Middleware
open Freetool.Api.OpenApi

/// Ensures an OpenFGA store exists, creating one if necessary
/// Returns the store ID to use for the application
let ensureOpenFgaStore (apiUrl: string) (configuredStoreId: string) : string =
    // Create a temporary service without store ID to check/create stores
    let tempService = OpenFgaService(apiUrl)
    let authService = tempService :> IAuthorizationService

    if System.String.IsNullOrEmpty(configuredStoreId) then
        // No store ID configured, create a new store
        eprintfn "No OpenFGA store ID configured. Creating new store..."

        let storeTask = authService.CreateStoreAsync({ Name = "freetool-authorization" })

        storeTask.Wait()
        let newStoreId = storeTask.Result.Id
        eprintfn "Created new OpenFGA store with ID: %s" newStoreId
        newStoreId
    else
        // Check if configured store exists
        eprintfn "Checking if OpenFGA store %s exists..." configuredStoreId
        let existsTask = authService.StoreExistsAsync(configuredStoreId)
        existsTask.Wait()

        if existsTask.Result then
            eprintfn "OpenFGA store %s exists, using it." configuredStoreId
            configuredStoreId
        else
            // Store doesn't exist, create a new one
            eprintfn "OpenFGA store %s does not exist. Creating new store..." configuredStoreId

            let storeTask = authService.CreateStoreAsync({ Name = "freetool-authorization" })

            storeTask.Wait()
            let newStoreId = storeTask.Result.Id
            eprintfn "Created new OpenFGA store with ID: %s" newStoreId
            newStoreId

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    // Add services to the container
    builder.Services
        .AddControllers(fun options -> options.SuppressAsyncSuffixInActionNames <- false)
        .ConfigureApiBehaviorOptions(fun options -> options.SuppressModelStateInvalidFilter <- false)
        .AddJsonOptions(fun options ->
            options.JsonSerializerOptions.DefaultIgnoreCondition <- JsonIgnoreCondition.WhenWritingNull
            options.JsonSerializerOptions.Converters.Add(HttpMethodConverter())
            options.JsonSerializerOptions.Converters.Add(EventTypeConverter())
            options.JsonSerializerOptions.Converters.Add(EntityTypeConverter())
            options.JsonSerializerOptions.Converters.Add(KeyValuePairConverter())
            options.JsonSerializerOptions.Converters.Add(FolderLocationConverter())

            // allowOverride = true lets property-level [<JsonConverter>] attributes take precedence
            options.JsonSerializerOptions.Converters.Add(JsonFSharpConverter(allowOverride = true)))
    |> ignore

    builder.Services.AddEndpointsApiExplorer() |> ignore

    builder.Services.AddSwaggerGen(fun c ->
        c.SupportNonNullableReferenceTypes()
        c.UseAllOfToExtendReferenceSchemas()

        c.MapType<FolderLocation>(fun () -> Microsoft.OpenApi.Models.OpenApiSchema(Type = "string", Nullable = true))

        c.SchemaFilter<FSharpUnionSchemaFilter>()
        c.OperationFilter<FSharpQueryParameterOperationFilter>())
    |> ignore

    builder.Services.AddDbContext<FreetoolDbContext>(fun options ->
        options.UseSqlite(builder.Configuration.GetConnectionString "DefaultConnection")
        |> ignore)
    |> ignore

    builder.Services.AddScoped<IUserRepository>(fun serviceProvider ->
        let context = serviceProvider.GetRequiredService<FreetoolDbContext>()
        let eventRepository = serviceProvider.GetRequiredService<IEventRepository>()
        UserRepository(context, eventRepository))
    |> ignore

    builder.Services.AddScoped<IGroupRepository>(fun serviceProvider ->
        let context = serviceProvider.GetRequiredService<FreetoolDbContext>()
        let eventRepository = serviceProvider.GetRequiredService<IEventRepository>()
        GroupRepository(context, eventRepository))
    |> ignore

    builder.Services.AddScoped<IWorkspaceRepository>(fun serviceProvider ->
        let context = serviceProvider.GetRequiredService<FreetoolDbContext>()
        let eventRepository = serviceProvider.GetRequiredService<IEventRepository>()
        WorkspaceRepository(context, eventRepository))
    |> ignore

    builder.Services.AddScoped<IResourceRepository, ResourceRepository>() |> ignore
    builder.Services.AddScoped<IFolderRepository, FolderRepository>() |> ignore
    builder.Services.AddScoped<IAppRepository, AppRepository>() |> ignore
    builder.Services.AddScoped<IRunRepository, RunRepository>() |> ignore
    builder.Services.AddScoped<IEventRepository, EventRepository>() |> ignore
    builder.Services.AddScoped<IEventPublisher, EventPublisher>() |> ignore

    // Ensure OpenFGA store exists before registering the service
    let openFgaApiUrl = builder.Configuration["OpenFGA:ApiUrl"]
    let configuredStoreId = builder.Configuration["OpenFGA:StoreId"]

    let actualStoreId =
        try
            ensureOpenFgaStore openFgaApiUrl configuredStoreId
        with ex ->
            eprintfn "Warning: Could not ensure OpenFGA store exists: %s" ex.Message
            eprintfn "Using configured store ID (if any). Authorization may fail."
            configuredStoreId

    builder.Services.AddScoped<IAuthorizationService>(fun _ ->
        // Always create with the actual store ID (which may have been created)
        if System.String.IsNullOrEmpty(actualStoreId) then
            OpenFgaService(openFgaApiUrl) :> IAuthorizationService
        else
            OpenFgaService(openFgaApiUrl, actualStoreId) :> IAuthorizationService)
    |> ignore

    builder.Services.AddScoped<IEventEnhancementService>(fun serviceProvider ->
        let userRepository = serviceProvider.GetRequiredService<IUserRepository>()
        let appRepository = serviceProvider.GetRequiredService<IAppRepository>()
        let groupRepository = serviceProvider.GetRequiredService<IGroupRepository>()
        let folderRepository = serviceProvider.GetRequiredService<IFolderRepository>()
        let resourceRepository = serviceProvider.GetRequiredService<IResourceRepository>()

        EventEnhancementService(userRepository, appRepository, groupRepository, folderRepository, resourceRepository)
        :> IEventEnhancementService)
    |> ignore

    builder.Services.AddScoped<UserHandler>() |> ignore

    builder.Services.AddScoped<GroupHandler>() |> ignore

    builder.Services.AddScoped<WorkspaceHandler>() |> ignore

    builder.Services.AddScoped<ResourceHandler>(fun serviceProvider ->
        let resourceRepository = serviceProvider.GetRequiredService<IResourceRepository>()
        let appRepository = serviceProvider.GetRequiredService<IAppRepository>()
        ResourceHandler(resourceRepository, appRepository))
    |> ignore

    builder.Services.AddScoped<FolderHandler>() |> ignore
    builder.Services.AddScoped<AppHandler>() |> ignore

    builder.Services.AddScoped<ICommandHandler>(fun serviceProvider ->
        let userHandler = serviceProvider.GetRequiredService<UserHandler>()
        let activitySource = serviceProvider.GetRequiredService<ActivitySource>()
        TracingUserCommandHandlerDecorator(userHandler, activitySource))
    |> ignore

    builder.Services.AddScoped<IMultiRepositoryCommandHandler<ResourceCommand, ResourceCommandResult>>
        (fun serviceProvider ->
            let resourceHandler = serviceProvider.GetRequiredService<ResourceHandler>()
            let activitySource = serviceProvider.GetRequiredService<ActivitySource>()
            AutoTracing.createMultiRepositoryTracingDecorator "resource" resourceHandler activitySource)
    |> ignore

    builder.Services.AddScoped<IMultiRepositoryCommandHandler<GroupCommand, GroupCommandResult>>(fun serviceProvider ->
        let groupHandler = serviceProvider.GetRequiredService<GroupHandler>()
        let activitySource = serviceProvider.GetRequiredService<ActivitySource>()
        AutoTracing.createMultiRepositoryTracingDecorator "group" groupHandler activitySource)
    |> ignore

    builder.Services.AddScoped<IMultiRepositoryCommandHandler<WorkspaceCommand, WorkspaceCommandResult>>
        (fun serviceProvider ->
            let workspaceHandler = serviceProvider.GetRequiredService<WorkspaceHandler>()
            let activitySource = serviceProvider.GetRequiredService<ActivitySource>()
            AutoTracing.createMultiRepositoryTracingDecorator "workspace" workspaceHandler activitySource)
    |> ignore

    builder.Services.AddScoped<IGenericCommandHandler<IFolderRepository, FolderCommand, FolderCommandResult>>
        (fun serviceProvider ->
            let folderHandler = serviceProvider.GetRequiredService<FolderHandler>()
            let activitySource = serviceProvider.GetRequiredService<ActivitySource>()
            AutoTracing.createTracingDecorator "folder" folderHandler activitySource)
    |> ignore

    builder.Services.AddScoped<IGenericCommandHandler<IAppRepository, AppCommand, AppCommandResult>>
        (fun serviceProvider ->
            let appHandler = serviceProvider.GetRequiredService<AppHandler>()
            let activitySource = serviceProvider.GetRequiredService<ActivitySource>()
            AutoTracing.createTracingDecorator "app" appHandler activitySource)
    |> ignore

    // Configure OpenTelemetry
    let activitySource = new ActivitySource("Freetool.Api")
    builder.Services.AddSingleton<ActivitySource>(activitySource) |> ignore

    builder.Services
        .AddOpenTelemetry()
        .WithTracing(fun tracing ->
            tracing
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("freetool-api", "1.0.0"))
                .AddSource("Freetool.Api")
                .AddAspNetCoreInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter(fun options ->
                    let endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]

                    if not (System.String.IsNullOrEmpty(endpoint)) then
                        options.Endpoint <- System.Uri(endpoint)
                        options.Protocol <- OtlpExportProtocol.Grpc
                    else
                        eprintfn "No OTLP endpoint configured, using default")
            |> ignore)
    |> ignore

    let app = builder.Build()

    // Debug logging for paths
    eprintfn "Content root: %s" builder.Environment.ContentRootPath
    eprintfn "Web root: %s" builder.Environment.WebRootPath
    eprintfn "Current directory: %s" (System.IO.Directory.GetCurrentDirectory())

    let connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection")

    Persistence.upgradeDatabase connectionString

    // Initialize OpenFGA authorization model if we have a valid store
    // Note: actualStoreId was set during DI registration (store was created if needed)
    if not (System.String.IsNullOrEmpty(actualStoreId)) then
        try
            eprintfn "Initializing OpenFGA authorization model..."
            use scope = app.Services.CreateScope()
            let authService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>()
            let modelTask = authService.WriteAuthorizationModelAsync()
            modelTask.Wait()
            eprintfn "OpenFGA authorization model initialized successfully"

            // Set up organization relations for all existing workspaces
            // This ensures org admins inherit permissions on all workspaces
            try
                eprintfn "Setting up organization relations for existing workspaces..."

                let workspaceRepository =
                    scope.ServiceProvider.GetRequiredService<IWorkspaceRepository>()

                let workspacesTask = workspaceRepository.GetAllAsync 0 1000
                workspacesTask.Wait()
                let workspaces = workspacesTask.Result

                for workspace in workspaces do
                    let workspaceId = workspace.State.Id.Value.ToString()

                    let tuple =
                        { Subject = Organization "default"
                          Relation = WorkspaceOrganization
                          Object = WorkspaceObject workspaceId }

                    let relationTask = authService.CreateRelationshipsAsync([ tuple ])
                    relationTask.Wait()

                eprintfn "Organization relations set up for %d workspaces" (List.length workspaces)
            with ex ->
                eprintfn "Warning: Could not set up organization relations for workspaces: %s" ex.Message
                eprintfn "Org admins may not have permissions on existing workspaces."

            // Note: Organization admin is now set automatically when the user first logs in
            // via TailscaleAuthMiddleware if their email matches OpenFGA:OrgAdminEmail config
            let orgAdminEmail = builder.Configuration["OpenFGA:OrgAdminEmail"]

            if not (System.String.IsNullOrEmpty(orgAdminEmail)) then
                eprintfn "Organization admin email configured: %s (will be set when user first logs in)" orgAdminEmail
        with ex ->
            eprintfn "Warning: Could not initialize OpenFGA authorization model: %s" ex.Message
            eprintfn "The application will continue, but authorization checks may fail."
            eprintfn "You can manually initialize by calling POST /admin/openfga/write-model"

    app.UseSwagger() |> ignore
    app.UseSwaggerUI() |> ignore

    app.UseHttpsRedirection() |> ignore

    let provider = FileExtensionContentTypeProvider()
    provider.Mappings[".js"] <- "application/javascript"
    let staticFileOptions = StaticFileOptions()
    staticFileOptions.ContentTypeProvider <- provider
    app.UseStaticFiles(staticFileOptions) |> ignore

    app.UseMiddleware<TailscaleAuthMiddleware>() |> ignore

    app.MapControllers() |> ignore

    app.MapFallbackToFile("index.html") |> ignore

    app.Run()
    0
