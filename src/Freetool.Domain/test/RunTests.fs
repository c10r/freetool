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
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let spaceId = SpaceId.FromGuid(Guid.NewGuid())

    let inputs =
        [ { Title = "userId"
            Type = InputType.Text(50) |> Result.defaultValue (InputType.Email())
            Required = true }
          { Title = "email"
            Type = InputType.Email()
            Required = true } ]

    // Create a test resource
    let resource =
        Resource.create
            actorUserId
            spaceId
            "Test API"
            "Test endpoint"
            "https://api.test.com/users/{userId}"
            [ "limit", "10" ]
            [ "Authorization", "Bearer {token}" ]
            [ "email", "{email}" ]

        |> unwrapResult

    let app =
        App.createWithResource
            actorUserId
            "Test App"
            folderId
            resource
            HttpMethod.Get
            inputs
            (Some "/{userId}/profile")
            []
            []
            []
        |> unwrapResult

    app, resource

// Helper function to create test app with inputs
let createTestApp () = createTestAppWithResource () |> fst

[<Fact>]
let ``Run creation should validate required inputs`` () =
    // Arrange
    let app = createTestApp ()
    let actorUserId = UserId.FromGuid(Guid.NewGuid())

    let inputValues =
        [ { Title = "userId"; Value = "123" }
          // Missing required email input intentionally
          ]

    // Act
    let result = Run.createWithValidation actorUserId app inputValues

    // Assert
    match result with
    | Error(ValidationError message) -> Assert.Contains("email", message)
    | _ -> Assert.True(false, "Expected validation error for missing required input")

[<Fact>]
let ``Run creation should reject invalid input names`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let app = createTestApp ()

    let inputValues =
        [ { Title = "userId"; Value = "123" }
          { Title = "email"
            Value = "test@example.com" }
          { Title = "invalidInput"
            Value = "test" } ] // This input doesn't exist in app schema

    // Act
    let result = Run.createWithValidation actorUserId app inputValues

    // Assert
    match result with
    | Error(ValidationError message) ->
        Assert.Contains("Invalid inputs not defined in app", message)
        Assert.Contains("invalidInput", message)
    | _ -> Assert.True(false, "Expected validation error for invalid input name")

[<Fact>]
let ``Run creation with valid inputs should succeed and generate events`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let app = createTestApp ()

    let inputValues =
        [ { Title = "userId"; Value = "123" }
          { Title = "email"
            Value = "test@example.com" } ]

    // Act
    let result = Run.createWithValidation actorUserId app inputValues

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
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let app = createTestApp ()

    let inputValues =
        [ { Title = "userId"; Value = "123" }
          { Title = "email"
            Value = "test@example.com" } ]

    let run = Run.createWithValidation actorUserId app inputValues |> unwrapResult

    // Act - transition to running
    let runningRun = Run.markAsRunning actorUserId run

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
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let app = createTestApp ()

    let inputValues =
        [ { Title = "userId"; Value = "123" }
          { Title = "email"
            Value = "test@example.com" } ]

    let run = Run.createWithValidation actorUserId app inputValues |> unwrapResult
    let runningRun = Run.markAsRunning actorUserId run
    let response = """{"id": 123, "name": "John Doe"}"""

    // Act
    let completedRun = Run.markAsSuccess actorUserId response runningRun

    // Assert
    Assert.Equal(Success, Run.getStatus completedRun)
    Assert.Equal(Some response, Run.getResponse completedRun)
    Assert.True((Run.getCompletedAt completedRun).IsSome)
    Assert.Equal(None, Run.getErrorMessage completedRun)

[<Fact>]
let ``Run completion with failure should update status and error message`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let app = createTestApp ()

    let inputValues =
        [ { Title = "userId"; Value = "123" }
          { Title = "email"
            Value = "test@example.com" } ]

    let run = Run.createWithValidation actorUserId app inputValues |> unwrapResult
    let runningRun = Run.markAsRunning actorUserId run
    let errorMessage = "Network timeout after 30 seconds"

    // Act
    let failedRun = Run.markAsFailure actorUserId errorMessage runningRun

    // Assert
    Assert.Equal(Failure, Run.getStatus failedRun)
    Assert.Equal(Some errorMessage, Run.getErrorMessage failedRun)
    Assert.True((Run.getCompletedAt failedRun).IsSome)
    Assert.Equal(None, Run.getResponse failedRun)

[<Fact>]
let ``Run with invalid configuration should update status correctly`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let app = createTestApp ()

    let inputValues =
        [ { Title = "userId"; Value = "123" }
          { Title = "email"
            Value = "test@example.com" } ]

    let run = Run.createWithValidation actorUserId app inputValues |> unwrapResult
    let configError = "Resource not found"

    // Act
    let invalidRun = Run.markAsInvalidConfiguration actorUserId configError run

    // Assert
    Assert.Equal(InvalidConfiguration, Run.getStatus invalidRun)
    Assert.Equal(Some configError, Run.getErrorMessage invalidRun)
    Assert.True((Run.getCompletedAt invalidRun).IsSome)

