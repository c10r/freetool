open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.EntityFrameworkCore
open System.Text.Json.Serialization
open System.Diagnostics
open OpenTelemetry.Resources
open OpenTelemetry.Trace
open Freetool.Infrastructure.Database
open Freetool.Infrastructure.Database.Repositories
open Freetool.Application.Interfaces
open Freetool.Application.Handlers
open Freetool.Api.Tracing

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    // Add services to the container
    builder.Services
        .AddControllers(fun options -> options.SuppressAsyncSuffixInActionNames <- false)
        .ConfigureApiBehaviorOptions(fun options -> options.SuppressModelStateInvalidFilter <- false)
        .AddJsonOptions(fun options ->
            options.JsonSerializerOptions.DefaultIgnoreCondition <- JsonIgnoreCondition.WhenWritingNull
            options.JsonSerializerOptions.Converters.Add(JsonFSharpConverter()))
    |> ignore

    builder.Services.AddEndpointsApiExplorer() |> ignore

    builder.Services.AddSwaggerGen(fun c ->
        c.SupportNonNullableReferenceTypes() |> ignore
        c.UseAllOfToExtendReferenceSchemas() |> ignore)
    |> ignore

    builder.Services.AddDbContext<FreetoolDbContext>(fun options ->
        options.UseSqlite(builder.Configuration.GetConnectionString "DefaultConnection")
        |> ignore)
    |> ignore

    builder.Services.AddScoped<IUserRepository, UserRepository>() |> ignore

    builder.Services.AddScoped<UserHandler>() |> ignore

    builder.Services.AddScoped<ICommandHandler>(fun serviceProvider ->
        let userHandler = serviceProvider.GetRequiredService<UserHandler>()
        let activitySource = serviceProvider.GetRequiredService<ActivitySource>()
        TracingCommandHandlerDecorator(userHandler, activitySource))
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
    app.UseAuthorization() |> ignore
    app.MapControllers() |> ignore

    app.Run()
    0