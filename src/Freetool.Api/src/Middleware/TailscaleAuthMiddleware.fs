namespace Freetool.Api.Middleware

open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open System.Threading.Tasks
open System.Diagnostics
open Freetool.Api.Services
open Freetool.Api.Tracing

type TailscaleAuthMiddleware(next: RequestDelegate, logger: ILogger<TailscaleAuthMiddleware>) =

    let TAILSCALE_USER_LOGIN = "Tailscale-User-Login"
    let TAILSCALE_USER_NAME = "Tailscale-User-Name"
    let TAILSCALE_USER_PROFILE = "Tailscale-User-Profile-Pic"

    let extractHeader (headerKey: string) (context: HttpContext) : string option =
        match context.Request.Headers.TryGetValue headerKey with
        | true, values when values.Count > 0 ->
            let value = values.[0]

            if System.String.IsNullOrWhiteSpace value then
                None
            else
                Some value
        | _ -> None

    member _.InvokeAsync(context: HttpContext) : Task =
        task {
            let currentActivity = Option.ofObj Activity.Current

            let userEmailOption = extractHeader TAILSCALE_USER_LOGIN context

            match userEmailOption with
            | None ->
                Tracing.addAttribute currentActivity "tailscale.auth.error" "missing_or_empty_user_login"
                Tracing.addAttribute currentActivity "tailscale.auth.header" TAILSCALE_USER_LOGIN
                Tracing.setSpanStatus currentActivity false (Some "Missing or empty Tailscale user login")
                context.Response.StatusCode <- 401
                do! context.Response.WriteAsync $"Unauthorized: Missing or invalid {TAILSCALE_USER_LOGIN}"
            | Some userEmail ->
                let provisioningService =
                    context.RequestServices.GetRequiredService<IIdentityProvisioningService>()

                let userNameOption = extractHeader TAILSCALE_USER_NAME context
                let profilePicOption = extractHeader TAILSCALE_USER_PROFILE context

                let! result =
                    provisioningService.EnsureUserAsync
                        { Email = userEmail
                          Name = userNameOption
                          ProfilePicUrl = profilePicOption
                          GroupKeys = []
                          Source = "tailscale" }

                match result with
                | Error(InvalidEmailFormat errorMessage) ->
                    Tracing.addAttribute currentActivity "tailscale.auth.error" "invalid_email_format"
                    Tracing.addAttribute currentActivity "tailscale.auth.user_email" userEmail
                    Tracing.setSpanStatus currentActivity false (Some "Invalid email format in Tailscale header")
                    logger.LogWarning("Failed to provision tailscale user {Email}: {Error}", userEmail, errorMessage)
                    context.Response.StatusCode <- 401
                    do! context.Response.WriteAsync $"Unauthorized: Invalid {TAILSCALE_USER_LOGIN}"
                | Error(CreateUserFailed errorMessage) ->
                    Tracing.addAttribute currentActivity "tailscale.auth.error" "user_creation_failed"
                    Tracing.addAttribute currentActivity "tailscale.auth.user_email" userEmail
                    Tracing.setSpanStatus currentActivity false (Some "Failed to create user")
                    logger.LogWarning("Failed to provision tailscale user {Email}: {Error}", userEmail, errorMessage)
                    context.Response.StatusCode <- 500
                    do! context.Response.WriteAsync $"Internal Server Error: Failed to create user - {errorMessage}"
                | Error(ActivateUserFailed errorMessage) ->
                    Tracing.addAttribute currentActivity "tailscale.auth.error" "user_activation_failed"
                    Tracing.addAttribute currentActivity "tailscale.auth.user_email" userEmail
                    Tracing.setSpanStatus currentActivity false (Some "Failed to activate user")
                    logger.LogWarning("Failed to provision tailscale user {Email}: {Error}", userEmail, errorMessage)
                    context.Response.StatusCode <- 500
                    do! context.Response.WriteAsync $"Internal Server Error: Failed to activate user - {errorMessage}"
                | Error(SaveActivatedUserFailed errorMessage) ->
                    Tracing.addAttribute currentActivity "tailscale.auth.error" "user_activation_save_failed"
                    Tracing.addAttribute currentActivity "tailscale.auth.user_email" userEmail
                    Tracing.setSpanStatus currentActivity false (Some "Failed to save activated user")
                    logger.LogWarning("Failed to provision tailscale user {Email}: {Error}", userEmail, errorMessage)
                    context.Response.StatusCode <- 500

                    do!
                        context.Response.WriteAsync
                            $"Internal Server Error: Failed to save activated user - {errorMessage}"
                | Error(ProvisioningFailed errorMessage) ->
                    Tracing.addAttribute currentActivity "tailscale.auth.error" "provisioning_failed"
                    Tracing.addAttribute currentActivity "tailscale.auth.user_email" userEmail
                    Tracing.setSpanStatus currentActivity false (Some "Failed to provision user")
                    logger.LogWarning("Failed to provision tailscale user {Email}: {Error}", userEmail, errorMessage)
                    context.Response.StatusCode <- 500
                    do! context.Response.WriteAsync $"Internal Server Error: Failed to provision user - {errorMessage}"
                | Ok userId ->
                    context.Items.["UserId"] <- userId
                    Tracing.addAttribute currentActivity "tailscale.auth.user_email" userEmail
                    Tracing.addAttribute currentActivity "user.id" (userId.Value.ToString())
                    Tracing.addAttribute currentActivity "tailscale.auth.success" "true"
                    Tracing.setSpanStatus currentActivity true None
                    do! next.Invoke context
        }
