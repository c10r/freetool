namespace Freetool.Api.Middleware

open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open System.Threading.Tasks
open System.Diagnostics
open Freetool.Application.Interfaces
open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
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
                return ()
            | Some userEmail ->
                match Email.Create(Some userEmail) with
                | Error _ ->
                    Tracing.addAttribute currentActivity "tailscale.auth.error" "invalid_email_format"
                    Tracing.addAttribute currentActivity "tailscale.auth.user_email" userEmail
                    Tracing.setSpanStatus currentActivity false (Some "Invalid email format in Tailscale header")
                    context.Response.StatusCode <- 401
                    do! context.Response.WriteAsync $"Unauthorized: Invalid {TAILSCALE_USER_LOGIN}"
                    return ()

                | Ok validEmail ->
                    let userRepository = context.RequestServices.GetRequiredService<IUserRepository>()
                    let! user = userRepository.GetByEmailAsync validEmail

                    match user with
                    | None ->
                        // Auto-create new user on first login
                        let userNameOption = extractHeader TAILSCALE_USER_NAME context
                        let profilePicOption = extractHeader TAILSCALE_USER_PROFILE context
                        let userName = userNameOption |> Option.defaultValue userEmail

                        let newUser = User.create userName validEmail profilePicOption

                        match! userRepository.AddAsync newUser with
                        | Error err ->
                            Tracing.addAttribute currentActivity "tailscale.auth.error" "user_creation_failed"
                            Tracing.addAttribute currentActivity "tailscale.auth.user_email" userEmail
                            Tracing.setSpanStatus currentActivity false (Some "Failed to create user")
                            context.Response.StatusCode <- 500

                            let errorMessage =
                                match err with
                                | ValidationError msg -> $"Validation error: {msg}"
                                | NotFound msg -> $"Not found: {msg}"
                                | Conflict msg -> $"Conflict: {msg}"
                                | InvalidOperation msg -> $"Invalid operation: {msg}"

                            do!
                                context.Response.WriteAsync
                                    $"Internal Server Error: Failed to create user - {errorMessage}"

                            return ()
                        | Ok() ->
                            context.Items.["UserId"] <- newUser.State.Id
                            Tracing.addAttribute currentActivity "tailscale.auth.user_created" "true"
                            Tracing.addAttribute currentActivity "tailscale.auth.user_email" userEmail
                            Tracing.addAttribute currentActivity "user.id" (newUser.State.Id.Value.ToString())

                            // Check if this user should be made org admin
                            let configuration = context.RequestServices.GetRequiredService<IConfiguration>()
                            let orgAdminEmail = configuration["OpenFGA:OrgAdminEmail"]

                            if
                                not (System.String.IsNullOrEmpty(orgAdminEmail))
                                && userEmail.Equals(orgAdminEmail, System.StringComparison.OrdinalIgnoreCase)
                            then
                                let authService =
                                    context.RequestServices.GetRequiredService<IAuthorizationService>()

                                let userId = newUser.State.Id.ToString()

                                try
                                    do! authService.InitializeOrganizationAsync "default" userId
                                with ex ->
                                    logger.LogWarning(
                                        "Failed to set user {Email} as org admin: {Error}",
                                        userEmail,
                                        ex.Message
                                    )

                            Tracing.addAttribute currentActivity "tailscale.auth.success" "true"
                            Tracing.setSpanStatus currentActivity true None
                            do! next.Invoke context

                    | Some existingUser when User.isInvitedPlaceholder existingUser ->
                        // Activate the invited placeholder user
                        let userNameOption = extractHeader TAILSCALE_USER_NAME context
                        let profilePicOption = extractHeader TAILSCALE_USER_PROFILE context
                        let userName = userNameOption |> Option.defaultValue userEmail

                        match User.activate userName profilePicOption existingUser with
                        | Error err ->
                            Tracing.addAttribute currentActivity "tailscale.auth.error" "user_activation_failed"
                            Tracing.addAttribute currentActivity "tailscale.auth.user_email" userEmail
                            Tracing.setSpanStatus currentActivity false (Some "Failed to activate user")
                            context.Response.StatusCode <- 500

                            let errorMessage =
                                match err with
                                | ValidationError msg -> $"Validation error: {msg}"
                                | NotFound msg -> $"Not found: {msg}"
                                | Conflict msg -> $"Conflict: {msg}"
                                | InvalidOperation msg -> $"Invalid operation: {msg}"

                            do!
                                context.Response.WriteAsync
                                    $"Internal Server Error: Failed to activate user - {errorMessage}"

                            return ()
                        | Ok activatedUser ->
                            match! userRepository.UpdateAsync activatedUser with
                            | Error err ->
                                Tracing.addAttribute
                                    currentActivity
                                    "tailscale.auth.error"
                                    "user_activation_save_failed"

                                Tracing.addAttribute currentActivity "tailscale.auth.user_email" userEmail
                                Tracing.setSpanStatus currentActivity false (Some "Failed to save activated user")
                                context.Response.StatusCode <- 500

                                let errorMessage =
                                    match err with
                                    | ValidationError msg -> $"Validation error: {msg}"
                                    | NotFound msg -> $"Not found: {msg}"
                                    | Conflict msg -> $"Conflict: {msg}"
                                    | InvalidOperation msg -> $"Invalid operation: {msg}"

                                do!
                                    context.Response.WriteAsync
                                        $"Internal Server Error: Failed to save activated user - {errorMessage}"

                                return ()
                            | Ok() ->
                                context.Items.["UserId"] <- activatedUser.State.Id
                                Tracing.addAttribute currentActivity "tailscale.auth.user_activated" "true"
                                Tracing.addAttribute currentActivity "tailscale.auth.user_email" userEmail
                                Tracing.addAttribute currentActivity "user.id" (activatedUser.State.Id.Value.ToString())

                                // Check if this user should be made org admin (same as new user flow)
                                let configuration = context.RequestServices.GetRequiredService<IConfiguration>()
                                let orgAdminEmail = configuration["OpenFGA:OrgAdminEmail"]

                                if
                                    not (System.String.IsNullOrEmpty(orgAdminEmail))
                                    && userEmail.Equals(orgAdminEmail, System.StringComparison.OrdinalIgnoreCase)
                                then
                                    let authService =
                                        context.RequestServices.GetRequiredService<IAuthorizationService>()

                                    let userId = activatedUser.State.Id.ToString()

                                    try
                                        do! authService.InitializeOrganizationAsync "default" userId
                                    with ex ->
                                        logger.LogWarning(
                                            "Failed to set user {Email} as org admin: {Error}",
                                            userEmail,
                                            ex.Message
                                        )

                                Tracing.addAttribute currentActivity "tailscale.auth.success" "true"
                                Tracing.setSpanStatus currentActivity true None
                                do! next.Invoke context

                    | Some existingUser ->
                        // Regular existing user - proceed normally
                        context.Items.["UserId"] <- existingUser.State.Id
                        Tracing.addAttribute currentActivity "tailscale.auth.user_email" userEmail
                        Tracing.addAttribute currentActivity "user.id" (existingUser.State.Id.Value.ToString())

                        // Ensure org admin tuple exists if email matches config
                        // This handles the case where store was recreated or user was created before org admin config
                        let configuration = context.RequestServices.GetRequiredService<IConfiguration>()
                        let orgAdminEmail = configuration["OpenFGA:OrgAdminEmail"]

                        if
                            not (System.String.IsNullOrEmpty(orgAdminEmail))
                            && userEmail.Equals(orgAdminEmail, System.StringComparison.OrdinalIgnoreCase)
                        then
                            let authService =
                                context.RequestServices.GetRequiredService<IAuthorizationService>()

                            let userId = existingUser.State.Id.ToString()

                            try
                                do! authService.InitializeOrganizationAsync "default" userId
                                Tracing.addAttribute currentActivity "tailscale.auth.org_admin_ensured" "true"
                            with ex ->
                                Tracing.addAttribute currentActivity "tailscale.auth.org_admin_error" ex.Message

                                logger.LogWarning(
                                    "Failed to ensure org admin for {Email}: {Error}",
                                    userEmail,
                                    ex.Message
                                )

                        Tracing.addAttribute currentActivity "tailscale.auth.success" "true"
                        Tracing.setSpanStatus currentActivity true None
                        do! next.Invoke context
        }
