namespace Freetool.Api.Middleware

open System
open System.Threading.Tasks
open System.Diagnostics
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Freetool.Api
open Freetool.Api.Services
open Freetool.Api.Tracing

type IapAuthMiddleware(next: RequestDelegate, logger: ILogger<IapAuthMiddleware>) =

    let defaultEmailHeader = "X-Goog-Authenticated-User-Email"
    let defaultNameHeader = "X-Goog-Authenticated-User-Name"
    let defaultPictureHeader = "X-Goog-Iap-Attr-Picture"
    let defaultGroupsHeader = "X-Goog-Iap-Attr-Groups"
    let defaultGroupsDelimiter = ","

    let extractHeader (headerKey: string) (context: HttpContext) : string option =
        match context.Request.Headers.TryGetValue headerKey with
        | true, values when values.Count > 0 ->
            let value = values.[0]

            if String.IsNullOrWhiteSpace value then None else Some value
        | _ -> None

    let parseEmailValue (rawValue: string) =
        // IAP may send values like "accounts.google.com:user@company.com"
        let separatorIndex = rawValue.IndexOf(":")

        if separatorIndex >= 0 && separatorIndex < rawValue.Length - 1 then
            rawValue.Substring(separatorIndex + 1)
        else
            rawValue

    let parseGroups (rawGroups: string option) (delimiter: string) =
        let actualDelimiter =
            if String.IsNullOrWhiteSpace delimiter then
                defaultGroupsDelimiter
            else
                delimiter

        rawGroups
        |> Option.defaultValue ""
        |> fun value ->
            value.Split(actualDelimiter, StringSplitOptions.TrimEntries ||| StringSplitOptions.RemoveEmptyEntries)
        |> Array.toList
        |> List.distinct

    member _.InvokeAsync(context: HttpContext) : Task =
        task {
            let currentActivity = Option.ofObj Activity.Current
            let configuration = context.RequestServices.GetRequiredService<IConfiguration>()

            let emailHeader =
                configuration[ConfigurationKeys.Auth.IAP.EmailHeader]
                |> Option.ofObj
                |> Option.defaultValue defaultEmailHeader

            let nameHeader =
                configuration[ConfigurationKeys.Auth.IAP.NameHeader]
                |> Option.ofObj
                |> Option.defaultValue defaultNameHeader

            let pictureHeader =
                configuration[ConfigurationKeys.Auth.IAP.PictureHeader]
                |> Option.ofObj
                |> Option.defaultValue defaultPictureHeader

            let groupsHeader =
                configuration[ConfigurationKeys.Auth.IAP.GroupsHeader]
                |> Option.ofObj
                |> Option.defaultValue defaultGroupsHeader

            let groupsDelimiter =
                configuration[ConfigurationKeys.Auth.IAP.GroupsDelimiter]
                |> Option.ofObj
                |> Option.defaultValue defaultGroupsDelimiter

            match extractHeader emailHeader context with
            | None ->
                Tracing.addAttribute currentActivity "iap.auth.error" "missing_email_header"
                Tracing.addAttribute currentActivity "iap.auth.header" emailHeader
                Tracing.setSpanStatus currentActivity false (Some "Missing IAP email header")
                context.Response.StatusCode <- 401
                do! context.Response.WriteAsync $"Unauthorized: Missing or invalid {emailHeader}"
            | Some rawEmail ->
                let userEmail = parseEmailValue rawEmail
                let userName = extractHeader nameHeader context
                let profilePicUrl = extractHeader pictureHeader context
                let groupKeys = parseGroups (extractHeader groupsHeader context) groupsDelimiter

                let provisioningService =
                    context.RequestServices.GetRequiredService<IIdentityProvisioningService>()

                let! result =
                    provisioningService.EnsureUserAsync
                        { Email = userEmail
                          Name = userName
                          ProfilePicUrl = profilePicUrl
                          GroupKeys = groupKeys
                          Source = "iap" }

                match result with
                | Error(InvalidEmailFormat errorMessage) ->
                    Tracing.addAttribute currentActivity "iap.auth.error" "invalid_email_format"
                    Tracing.addAttribute currentActivity "iap.auth.user_email" userEmail
                    Tracing.setSpanStatus currentActivity false (Some "Invalid email format in IAP header")
                    logger.LogWarning("Failed to provision IAP user {Email}: {Error}", userEmail, errorMessage)
                    context.Response.StatusCode <- 401
                    do! context.Response.WriteAsync $"Unauthorized: Invalid {emailHeader}"
                | Error error ->
                    let errorMessage = IdentityProvisioningError.toMessage error
                    Tracing.addAttribute currentActivity "iap.auth.error" "provisioning_failed"
                    Tracing.addAttribute currentActivity "iap.auth.user_email" userEmail
                    Tracing.setSpanStatus currentActivity false (Some "Failed to provision user")
                    logger.LogWarning("Failed to provision IAP user {Email}: {Error}", userEmail, errorMessage)
                    context.Response.StatusCode <- 500
                    do! context.Response.WriteAsync $"Internal Server Error: Failed to provision user - {errorMessage}"
                | Ok userId ->
                    context.Items.["UserId"] <- userId
                    Tracing.addAttribute currentActivity "iap.auth.user_email" userEmail
                    Tracing.addAttribute currentActivity "iap.auth.groups_count" (string groupKeys.Length)
                    Tracing.addAttribute currentActivity "user.id" (userId.Value.ToString())
                    Tracing.addAttribute currentActivity "iap.auth.success" "true"
                    Tracing.setSpanStatus currentActivity true None
                    do! next.Invoke context
        }
