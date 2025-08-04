module Freetool.Domain.Tests.RunTests

open System
open Xunit
open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events
open Freetool.Domain.Services

// Helper function for test assertions
let unwrapResult result =
    match result with
    | Ok value -> value
    | Error error -> failwith $"Expected Ok but got Error: {error}"

// Helper function to create test app with inputs - returns both app and resource for testing
let createTestAppWithResource () =
    let folderId = FolderId.NewId()

    let inputs = [
        {
            Title = "userId"
            Type = InputType.Text(50) |> Result.defaultValue (InputType.Email())
            Required = true
        }
        {
            Title = "email"
            Type = InputType.Email()
            Required = true
        }
    ]

    // Create a test resource
    let resource =
        Resource.create
            "Test API"
            "Test endpoint"
            "https://api.test.com/users/{userId}"
            [ "limit", "10" ]
            [ "Authorization", "Bearer {token}" ]
            [ "email", "{email}" ]
            "GET"
        |> unwrapResult

    let app =
        App.createWithResource "Test App" folderId resource inputs (Some "/{userId}/profile") [] [] []
        |> unwrapResult

    (app, resource)

// Helper function to create test app with inputs
let createTestApp () = createTestAppWithResource () |> fst

[<Fact>]
let ``Run creation should validate required inputs`` () =
    // Arrange
    let app = createTestApp ()

    let inputValues = [
        { Title = "userId"; Value = "123" }
    // Missing required email input intentionally
    ]

    // Act
    let result = Run.createWithValidation app inputValues

    // Assert
    match result with
    | Error(ValidationError message) -> Assert.Contains("email", message)
    | _ -> Assert.True(false, "Expected validation error for missing required input")

[<Fact>]
let ``Run creation should reject invalid input names`` () =
    // Arrange
    let app = createTestApp ()

    let inputValues = [
        { Title = "userId"; Value = "123" }
        {
            Title = "email"
            Value = "test@example.com"
        }
        {
            Title = "invalidInput"
            Value = "test"
        } // This input doesn't exist in app schema
    ]

    // Act
    let result = Run.createWithValidation app inputValues

    // Assert
    match result with
    | Error(ValidationError message) ->
        Assert.Contains("Invalid inputs not defined in app", message)
        Assert.Contains("invalidInput", message)
    | _ -> Assert.True(false, "Expected validation error for invalid input name")

[<Fact>]
let ``Run creation with valid inputs should succeed and generate events`` () =
    // Arrange
    let app = createTestApp ()

    let inputValues = [
        { Title = "userId"; Value = "123" }
        {
            Title = "email"
            Value = "test@example.com"
        }
    ]

    // Act
    let result = Run.createWithValidation app inputValues

    // Assert
    match result with
    | Ok run ->
        Assert.Equal(Pending, Run.getStatus run)
        Assert.Equal(App.getId app, Run.getAppId run)
        Assert.Equal<RunInputValue list>(inputValues, Run.getInputValues run)

        let events = Run.getUncommittedEvents run
        Assert.Single events

        match events.[0] with
        | :? RunCreatedEvent as event ->
            Assert.Equal(Run.getId run, event.RunId)
            Assert.Equal(App.getId app, event.AppId)
            Assert.Equal<RunInputValue list>(inputValues, event.InputValues)
        | _ -> Assert.True(false, "Expected RunCreatedEvent")
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Run status transitions should generate correct events`` () =
    // Arrange
    let app = createTestApp ()

    let inputValues = [
        { Title = "userId"; Value = "123" }
        {
            Title = "email"
            Value = "test@example.com"
        }
    ]

    let run = Run.createWithValidation app inputValues |> unwrapResult

    // Act - transition to running
    let runningRun = Run.markAsRunning run

    // Assert
    Assert.Equal(Running, Run.getStatus runningRun)
    Assert.True((Run.getStartedAt runningRun).IsSome)

    let events = Run.getUncommittedEvents runningRun

    let statusEvents =
        events
        |> List.choose (function
            | :? RunStatusChangedEvent as e -> Some e
            | _ -> None)

    Assert.Single statusEvents

    let statusEvent = statusEvents.[0]
    Assert.Equal(Pending, statusEvent.OldStatus)
    Assert.Equal(Running, statusEvent.NewStatus)