[<Fact>]
let ``Run executable request composition should substitute input values`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let app, resource = createTestAppWithResource ()

    let inputValues =
        [ { Title = "userId"; Value = "123" }
          { Title = "email"
            Value = "john@example.com" } ]

    let run = Run.createWithValidation actorUserId app inputValues |> unwrapResult

    // Create a test current user for substitution
    let testCurrentUser: CurrentUser =
        { Id = "test-user-id"
          Email = "test@example.com"
          FirstName = "Test"
          LastName = "User" }

    // Act
    let result =
        Run.composeExecutableRequestFromAppAndResource run app resource testCurrentUser

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

// Input Type Validation Tests

[<Fact>]
let ``Run creation should validate Email input type with valid email`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let spaceId = SpaceId.FromGuid(Guid.NewGuid())
    let folderId = FolderId.NewId()

    let inputs =
        [ { Title = "userEmail"
            Type = InputType.Email()
            Required = true } ]

    let resource =
        Resource.create
            actorUserId
            spaceId
            "Test API"
            "Test endpoint"
            "https://api.test.com/users"
            []
            []
            [ "email", "{userEmail}" ]

        |> unwrapResult

    let app =
        App.createWithResource actorUserId "Test App" folderId resource HttpMethod.Get inputs (Some "/test") [] [] []
        |> unwrapResult

    let inputValues =
        [ { Title = "userEmail"
            Value = "test@example.com" } ]

    // Act
    let result = Run.createWithValidation actorUserId app inputValues

    // Assert
    match result with
    | Ok _ -> Assert.True(true)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Run creation should reject Email input type with invalid email`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let spaceId = SpaceId.FromGuid(Guid.NewGuid())
    let folderId = FolderId.NewId()

    let inputs =
        [ { Title = "userEmail"
            Type = InputType.Email()
            Required = true } ]

    let resource =
        Resource.create
            actorUserId
            spaceId
            "Test API"
            "Test endpoint"
            "https://api.test.com/users"
            []
            []
            [ "email", "{userEmail}" ]

        |> unwrapResult

    let app =
        App.createWithResource actorUserId "Test App" folderId resource HttpMethod.Get inputs (Some "/test") [] [] []
        |> unwrapResult

    let inputValues =
        [ { Title = "userEmail"
            Value = "invalid-email" } ]

    // Act
    let result = Run.createWithValidation actorUserId app inputValues

    // Assert
    match result with
    | Error(ValidationError message) -> Assert.Contains("Invalid email format", message)
    | _ -> Assert.True(false, "Expected validation error for invalid email")

[<Fact>]
let ``Run creation should validate Integer input type with valid integer`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let spaceId = SpaceId.FromGuid(Guid.NewGuid())
    let folderId = FolderId.NewId()

    let inputs =
        [ { Title = "age"
            Type = InputType.Integer()
            Required = true } ]

    let resource =
        Resource.create
            actorUserId
            spaceId
            "Test API"
            "Test endpoint"
            "https://api.test.com/users"
            []
            []
            [ "age", "{age}" ]

        |> unwrapResult

    let app =
        App.createWithResource actorUserId "Test App" folderId resource HttpMethod.Get inputs (Some "/test") [] [] []
        |> unwrapResult

    let inputValues = [ { Title = "age"; Value = "25" } ]

    // Act
    let result = Run.createWithValidation actorUserId app inputValues

    // Assert
    match result with
    | Ok _ -> Assert.True(true)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Run creation should reject Integer input type with invalid integer`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let spaceId = SpaceId.FromGuid(Guid.NewGuid())
    let folderId = FolderId.NewId()

    let inputs =
        [ { Title = "age"
            Type = InputType.Integer()
            Required = true } ]

    let resource =
        Resource.create
            actorUserId
            spaceId
            "Test API"
            "Test endpoint"
            "https://api.test.com/users"
            []
            []
            [ "age", "{age}" ]

        |> unwrapResult

    let app =
        App.createWithResource actorUserId "Test App" folderId resource HttpMethod.Get inputs (Some "/test") [] [] []
        |> unwrapResult

    let inputValues =
        [ { Title = "age"
            Value = "not-a-number" } ]

    // Act
    let result = Run.createWithValidation actorUserId app inputValues

    // Assert
    match result with
    | Error(ValidationError message) ->
        Assert.Contains("age", message)
        Assert.Contains("valid integer", message)
    | _ -> Assert.True(false, "Expected validation error for invalid integer")

[<Fact>]
let ``Run creation should validate Boolean input type with valid boolean`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let spaceId = SpaceId.FromGuid(Guid.NewGuid())
    let folderId = FolderId.NewId()

    let inputs =
        [ { Title = "isActive"
            Type = InputType.Boolean()
            Required = true } ]

    let resource =
        Resource.create
            actorUserId
            spaceId
            "Test API"
            "Test endpoint"
            "https://api.test.com/users"
            []
            []
            [ "active", "{isActive}" ]

        |> unwrapResult

    let app =
        App.createWithResource actorUserId "Test App" folderId resource HttpMethod.Get inputs (Some "/test") [] [] []
        |> unwrapResult

    let inputValues = [ { Title = "isActive"; Value = "true" } ]

    // Act
    let result = Run.createWithValidation actorUserId app inputValues

    // Assert
    match result with
    | Ok _ -> Assert.True(true)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Run creation should reject Boolean input type with invalid boolean`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let spaceId = SpaceId.FromGuid(Guid.NewGuid())
    let folderId = FolderId.NewId()

    let inputs =
        [ { Title = "isActive"
            Type = InputType.Boolean()
            Required = true } ]

    let resource =
        Resource.create
            actorUserId
            spaceId
            "Test API"
            "Test endpoint"
            "https://api.test.com/users"
            []
            []
            [ "active", "{isActive}" ]

        |> unwrapResult

    let app =
        App.createWithResource actorUserId "Test App" folderId resource HttpMethod.Get inputs (Some "/test") [] [] []
        |> unwrapResult

    let inputValues = [ { Title = "isActive"; Value = "maybe" } ]

    // Act
    let result = Run.createWithValidation actorUserId app inputValues

    // Assert
    match result with
    | Error(ValidationError message) ->
        Assert.Contains("isActive", message)
        Assert.Contains("valid boolean", message)
    | _ -> Assert.True(false, "Expected validation error for invalid boolean")

[<Fact>]
let ``Run creation should validate Date input type with valid date`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let spaceId = SpaceId.FromGuid(Guid.NewGuid())
    let folderId = FolderId.NewId()

    let inputs =
        [ { Title = "birthDate"
            Type = InputType.Date()
            Required = true } ]

    let resource =
        Resource.create
            actorUserId
            spaceId
            "Test API"
            "Test endpoint"
            "https://api.test.com/users"
            []
            []
            [ "birth_date", "{birthDate}" ]

        |> unwrapResult

    let app =
        App.createWithResource actorUserId "Test App" folderId resource HttpMethod.Get inputs (Some "/test") [] [] []
        |> unwrapResult

    let inputValues =
        [ { Title = "birthDate"
            Value = "2023-01-15" } ]

    // Act
    let result = Run.createWithValidation actorUserId app inputValues

    // Assert
    match result with
    | Ok _ -> Assert.True(true)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Run creation should reject Date input type with invalid date`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let spaceId = SpaceId.FromGuid(Guid.NewGuid())
    let folderId = FolderId.NewId()

    let inputs =
        [ { Title = "birthDate"
            Type = InputType.Date()
            Required = true } ]

    let resource =
        Resource.create
            actorUserId
            spaceId
            "Test API"
            "Test endpoint"
            "https://api.test.com/users"
            []
            []
            [ "birth_date", "{birthDate}" ]

        |> unwrapResult

    let app =
        App.createWithResource actorUserId "Test App" folderId resource HttpMethod.Get inputs (Some "/test") [] [] []
        |> unwrapResult

    let inputValues =
        [ { Title = "birthDate"
            Value = "not-a-date" } ]

    // Act
    let result = Run.createWithValidation actorUserId app inputValues

    // Assert
    match result with
    | Error(ValidationError message) ->
        Assert.Contains("birthDate", message)
        Assert.Contains("valid date", message)
    | _ -> Assert.True(false, "Expected validation error for invalid date")

[<Fact>]
let ``Run creation should validate Text input type within length limit`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let spaceId = SpaceId.FromGuid(Guid.NewGuid())
    let folderId = FolderId.NewId()

    let inputs =
        [ { Title = "description"
            Type = InputType.Text(50) |> unwrapResult
            Required = true } ]

    let resource =
        Resource.create
            actorUserId
            spaceId
            "Test API"
            "Test endpoint"
            "https://api.test.com/users"
            []
            []
            [ "desc", "{description}" ]

        |> unwrapResult

    let app =
        App.createWithResource actorUserId "Test App" folderId resource HttpMethod.Get inputs (Some "/test") [] [] []
        |> unwrapResult

    let inputValues =
        [ { Title = "description"
            Value = "Short description" } ]

    // Act
    let result = Run.createWithValidation actorUserId app inputValues

    // Assert
    match result with
    | Ok _ -> Assert.True(true)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Run creation should reject Text input type exceeding length limit`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let spaceId = SpaceId.FromGuid(Guid.NewGuid())
    let folderId = FolderId.NewId()

    let inputs =
        [ { Title = "description"
            Type = InputType.Text(10) |> unwrapResult
            Required = true } ]

    let resource =
        Resource.create
            actorUserId
            spaceId
            "Test API"
            "Test endpoint"
            "https://api.test.com/users"
            []
            []
            [ "desc", "{description}" ]

        |> unwrapResult

    let app =
        App.createWithResource actorUserId "Test App" folderId resource HttpMethod.Get inputs (Some "/test") [] [] []
        |> unwrapResult

    let inputValues =
        [ { Title = "description"
            Value = "This description is way too long for the limit" } ]

    // Act
    let result = Run.createWithValidation actorUserId app inputValues

    // Assert
    match result with
    | Error(ValidationError message) ->
        Assert.Contains("description", message)
        Assert.Contains("maximum length", message)
        Assert.Contains("10", message)
    | _ -> Assert.True(false, "Expected validation error for text exceeding length limit")

[<Fact>]
let ``Run creation should validate MultiText input type with valid choice`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let spaceId = SpaceId.FromGuid(Guid.NewGuid())
    let folderId = FolderId.NewId()

    let inputs =
        [ { Title = "priority"
            Type = InputType.MultiText(20, [ "high"; "medium"; "low" ]) |> unwrapResult
            Required = true } ]

    let resource =
        Resource.create
            actorUserId
            spaceId
            "Test API"
            "Test endpoint"
            "https://api.test.com/users"
            []
            []
            [ "priority", "{priority}" ]

        |> unwrapResult

    let app =
        App.createWithResource actorUserId "Test App" folderId resource HttpMethod.Get inputs (Some "/test") [] [] []
        |> unwrapResult

    let inputValues = [ { Title = "priority"; Value = "high" } ] // Valid text choice

    // Act
    let result = Run.createWithValidation actorUserId app inputValues

    // Assert
    match result with
    | Ok _ -> Assert.True(true)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Run creation should reject MultiText input type with invalid choice`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let spaceId = SpaceId.FromGuid(Guid.NewGuid())
    let folderId = FolderId.NewId()

    let inputs =
        [ { Title = "priority"
            Type = InputType.MultiText(20, [ "high"; "medium"; "low" ]) |> unwrapResult
            Required = true } ]

    let resource =
        Resource.create
            actorUserId
            spaceId
            "Test API"
            "Test endpoint"
            "https://api.test.com/users"
            []
            []
            [ "priority", "{priority}" ]

        |> unwrapResult

    let app =
        App.createWithResource actorUserId "Test App" folderId resource HttpMethod.Get inputs (Some "/test") [] [] []
        |> unwrapResult

    let inputValues =
        [ { Title = "priority"
            Value = "invalid-choice" } ]

    // Act
    let result = Run.createWithValidation actorUserId app inputValues

    // Assert
    match result with
    | Error(ValidationError message) ->
        Assert.Contains("priority", message)
        Assert.Contains("allowed text values", message)
    | _ -> Assert.True(false, "Expected validation error for invalid MultiText choice")
