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
open Freetool.Api
open Freetool.Api.Tracing
open Freetool.Api.Middleware
open Freetool.Api.OpenApi
open Freetool.Api.Services

/// Validates that all EventType cases can be serialized and deserialized consistently.
/// This catches bugs where a new event type is added to the DU but not to fromString.
let private validateEventTypeRegistry () =
    // List all known event types that should be registered
    let allEventTypes =
        [ // User events
          EventType.UserEvents UserCreatedEvent
          EventType.UserEvents UserUpdatedEvent
          EventType.UserEvents UserDeletedEvent
          EventType.UserEvents UserInvitedEvent
          EventType.UserEvents UserActivatedEvent
          // App events
          EventType.AppEvents AppCreatedEvent
          EventType.AppEvents AppUpdatedEvent
          EventType.AppEvents AppDeletedEvent
          EventType.AppEvents AppRestoredEvent
          // Resource events
          EventType.ResourceEvents ResourceCreatedEvent
          EventType.ResourceEvents ResourceUpdatedEvent
          EventType.ResourceEvents ResourceDeletedEvent
          EventType.ResourceEvents ResourceRestoredEvent
          // Folder events
          EventType.FolderEvents FolderCreatedEvent
          EventType.FolderEvents FolderUpdatedEvent
          EventType.FolderEvents FolderDeletedEvent
          EventType.FolderEvents FolderRestoredEvent
          // Run events
          EventType.RunEvents RunCreatedEvent
          EventType.RunEvents RunStatusChangedEvent
          // Space events
          EventType.SpaceEvents SpaceCreatedEvent
          EventType.SpaceEvents SpaceUpdatedEvent
          EventType.SpaceEvents SpaceDeletedEvent ]

    let mutable hasErrors = false

    for eventType in allEventTypes do
        let serialized = EventTypeConverter.toString eventType
        let deserialized = EventTypeConverter.fromString serialized

        match deserialized with
        | None ->
            eprintfn
                "ERROR: EventTypeConverter.fromString cannot parse '%s' (from %A). Add it to EventTypeConverter.fromString."
                serialized
                eventType

            hasErrors <- true
        | Some parsed when parsed <> eventType ->
            eprintfn
                "ERROR: EventTypeConverter round-trip failed for %A: serialized='%s', deserialized=%A"
                eventType
                serialized
                parsed

            hasErrors <- true
        | Some _ -> ()

    if hasErrors then
        failwith "EventType registry validation failed. See errors above."
    else
        eprintfn "EventType registry validation passed (%d event types)." allEventTypes.Length

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
    SettingsStore.set connectionString ConfigurationKeys.OpenFGA.StoreId newStoreId
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
    let dbStoreId = SettingsStore.get connectionString ConfigurationKeys.OpenFGA.StoreId

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
                SettingsStore.set connectionString ConfigurationKeys.OpenFGA.StoreId configuredStoreId
                configuredStoreId
            else
                // Configured store doesn't exist, create a new one
                eprintfn "OpenFGA store %s does not exist. Creating new store..." configuredStoreId
                createAndSaveNewStore connectionString apiUrl

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    // Detect dev mode from environment variable
    let isDevMode =
        System.Environment.GetEnvironmentVariable(ConfigurationKeys.Environment.DevMode) = "true"

    if isDevMode then
        eprintfn "[DEV MODE] Running in development mode with user impersonation"

    // Add CORS for dev mode (allows frontend on different port)
    if isDevMode then
        builder.Services.AddCors(fun options ->
            options.AddPolicy(
                "DevCors",
                fun policy -> policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader() |> ignore
            ))
        |> ignore

    // Run database migrations early (before OpenFGA store check)
    // This ensures the Settings table exists for storing the store ID
    let connectionString =
        builder.Configuration.GetConnectionString(ConfigurationKeys.DefaultConnection)

    Persistence.upgradeDatabase connectionString

    // Validate that all EventType cases are properly registered
    validateEventTypeRegistry ()

    // Add services to the container
    builder.Services
        .AddControllers(fun options -> options.SuppressAsyncSuffixInActionNames <- false)
        .ConfigureApiBehaviorOptions(fun options -> options.SuppressModelStateInvalidFilter <- false)
        .AddJsonOptions(fun options ->
            options.JsonSerializerOptions.DefaultIgnoreCondition <- JsonIgnoreCondition.WhenWritingNull
            options.JsonSerializerOptions.PropertyNamingPolicy <- System.Text.Json.JsonNamingPolicy.CamelCase
            options.JsonSerializerOptions.Converters.Add(HttpMethodConverter())
            options.JsonSerializerOptions.Converters.Add(EventTypeConverter())
            options.JsonSerializerOptions.Converters.Add(EntityTypeConverter())
            options.JsonSerializerOptions.Converters.Add(KeyValuePairConverter())
            options.JsonSerializerOptions.Converters.Add(FolderLocationConverter())

            // allowOverride = true lets type-level [<JsonFSharpConverter>] attributes take precedence
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
        options.UseSqlite(builder.Configuration.GetConnectionString ConfigurationKeys.DefaultConnection)
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
    let openFgaApiUrl = builder.Configuration[ConfigurationKeys.OpenFGA.ApiUrl]
    let configuredStoreId = builder.Configuration[ConfigurationKeys.OpenFGA.StoreId]

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
        let authService = serviceProvider.GetRequiredService<IAuthorizationService>()
        SpaceHandler(spaceRepository, userRepository, authService))
    |> ignore

    builder.Services.AddScoped<ResourceHandler>(fun serviceProvider ->
        let resourceRepository = serviceProvider.GetRequiredService<IResourceRepository>()
        let appRepository = serviceProvider.GetRequiredService<IAppRepository>()
        ResourceHandler(resourceRepository, appRepository))
    |> ignore

    builder.Services.AddScoped<FolderHandler>() |> ignore
    builder.Services.AddScoped<AppHandler>() |> ignore

    builder.Services.AddScoped<ICommandHandler<UserCommand, UserCommandResult>>(fun serviceProvider ->
        let userHandler = serviceProvider.GetRequiredService<UserHandler>()
        let activitySource = serviceProvider.GetRequiredService<ActivitySource>()
        AutoTracing.createTracingDecorator "user" userHandler activitySource)
    |> ignore

    builder.Services.AddScoped<ICommandHandler<ResourceCommand, ResourceCommandResult>>(fun serviceProvider ->
        let resourceHandler = serviceProvider.GetRequiredService<ResourceHandler>()
        let activitySource = serviceProvider.GetRequiredService<ActivitySource>()
        AutoTracing.createTracingDecorator "resource" resourceHandler activitySource)
    |> ignore

    builder.Services.AddScoped<ICommandHandler<SpaceCommand, SpaceCommandResult>>(fun serviceProvider ->
        let spaceHandler = serviceProvider.GetRequiredService<SpaceHandler>()
        let activitySource = serviceProvider.GetRequiredService<ActivitySource>()
        AutoTracing.createTracingDecorator "space" spaceHandler activitySource)
    |> ignore

    builder.Services.AddScoped<ICommandHandler<FolderCommand, FolderCommandResult>>(fun serviceProvider ->
        let folderHandler = serviceProvider.GetRequiredService<FolderHandler>()
        let activitySource = serviceProvider.GetRequiredService<ActivitySource>()
        AutoTracing.createTracingDecorator "folder" folderHandler activitySource)
    |> ignore

    builder.Services.AddScoped<ICommandHandler<AppCommand, AppCommandResult>>(fun serviceProvider ->
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

    builder.Services.AddScoped<ICommandHandler<TrashCommand, TrashCommandResult>>(fun serviceProvider ->
        let trashHandler = serviceProvider.GetRequiredService<TrashHandler>()
        let activitySource = serviceProvider.GetRequiredService<ActivitySource>()
        AutoTracing.createTracingDecorator "trash" trashHandler activitySource)
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
                    let endpoint = builder.Configuration[ConfigurationKeys.Environment.OtlpEndpoint]

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
            let orgAdminEmail = builder.Configuration[ConfigurationKeys.OpenFGA.OrgAdminEmail]

            if not (System.String.IsNullOrEmpty(orgAdminEmail)) then
                eprintfn "Organization admin email configured: %s (will be set when user first logs in)" orgAdminEmail

            // Run dev seeding after OpenFGA is initialized (only in dev mode)
            if isDevMode then
                try
                    let userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>()
                    let spaceRepository = scope.ServiceProvider.GetRequiredService<ISpaceRepository>()

                    let resourceRepository =
                        scope.ServiceProvider.GetRequiredService<IResourceRepository>()

                    let folderRepository = scope.ServiceProvider.GetRequiredService<IFolderRepository>()
                    let appRepository = scope.ServiceProvider.GetRequiredService<IAppRepository>()

                    // First, seed database data if needed (only runs if database is empty)
                    let seedTask =
                        DevSeedingService.seedDataAsync
                            userRepository
                            spaceRepository
                            resourceRepository
                            folderRepository
                            appRepository
                            authService

                    seedTask |> Async.AwaitTask |> Async.RunSynchronously

                    // Then, ensure OpenFGA relationships exist (always runs in dev mode)
                    // This handles the case where OpenFGA store was recreated but database still has users
                    let ensureRelationshipsTask =
                        DevSeedingService.ensureOpenFgaRelationshipsAsync userRepository spaceRepository authService

                    ensureRelationshipsTask |> Async.AwaitTask |> Async.RunSynchronously
                with ex ->
                    eprintfn "[DEV MODE] Warning: Failed to seed dev data: %s" ex.Message
        with ex ->
            eprintfn "Warning: Could not initialize OpenFGA authorization model: %s" ex.Message
            eprintfn "The application will continue, but authorization checks may fail."
            eprintfn "You can manually initialize by calling POST /admin/openfga/write-model"

    app.UseSwagger() |> ignore
    app.UseSwaggerUI() |> ignore

    app.UseHttpsRedirection() |> ignore

    // Enable CORS for dev mode
    if isDevMode then
        app.UseCors("DevCors") |> ignore

    let provider = FileExtensionContentTypeProvider()
    provider.Mappings[".js"] <- "application/javascript"
    let staticFileOptions = StaticFileOptions()
    staticFileOptions.ContentTypeProvider <- provider
    app.UseStaticFiles(staticFileOptions) |> ignore

    // Use DevAuthMiddleware in dev mode, TailscaleAuthMiddleware otherwise
    if isDevMode then
        app.UseMiddleware<DevAuthMiddleware>() |> ignore
    else
        app.UseMiddleware<TailscaleAuthMiddleware>() |> ignore

    app.MapControllers() |> ignore

    app.MapFallbackToFile("index.html") |> ignore

    app.Run()
    0