[<Fact>]
let ``Run completion with success should update status and response`` () =
    // Arrange
    let app = createTestApp ()

    let inputValues = [
        { Title = "userId"; Value = "123" }
        {
            Title = "email"
            Value = "test@example.com"
        }
    ]

    let run = Run.createWithValidation app inputValues |> unwrapResult
    let runningRun = Run.markAsRunning run
    let response = """{"id": 123, "name": "John Doe"}"""

    // Act
    let completedRun = Run.markAsSuccess response runningRun

    // Assert
    Assert.Equal(Success, Run.getStatus completedRun)
    Assert.Equal(Some response, Run.getResponse completedRun)
    Assert.True((Run.getCompletedAt completedRun).IsSome)
    Assert.Equal(None, Run.getErrorMessage completedRun)

[<Fact>]
let ``Run completion with failure should update status and error message`` () =
    // Arrange
    let app = createTestApp ()

    let inputValues = [
        { Title = "userId"; Value = "123" }
        {
            Title = "email"
            Value = "test@example.com"
        }
    ]

    let run = Run.createWithValidation app inputValues |> unwrapResult
    let runningRun = Run.markAsRunning run
    let errorMessage = "Network timeout after 30 seconds"

    // Act
    let failedRun = Run.markAsFailure errorMessage runningRun

    // Assert
    Assert.Equal(Failure, Run.getStatus failedRun)
    Assert.Equal(Some errorMessage, Run.getErrorMessage failedRun)
    Assert.True((Run.getCompletedAt failedRun).IsSome)
    Assert.Equal(None, Run.getResponse failedRun)

[<Fact>]
let ``Run with invalid configuration should update status correctly`` () =
    // Arrange
    let app = createTestApp ()

    let inputValues = [
        { Title = "userId"; Value = "123" }
        {
            Title = "email"
            Value = "test@example.com"
        }
    ]

    let run = Run.createWithValidation app inputValues |> unwrapResult
    let configError = "Resource not found"

    // Act
    let invalidRun = Run.markAsInvalidConfiguration configError run

    // Assert
    Assert.Equal(InvalidConfiguration, Run.getStatus invalidRun)
    Assert.Equal(Some configError, Run.getErrorMessage invalidRun)
    Assert.True((Run.getCompletedAt invalidRun).IsSome)

[<Fact>]
let ``Run executable request composition should substitute input values`` () =
    // Arrange
    let app, resource = createTestAppWithResource ()

    let inputValues = [
        { Title = "userId"; Value = "123" }
        {
            Title = "email"
            Value = "john@example.com"
        }
    ]

    let run = Run.createWithValidation app inputValues |> unwrapResult

    // Act
    let result = Run.composeExecutableRequestFromAppAndResource run app resource

    // Assert
    match result with
    | Ok runWithRequest ->
        match Run.getExecutableRequest runWithRequest with
        | Some execRequest ->
            // Base URL should have userId substituted
            Assert.Contains("123", execRequest.BaseUrl)
            Assert.DoesNotContain("{userId}", execRequest.BaseUrl)

            // Headers should have token placeholder (not substituted since no token input provided)
            let authHeader =
                execRequest.Headers |> List.find (fun (k, _) -> k = "Authorization")

            Assert.Equal(("Authorization", "Bearer {token}"), authHeader)

            // Body should have email substituted
            let emailBody = execRequest.Body |> List.find (fun (k, _) -> k = "email")
            Assert.Equal(("email", "john@example.com"), emailBody)

        | None -> Assert.True(false, "Expected executable request to be set")
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")