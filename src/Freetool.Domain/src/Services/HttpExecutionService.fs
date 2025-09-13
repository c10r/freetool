namespace Freetool.Domain.Services

open System
open System.Net.Http
open System.Text
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

    /// Build request body content based on HTTP method and body parameters
    let private buildRequestContent (httpMethod: string) (bodyParameters: (string * string) list) : HttpContent option =
        match httpMethod.ToUpperInvariant() with
        | "GET"
        | "DELETE"
        | "HEAD" -> None // These methods typically don't have request bodies
        | _ when List.isEmpty bodyParameters -> None // No body parameters provided
        | _ ->
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

                // Add headers
                for (headerName, headerValue) in request.Headers do
                    try
                        // Try to add to request headers first
                        if not (requestMessage.Headers.TryAddWithoutValidation(headerName, headerValue)) then
                            // If it fails, it might be a content header, so we'll handle it when we set content
                            ()
                    with _ ->
                        () // Ignore invalid headers

                // Add body content if applicable
                match buildRequestContent request.HttpMethod request.Body with
                | Some content ->
                    requestMessage.Content <- content

                    // Add any content headers that couldn't be added to request headers
                    for (headerName, headerValue) in request.Headers do
                        try
                            if requestMessage.Content.Headers.Contains(headerName) |> not then
                                requestMessage.Content.Headers.TryAddWithoutValidation(headerName, headerValue)
                                |> ignore
                        with _ ->
                            () // Ignore invalid content headers
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
