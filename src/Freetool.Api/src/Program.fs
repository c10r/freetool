open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.EntityFrameworkCore
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
open Freetool.Api.Tracing
open Freetool.Api.Middleware
open Freetool.Api.OpenApi

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
            options.JsonSerializerOptions.Converters.Add(JsonFSharpConverter()))
    |> ignore

    builder.Services.AddEndpointsApiExplorer() |> ignore

    builder.Services.AddSwaggerGen(fun c ->
        c.SupportNonNullableReferenceTypes() |> ignore
        c.UseAllOfToExtendReferenceSchemas() |> ignore
        c.SchemaFilter<FSharpUnionSchemaFilter>() |> ignore)
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

    builder.Services.AddScoped<IResourceRepository, ResourceRepository>() |> ignore
    builder.Services.AddScoped<IFolderRepository, FolderRepository>() |> ignore
    builder.Services.AddScoped<IAppRepository, AppRepository>() |> ignore
    builder.Services.AddScoped<IRunRepository, RunRepository>() |> ignore
    builder.Services.AddScoped<IEventRepository, EventRepository>() |> ignore
    builder.Services.AddScoped<IEventPublisher, EventPublisher>() |> ignore

    builder.Services.AddScoped<UserHandler>() |> ignore

    builder.Services.AddScoped<GroupHandler>() |> ignore

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
                    let endpoint = builder.Configuration.["OTEL_EXPORTER_OTLP_ENDPOINT"]

                    if not (System.String.IsNullOrEmpty(endpoint)) then
                        options.Endpoint <- System.Uri(endpoint)
                        options.Protocol <- OtlpExportProtocol.Grpc
                    else
                        eprintfn "No OTLP endpoint configured, using default")
            |> ignore)
    |> ignore

    let app = builder.Build()

    let connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection")

    Persistence.upgradeDatabase connectionString

    app.UseSwagger() |> ignore
    app.UseSwaggerUI() |> ignore

    app.UseHttpsRedirection() |> ignore

    app.UseStaticFiles() |> ignore

    app.UseMiddleware<TailscaleAuthMiddleware>() |> ignore

    app.MapControllers() |> ignore

    app.MapFallbackToFile("index.html") |> ignore

    app.Run()
    0