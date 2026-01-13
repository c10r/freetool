module Freetool.Domain.Tests.HttpExecutionServiceTests

open System.Net
open System.Net.Http
open System.Text
open System.Threading
open System.Threading.Tasks
open Xunit
open Freetool.Domain
open Freetool.Domain.Services

// Mock HttpMessageHandler for testing
type MockHttpMessageHandler(responseContent: string, statusCode: HttpStatusCode) =
    inherit HttpMessageHandler()

    override _.SendAsync
        (request: HttpRequestMessage, cancellationToken: CancellationToken)
        : Task<HttpResponseMessage> =
        task {
            let response = new HttpResponseMessage(statusCode)
            response.Content <- new StringContent(responseContent, Encoding.UTF8, "application/json")
            return response
        }

[<Fact>]
let ``HttpExecutionService executeRequestWithClient should build correct URL with parameters`` () =
    task {
        // Arrange
        let mockHandler = new MockHttpMessageHandler("test response", HttpStatusCode.OK)
        use httpClient = new HttpClient(mockHandler)

        let request =
            { BaseUrl = "https://api.example.com/users/123"
              UrlParameters = [ ("limit", "10"); ("offset", "5") ]
              Headers = [ ("Authorization", "Bearer token123") ]
              Body = [ ("email", "test@example.com") ]
              HttpMethod = "GET"
              UseJsonBody = false }

        // Act
        let! result = HttpExecutionService.executeRequestWithClient httpClient request

        // Assert
        match result with
        | Ok response -> Assert.Equal("test response", response)
        | Error error -> Assert.True(false, $"Expected success but got error: {error}")
    }

[<Fact>]
let ``HttpExecutionService executeRequestWithClient should handle different HTTP methods`` () =
    task {
        let httpMethods = [ "GET"; "POST"; "PUT"; "DELETE"; "PATCH" ]

        for httpMethod in httpMethods do
            // Arrange
            let mockHandler =
                new MockHttpMessageHandler($"Response for {httpMethod}", HttpStatusCode.OK)

            use httpClient = new HttpClient(mockHandler)

            let request =
                { BaseUrl = "https://api.example.com/test"
                  UrlParameters = []
                  Headers = []
                  Body = []
                  HttpMethod = httpMethod
                  UseJsonBody = false }

            // Act
            let! result = HttpExecutionService.executeRequestWithClient httpClient request

            // Assert
            match result with
            | Ok response -> Assert.Contains($"Response for {httpMethod}", response)
            | Error error -> Assert.True(false, $"Expected success for {httpMethod} but got error: {error}")
    }

[<Fact>]
let ``HttpExecutionService executeRequestWithClient should handle HTTP error responses`` () =
    task {
        // Arrange
        let mockHandler = new MockHttpMessageHandler("Not Found", HttpStatusCode.NotFound)
        use httpClient = new HttpClient(mockHandler)

        let request =
            { BaseUrl = "https://api.example.com/nonexistent"
              UrlParameters = []
              Headers = []
              Body = []
              HttpMethod = "GET"
              UseJsonBody = false }

        // Act
        let! result = HttpExecutionService.executeRequestWithClient httpClient request

        // Assert
        match result with
        | Error(InvalidOperation message) ->
            Assert.Contains("404", message)
            Assert.Contains("Not Found", message)
        | Ok response -> Assert.True(false, $"Expected error but got success: {response}")
        | Error error -> Assert.True(false, $"Expected InvalidOperation but got: {error}")
    }

[<Fact>]
let ``HttpExecutionService executeRequestWithClient should handle POST with body parameters`` () =
    task {
        // Arrange
        let mockHandler = new MockHttpMessageHandler("Created", HttpStatusCode.Created)
        use httpClient = new HttpClient(mockHandler)

        let request =
            { BaseUrl = "https://api.example.com/users"
              UrlParameters = []
              Headers = [ ("Content-Type", "application/x-www-form-urlencoded") ]
              Body = [ ("name", "John Doe"); ("email", "john@example.com") ]
              HttpMethod = "POST"
              UseJsonBody = false }

        // Act
        let! result = HttpExecutionService.executeRequestWithClient httpClient request

        // Assert
        match result with
        | Ok response -> Assert.Equal("Created", response)
        | Error error -> Assert.True(false, $"Expected success but got error: {error}")
    }
