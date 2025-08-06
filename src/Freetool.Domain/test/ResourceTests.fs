module Freetool.Domain.Tests.ResourceTests

open Xunit
open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Domain.Events

// Helper function for test assertions
let unwrapResult result =
    match result with
    | Ok value -> value
    | Error error -> failwith $"Expected Ok but got Error: {error}"

[<Fact>]
let ``Resource creation should generate ResourceCreatedEvent`` () =
    // Arrange
    let urlParams = [ ("userId", "123"); ("format", "json") ]
    let headers = [ ("Authorization", "Bearer token"); ("Content-Type", "application/json") ]
    let body = [ ("name", "John Doe"); ("email", "john@example.com") ]

    // Act
    let result =
        Resource.create "User API" "Manages user data" "https://api.example.com/users" urlParams headers body "GET"

    // Assert
    match result with
    | Ok resource ->
        let events = Resource.getUncommittedEvents resource
        Assert.Single(events) |> ignore

        match events.[0] with
        | :? ResourceCreatedEvent as event ->
            Assert.Equal("User API", event.Name.Value)
            Assert.Equal("Manages user data", event.Description.Value)
            Assert.Equal("https://api.example.com/users", event.BaseUrl.Value)
            Assert.Equal(2, event.UrlParameters.Length)
            Assert.Equal(2, event.Headers.Length)
            Assert.Equal(2, event.Body.Length)
        | _ -> Assert.True(false, "Expected ResourceCreatedEvent")
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Resource name update should generate correct event`` () =
    // Arrange
    let resource =
        Resource.create "Old API Name" "Description" "https://api.example.com" [] [] [] "GET"
        |> unwrapResult

    // Act
    let result = Resource.updateName "New API Name" resource

    // Assert
    match result with
    | Ok updatedResource ->
        let events = Resource.getUncommittedEvents updatedResource
        Assert.Equal(2, events.Length) // Creation event + update event

        match events.[1] with // The update event is the second one
        | :? ResourceUpdatedEvent as event ->
            Assert.Single(event.Changes) |> ignore

            match event.Changes.[0] with
            | ResourceChange.NameChanged(oldValue, newValue) ->
                Assert.Equal("Old API Name", oldValue.Value)
                Assert.Equal("New API Name", newValue.Value)
            | _ -> Assert.True(false, "Expected NameChanged event")
        | _ -> Assert.True(false, "Expected ResourceUpdatedEvent")
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Resource description update should generate correct event`` () =
    // Arrange
    let resource =
        Resource.create "API Name" "Old description" "https://api.example.com" [] [] [] "GET"
        |> unwrapResult

    // Act
    let result = Resource.updateDescription "New detailed description" resource

    // Assert
    match result with
    | Ok updatedResource ->
        let events = Resource.getUncommittedEvents updatedResource
        Assert.Equal(2, events.Length) // Creation event + update event

        match events.[1] with // The update event is the second one
        | :? ResourceUpdatedEvent as event ->
            Assert.Single(event.Changes) |> ignore

            match event.Changes.[0] with
            | ResourceChange.DescriptionChanged(oldValue, newValue) ->
                Assert.Equal("Old description", oldValue.Value)
                Assert.Equal("New detailed description", newValue.Value)
            | _ -> Assert.True(false, "Expected DescriptionChanged event")
        | _ -> Assert.True(false, "Expected ResourceUpdatedEvent")
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Resource base URL update should generate correct event`` () =
    // Arrange
    let resource =
        Resource.create "API Name" "Description" "https://old-api.example.com" [] [] [] "POST"
        |> unwrapResult

    // Act
    let result = Resource.updateBaseUrl "https://new-api.example.com" resource

    // Assert
    match result with
    | Ok updatedResource ->
        let events = Resource.getUncommittedEvents updatedResource
        Assert.Equal(2, events.Length) // Creation event + update event

        match events.[1] with // The update event is the second one
        | :? ResourceUpdatedEvent as event ->
            Assert.Single(event.Changes) |> ignore

            match event.Changes.[0] with
            | ResourceChange.BaseUrlChanged(oldValue, newValue) ->
                Assert.Equal("https://old-api.example.com", oldValue.Value)
                Assert.Equal("https://new-api.example.com", newValue.Value)
            | _ -> Assert.True(false, "Expected BaseUrlChanged event")
        | _ -> Assert.True(false, "Expected ResourceUpdatedEvent")
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Resource URL parameters update should generate correct event`` () =
    // Arrange
    let initialParams = [ ("id", "1") ]

    let resource =
        Resource.create "API Name" "Description" "https://api.example.com" initialParams [] [] "PUT"
        |> unwrapResult

    let newParams = [ ("userId", "123"); ("format", "json"); ("limit", "10") ]

    // Act
    let result = Resource.updateUrlParameters newParams [] resource

    // Assert
    match result with
    | Ok updatedResource ->
        let events = Resource.getUncommittedEvents updatedResource
        Assert.Equal(2, events.Length) // Creation event + update event

        match events.[1] with // The update event is the second one
        | :? ResourceUpdatedEvent as event ->
            Assert.Single(event.Changes) |> ignore

            match event.Changes.[0] with
            | ResourceChange.UrlParametersChanged(oldParams, newParams) ->
                Assert.Single(oldParams) |> ignore
                Assert.Equal("id", oldParams.[0].Key)
                Assert.Equal("1", oldParams.[0].Value)
                Assert.Equal(3, newParams.Length)
                Assert.Equal("userId", newParams.[0].Key)
                Assert.Equal("123", newParams.[0].Value)
            | _ -> Assert.True(false, "Expected UrlParametersChanged event")
        | _ -> Assert.True(false, "Expected ResourceUpdatedEvent")
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Resource headers update should generate correct event`` () =
    // Arrange
    let initialHeaders = [ ("Accept", "application/json") ]

    let resource =
        Resource.create "API Name" "Description" "https://api.example.com" [] initialHeaders [] "DELETE"
        |> unwrapResult

    let newHeaders = [ ("Authorization", "Bearer token"); ("Content-Type", "application/json") ]

    // Act
    let result = Resource.updateHeaders newHeaders [] resource

    // Assert
    match result with
    | Ok updatedResource ->
        let events = Resource.getUncommittedEvents updatedResource
        Assert.Equal(2, events.Length) // Creation event + update event

        match events.[1] with // The update event is the second one
        | :? ResourceUpdatedEvent as event ->
            Assert.Single(event.Changes) |> ignore

            match event.Changes.[0] with
            | ResourceChange.HeadersChanged(oldHeaders, newHeaders) ->
                Assert.Single(oldHeaders) |> ignore
                Assert.Equal("Accept", oldHeaders.[0].Key)
                Assert.Equal(2, newHeaders.Length)
                Assert.Equal("Authorization", newHeaders.[0].Key)
            | _ -> Assert.True(false, "Expected HeadersChanged event")
        | _ -> Assert.True(false, "Expected ResourceUpdatedEvent")
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Resource body update should generate correct event`` () =
    // Arrange
    let initialBody = [ ("name", "John") ]

    let resource =
        Resource.create "API Name" "Description" "https://api.example.com" [] [] initialBody "PATCH"
        |> unwrapResult

    let newBody = [ ("firstName", "Jane"); ("lastName", "Doe"); ("age", "30") ]

    // Act
    let result = Resource.updateBody newBody [] resource

    // Assert
    match result with
    | Ok updatedResource ->
        let events = Resource.getUncommittedEvents updatedResource
        Assert.Equal(2, events.Length) // Creation event + update event

        match events.[1] with // The update event is the second one
        | :? ResourceUpdatedEvent as event ->
            Assert.Single(event.Changes) |> ignore

            match event.Changes.[0] with
            | ResourceChange.BodyChanged(oldBody, newBody) ->
                Assert.Single(oldBody) |> ignore
                Assert.Equal("name", oldBody.[0].Key)
                Assert.Equal(3, newBody.Length)
                Assert.Equal("firstName", newBody.[0].Key)
            | _ -> Assert.True(false, "Expected BodyChanged event")
        | _ -> Assert.True(false, "Expected ResourceUpdatedEvent")
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Resource creation should reject empty name`` () =
    // Act
    let result =
        Resource.create "" "Description" "https://api.example.com" [] [] [] "GET"

    // Assert
    match result with
    | Error(ValidationError msg) -> Assert.Equal("Resource name cannot be empty", msg)
    | _ -> Assert.True(false, "Expected validation error for empty name")

