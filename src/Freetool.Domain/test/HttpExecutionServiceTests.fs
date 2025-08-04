module Freetool.Domain.Tests.HttpExecutionServiceTests

open System.Threading.Tasks
open Xunit
open Freetool.Domain
open Freetool.Domain.Services

[<Fact>]
let ``HttpExecutionService executeRequest should return mock response`` () = task {
    // Arrange
    let request = {
        BaseUrl = "https://api.example.com/users/123"
        UrlParameters = [ ("limit", "10") ]
        Headers = [ ("Authorization", "Bearer token123") ]
        Body = [ ("email", "test@example.com") ]
        HttpMethod = "GET"
    }

    // Act
    let! result = HttpExecutionService.executeRequest request

    // Assert
    match result with
    | Ok response ->
        Assert.Contains("Mock response", response)
        Assert.Contains("GET", response)
        Assert.Contains("https://api.example.com/users/123", response)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")
}

[<Fact>]
let ``HttpExecutionService executeRequest should handle different HTTP methods`` () = task {
    let httpMethods = [ "GET"; "POST"; "PUT"; "DELETE"; "PATCH" ]

    for httpMethod in httpMethods do
        // Arrange
        let request = {
            BaseUrl = "https://api.example.com/test"
            UrlParameters = []
            Headers = []
            Body = []
            HttpMethod = httpMethod
        }

        // Act
        let! result = HttpExecutionService.executeRequest request

        // Assert
        match result with
        | Ok response ->
            Assert.Contains("Mock response", response)
            Assert.Contains(httpMethod, response)
        | Error error -> Assert.True(false, $"Expected success for {httpMethod} but got error: {error}")
}