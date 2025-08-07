namespace Freetool.Api.Middleware

open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open System.Threading.Tasks
open System.Diagnostics
open Freetool.Application.Interfaces
open Freetool.Domain.ValueObjects
open Freetool.Api.Tracing

type TailscaleAuthMiddleware(next: RequestDelegate) =

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

    member _.InvokeAsync(context: HttpContext) : Task = task {
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
            match Email.Create(Some userEmail) with
            | Error _ ->
                Tracing.addAttribute currentActivity "tailscale.auth.error" "invalid_email_format"
                Tracing.addAttribute currentActivity "tailscale.auth.user_email" userEmail
                Tracing.setSpanStatus currentActivity false (Some "Invalid email format in Tailscale header")
                context.Response.StatusCode <- 401
                do! context.Response.WriteAsync $"Unauthorized: Invalid {TAILSCALE_USER_LOGIN}"

            | Ok validEmail ->
                let userRepository = context.RequestServices.GetRequiredService<IUserRepository>()
                let! userExists = userRepository.ExistsByEmailAsync validEmail

                if not userExists then
                    // Auto-create new user on first login
                    let userNameOption = extractHeader TAILSCALE_USER_NAME context
                    let profilePicOption = extractHeader TAILSCALE_USER_PROFILE context
                    let userName = userNameOption |> Option.defaultValue userEmail

                    let newUser =
                        Freetool.Domain.Entities.User.create userName validEmail profilePicOption

                    match! userRepository.AddAsync newUser with
                    | Error err ->
                        Tracing.addAttribute currentActivity "tailscale.auth.error" "user_creation_failed"
                        Tracing.addAttribute currentActivity "tailscale.auth.user_email" userEmail
                        Tracing.setSpanStatus currentActivity false (Some "Failed to create user")
                        context.Response.StatusCode <- 500
                        do! context.Response.WriteAsync "Internal Server Error: Failed to create user"
                        return ()
                    | Ok _ ->
                        Tracing.addAttribute currentActivity "tailscale.auth.user_created" "true"
                        Tracing.addAttribute currentActivity "tailscale.auth.user_email" userEmail

                // User is authorized (either existing or newly created), add user info to context
                let userNameOption = extractHeader TAILSCALE_USER_NAME context
                let profilePicOption = extractHeader TAILSCALE_USER_PROFILE context

                context.Items.[TAILSCALE_USER_LOGIN] <- userEmail
                context.Items.[TAILSCALE_USER_NAME] <- (userNameOption |> Option.defaultValue "")
                context.Items.["TailscaleUserProfilePic"] <- (profilePicOption |> Option.defaultValue "")

                // Add successful auth attributes to span
                Tracing.addAttribute currentActivity "tailscale.auth.success" "true"
                Tracing.addUserAttributes currentActivity None (Some userEmail)

                userNameOption
                |> Option.iter (Tracing.addAttribute currentActivity "tailscale.auth.user_name")

                profilePicOption
                |> Option.iter (Tracing.addAttribute currentActivity "tailscale.auth.user_profile_pic")

                Tracing.setSpanStatus currentActivity true None

                do! next.Invoke context
    }