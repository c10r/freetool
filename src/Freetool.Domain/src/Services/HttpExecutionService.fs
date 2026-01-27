namespace Freetool.Domain.Services

open System
open System.Net.Http
open System.Text
open System.Text.Json
open System.Threading.Tasks
open Freetool.Domain

module HttpExecutionService =

    /// Build the full URL with query parameters
    let private buildUrl (baseUrl: string) (urlParameters: (string * string) list) : string =
        if List.isEmpty urlParameters then
            baseUrl
        else
            let queryString =
                urlParameters
                |> List.map (fun (key, value) -> $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}")
                |> String.concat "&"

            let separator = if baseUrl.Contains("?") then "&" else "?"
            $"{baseUrl}{separator}{queryString}"

    /// Build request body content based on HTTP method, body parameters, and content type preference
    let private buildRequestContent
        (httpMethod: string)
        (bodyParameters: (string * string) list)
        (useJsonBody: bool)
        : HttpContent option =
        match httpMethod.ToUpperInvariant() with
        | "GET"
        | "DELETE"
        | "HEAD" -> None // These methods typically don't have request bodies
        | _ when List.isEmpty bodyParameters -> None // No body parameters provided
        | _ ->
            if useJsonBody then
                // Build JSON object from key-value pairs, preserving value types
                // Try to parse values as their proper JSON types (number, boolean, null)
                let jsonObject =
                    bodyParameters
                    |> List.fold
                        (fun (dict: System.Collections.Generic.Dictionary<string, obj>) (key, value) ->
                            // Try parsing as different types to preserve JSON type information
                            let typedValue: obj =
                                match System.Int64.TryParse(value) with
                                | true, intVal -> intVal :> obj
                                | false, _ ->
                                    match
                                        System.Double.TryParse(
                                            value,
                                            System.Globalization.NumberStyles.Float,
                                            System.Globalization.CultureInfo.InvariantCulture
                                        )
                                    with
                                    | true, floatVal -> floatVal :> obj
                                    | false, _ ->
                                        match System.Boolean.TryParse(value) with
                                        | true, boolVal -> boolVal :> obj
                                        | false, _ -> if value = "null" then null else value :> obj

                            dict.[key] <- typedValue
                            dict)
                        (System.Collections.Generic.Dictionary<string, obj>())

                let jsonString = JsonSerializer.Serialize(jsonObject)
                let content = new StringContent(jsonString, Encoding.UTF8, "application/json")
                Some(content :> HttpContent)
            else
                // Build form-encoded content for POST/PUT/PATCH requests
                let formContent =
                    bodyParameters
                    |> List.map (fun (key, value) -> $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}")
                    |> String.concat "&"

                let content =
                    new StringContent(formContent, Encoding.UTF8, "application/x-www-form-urlencoded")

                Some(content :> HttpContent)

    /// Execute an ExecutableHttpRequest using provided HttpClient and return the response or error
    let executeRequestWithClient
        (httpClient: HttpClient)
        (request: ExecutableHttpRequest)
        : Task<Result<string, DomainError>> =
        task {
            try
                // Build the full URL with query parameters
                let fullUrl = buildUrl request.BaseUrl request.UrlParameters

                // Create HTTP request message
                let httpMethod = HttpMethod(request.HttpMethod.ToUpperInvariant())
                use requestMessage = new HttpRequestMessage(httpMethod, fullUrl)

                // Add headers - track which ones fail to be added to request headers
                let mutable failedRequestHeaders = []

                for (headerName, headerValue) in request.Headers do
                    try
                        // Try to add to request headers first
                        let added = requestMessage.Headers.TryAddWithoutValidation(headerName, headerValue)

                        if not added then
                            failedRequestHeaders <- (headerName, headerValue) :: failedRequestHeaders
                    with _ ->
                        failedRequestHeaders <- (headerName, headerValue) :: failedRequestHeaders

                // Add body content if applicable
                match buildRequestContent request.HttpMethod request.Body request.UseJsonBody with
                | Some content ->
                    requestMessage.Content <- content

                    // Only add headers that failed to be added to request headers
                    for (headerName, headerValue) in failedRequestHeaders do
                        try
                            if requestMessage.Content.Headers.Contains(headerName) |> not then
                                requestMessage.Content.Headers.TryAddWithoutValidation(headerName, headerValue)
                                |> ignore
                        with _ ->
                            ()
                | None -> ()

                // Execute the request
                use! response = httpClient.SendAsync(requestMessage)

                // Read response content
                let! responseContent = response.Content.ReadAsStringAsync()

                // Check if the response indicates success
                if response.IsSuccessStatusCode then
                    return Ok(responseContent)
                else
                    return
                        Error(
                            InvalidOperation
                                $"HTTP request failed with status {int response.StatusCode} ({response.StatusCode}): {responseContent}"
                        )

            with
            | :? HttpRequestException as ex -> return Error(InvalidOperation $"HTTP request failed: {ex.Message}")
            | :? TaskCanceledException as ex -> return Error(InvalidOperation $"HTTP request timed out: {ex.Message}")
            | :? UriFormatException as ex -> return Error(ValidationError $"Invalid URL format: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Unexpected error during HTTP request: {ex.Message}")
        }

    /// Execute an ExecutableHttpRequest and return the response or error
    /// This version creates its own HttpClient for simple usage scenarios
    let executeRequest (request: ExecutableHttpRequest) : Task<Result<string, DomainError>> =
        task {
            use httpClient = new HttpClient()
            return! executeRequestWithClient httpClient request
        }
