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
        .ConfigureApiBehaviorOptions(fun options ->
            // Enable automatic model validation
            options.SuppressModelStateInvalidFilter <- false)
        .AddJsonOptions(fun options ->
            // Configure JSON serialization for F# option types
            options.JsonSerializerOptions.DefaultIgnoreCondition <- JsonIgnoreCondition.WhenWritingNull
            options.JsonSerializerOptions.Converters.Add(JsonFSharpConverter()))
    |> ignore

    builder.Services.AddEndpointsApiExplorer() |> ignore

    builder.Services.AddSwaggerGen(fun c ->
        c.SupportNonNullableReferenceTypes() |> ignore
        c.UseAllOfToExtendReferenceSchemas() |> ignore)
    |> ignore

    // Add Entity Framework
    builder.Services.AddDbContext<FreetoolDbContext>(fun options ->
        options.UseSqlite(builder.Configuration.GetConnectionString "DefaultConnection")
        |> ignore)
    |> ignore

    // Register repositories
    builder.Services.AddScoped<IUserRepository, UserRepository>() |> ignore

    // Register command handlers with tracing decorator
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
                .AddConsoleExporter()
            |> ignore)
    |> ignore

    // No use cases needed - using command handler pattern

    let app = builder.Build()

    // Run database migrations
    let connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection")

    Persistence.upgradeDatabase connectionString

    // Configure the HTTP request pipeline
    // Enable Swagger in all environments for this demo app
    app.UseSwagger() |> ignore
    app.UseSwaggerUI() |> ignore

    app.UseHttpsRedirection() |> ignore
    app.UseAuthorization() |> ignore
    app.MapControllers() |> ignore

    app.Run()
    0