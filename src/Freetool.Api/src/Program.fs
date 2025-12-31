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
open Freetool.Domain.Entities
open Freetool.Api.Tracing
open Freetool.Api.Middleware
open Freetool.Api.OpenApi

/// Creates a new OpenFGA store and saves the ID to the database
let private createAndSaveNewStore (connectionString: string) (apiUrl: string) : string =
    let tempService = OpenFgaService(apiUrl)
    let authService = tempService :> IAuthorizationService

    eprintfn "Creating new OpenFGA store..."
    let storeTask = authService.CreateStoreAsync({ Name = "freetool-authorization" })
    storeTask.Wait()
    let newStoreId = storeTask.Result.Id
    eprintfn "Created new OpenFGA store with ID: %s" newStoreId

    // Save to database for future restarts
    SettingsStore.set connectionString "OpenFGA:StoreId" newStoreId
    eprintfn "Saved OpenFGA store ID to database"

    newStoreId

/// Checks if a store exists in OpenFGA
let private storeExists (apiUrl: string) (storeId: string) : bool =
    let tempService = OpenFgaService(apiUrl)
    let authService = tempService :> IAuthorizationService
    let existsTask = authService.StoreExistsAsync(storeId)
    existsTask.Wait()
    existsTask.Result

/// Ensures an OpenFGA store exists, creating one if necessary
/// Persists the store ID to the database to survive restarts
/// Returns the store ID to use for the application
let ensureOpenFgaStore (connectionString: string) (apiUrl: string) (configuredStoreId: string) : string =
    // First, check if we have a store ID saved in the database
    let dbStoreId = SettingsStore.get connectionString "OpenFGA:StoreId"

    match dbStoreId with
    | Some storeId when not (System.String.IsNullOrEmpty(storeId)) ->
        // We have a store ID in the database, verify it exists in OpenFGA
        eprintfn "Found OpenFGA store ID in database: %s" storeId

        if storeExists apiUrl storeId then
            eprintfn "OpenFGA store %s exists, using it." storeId
            storeId
        else
            // Store was deleted from OpenFGA, create a new one
            eprintfn "OpenFGA store %s no longer exists. Creating new store..." storeId
            createAndSaveNewStore connectionString apiUrl
    | _ ->
        // No store ID in database, check config
        if System.String.IsNullOrEmpty(configuredStoreId) then
            // No store configured anywhere, create a new one
            eprintfn "No OpenFGA store ID configured. Creating new store..."
            createAndSaveNewStore connectionString apiUrl
        else
            // Check if configured store exists
            eprintfn "Checking if configured OpenFGA store %s exists..." configuredStoreId

            if storeExists apiUrl configuredStoreId then
                eprintfn "OpenFGA store %s exists, using it and saving to database." configuredStoreId
                // Save the config store ID to database for future restarts
                SettingsStore.set connectionString "OpenFGA:StoreId" configuredStoreId
                configuredStoreId
            else
                // Configured store doesn't exist, create a new one
                eprintfn "OpenFGA store %s does not exist. Creating new store..." configuredStoreId
                createAndSaveNewStore connectionString apiUrl

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    // Run database migrations early (before OpenFGA store check)
    // This ensures the Settings table exists for storing the store ID
    let connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection")

    Persistence.upgradeDatabase connectionString

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

    builder.Services.AddScoped<ISpaceRepository>(fun serviceProvider ->
        let context = serviceProvider.GetRequiredService<FreetoolDbContext>()
        let eventRepository = serviceProvider.GetRequiredService<IEventRepository>()
        SpaceRepository(context, eventRepository))
    |> ignore

    builder.Services.AddScoped<IResourceRepository, ResourceRepository>() |> ignore
    builder.Services.AddScoped<IFolderRepository, FolderRepository>() |> ignore
    builder.Services.AddScoped<IAppRepository, AppRepository>() |> ignore
    builder.Services.AddScoped<IRunRepository, RunRepository>() |> ignore
    builder.Services.AddScoped<IEventRepository, EventRepository>() |> ignore
    builder.Services.AddScoped<IEventPublisher, EventPublisher>() |> ignore

    // Ensure OpenFGA store exists before registering the service
    // The store ID is persisted to the database to survive restarts
    let openFgaApiUrl = builder.Configuration["OpenFGA:ApiUrl"]
    let configuredStoreId = builder.Configuration["OpenFGA:StoreId"]

    let actualStoreId =
        try
            ensureOpenFgaStore connectionString openFgaApiUrl configuredStoreId
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
        let folderRepository = serviceProvider.GetRequiredService<IFolderRepository>()
        let resourceRepository = serviceProvider.GetRequiredService<IResourceRepository>()
        let spaceRepository = serviceProvider.GetRequiredService<ISpaceRepository>()

        EventEnhancementService(userRepository, appRepository, folderRepository, resourceRepository, spaceRepository)
        :> IEventEnhancementService)
    |> ignore

    builder.Services.AddScoped<UserHandler>() |> ignore

    builder.Services.AddScoped<SpaceHandler>(fun serviceProvider ->
        let spaceRepository = serviceProvider.GetRequiredService<ISpaceRepository>()
        let userRepository = serviceProvider.GetRequiredService<IUserRepository>()
        SpaceHandler(spaceRepository, userRepository))
    |> ignore

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

    builder.Services.AddScoped<IMultiRepositoryCommandHandler<SpaceCommand, SpaceCommandResult>>(fun serviceProvider ->
        let spaceHandler = serviceProvider.GetRequiredService<SpaceHandler>()
        let activitySource = serviceProvider.GetRequiredService<ActivitySource>()
        AutoTracing.createMultiRepositoryTracingDecorator "space" spaceHandler activitySource)
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

    builder.Services.AddScoped<TrashHandler>(fun serviceProvider ->
        let appRepository = serviceProvider.GetRequiredService<IAppRepository>()
        let folderRepository = serviceProvider.GetRequiredService<IFolderRepository>()
        let resourceRepository = serviceProvider.GetRequiredService<IResourceRepository>()
        TrashHandler(appRepository, folderRepository, resourceRepository))
    |> ignore

    builder.Services.AddScoped<IMultiRepositoryCommandHandler<TrashCommand, TrashCommandResult>>(fun serviceProvider ->
        let trashHandler = serviceProvider.GetRequiredService<TrashHandler>()
        let activitySource = serviceProvider.GetRequiredService<ActivitySource>()
        AutoTracing.createMultiRepositoryTracingDecorator "trash" trashHandler activitySource)
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

    // Note: Database migrations were already run at startup (before OpenFGA store check)

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

            // Set up organization relations for all existing spaces
            // This ensures org admins inherit permissions on all spaces
            try
                eprintfn "Setting up organization relations for existing spaces..."

                let spaceRepository = scope.ServiceProvider.GetRequiredService<ISpaceRepository>()

                let spacesTask = spaceRepository.GetAllAsync 0 1000
                spacesTask.Wait()
                let spaces = spacesTask.Result

                for space in spaces do
                    let spaceId = Space.getId space
                    let spaceIdStr = spaceId.Value.ToString()

                    let tuple =
                        { Subject = Organization "default"
                          Relation = SpaceOrganization
                          Object = SpaceObject spaceIdStr }

                    let relationTask = authService.CreateRelationshipsAsync([ tuple ])
                    relationTask.Wait()

                eprintfn "Organization relations set up for %d spaces" (List.length spaces)
            with ex ->
                eprintfn "Warning: Could not set up organization relations for spaces: %s" ex.Message
                eprintfn "Org admins may not have permissions on existing spaces."

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