[<Fact>]
let ``Resource creation should reject empty description`` () =
    // Act
    let result = Resource.create "API Name" "" "https://api.example.com" [] [] [] "GET"

    // Assert
    match result with
    | Error(ValidationError msg) -> Assert.Equal("Resource description cannot be empty", msg)
    | _ -> Assert.True(false, "Expected validation error for empty description")

[<Fact>]
let ``Resource creation should reject invalid URL`` () =
    // Act
    let result =
        Resource.create "API Name" "Description" "not-a-valid-url" [] [] [] "GET"

    // Assert
    match result with
    | Error(ValidationError msg) -> Assert.Equal("Invalid URL format", msg)
    | _ -> Assert.True(false, "Expected validation error for invalid URL")

[<Fact>]
let ``Resource creation should reject invalid key-value pairs`` () =
    // Arrange
    let invalidParams = [ ("", "value") ] // Empty key

    // Act
    let result =
        Resource.create "API Name" "Description" "https://api.example.com" invalidParams [] [] "GET"

    // Assert
    match result with
    | Error(ValidationError msg) -> Assert.Equal("Key cannot be empty", msg)
    | _ -> Assert.True(false, "Expected validation error for invalid key-value pair")

[<Fact>]
let ``Resource deletion should generate ResourceDeletedEvent`` () =
    // Arrange
    let resource =
        Resource.create "Test API" "Description" "https://api.example.com" [] [] [] "GET"
        |> unwrapResult

    // Act
    let deletedResource = Resource.markForDeletion resource

    // Assert
    let events = Resource.getUncommittedEvents deletedResource
    Assert.Equal(2, events.Length) // Creation event + deletion event

    match events.[1] with // The deletion event is the second one
    | :? ResourceDeletedEvent as event -> Assert.Equal(resource.State.Id, event.ResourceId)
    | _ -> Assert.True(false, "Expected ResourceDeletedEvent")