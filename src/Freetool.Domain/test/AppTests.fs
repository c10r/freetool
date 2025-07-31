module Freetool.Domain.Tests.AppTests

open System
open Xunit
open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events

// Helper function for test assertions
let unwrapResult result =
    match result with
    | Ok value -> value
    | Error error -> failwith $"Expected Ok but got Error: {error}"

[<Fact>]
let ``App creation should generate AppCreatedEvent`` () =
    // Arrange
    let folderId = FolderId.NewId()

    let inputs = [
        {
            Title = "Email"
            Type = InputType.Email()
        }
        {
            Title = "Password"
            Type = InputType.Text(50) |> Result.defaultValue (InputType.Email())
        }
    ]

    // Act
    let result = App.create "User Management App" folderId inputs

    // Assert
    match result with
    | Ok app ->
        let events = App.getUncommittedEvents app
        Assert.Single(events) |> ignore

        match events.[0] with
        | :? AppCreatedEvent as event ->
            Assert.Equal("User Management App", event.Name.Value)
            Assert.Equal(Some folderId, event.FolderId)
            Assert.Equal(2, event.Inputs.Length)
            Assert.Equal("Email", event.Inputs.[0].Title)
            Assert.Equal("Password", event.Inputs.[1].Title)
        | _ -> Assert.True(false, "Expected AppCreatedEvent")
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``App name update should generate correct event`` () =
    // Arrange
    let folderId = FolderId.NewId()
    let app = App.create "Old App Name" folderId [] |> unwrapResult

    // Act
    let result = App.updateName "New App Name" app

    // Assert
    match result with
    | Ok updatedApp ->
        let events = App.getUncommittedEvents updatedApp
        Assert.Equal(2, events.Length) // Creation event + update event

        match events.[1] with // The update event is the second one
        | :? AppUpdatedEvent as event ->
            Assert.Single(event.Changes) |> ignore

            match event.Changes.[0] with
            | AppChange.NameChanged(oldValue, newValue) ->
                Assert.Equal("Old App Name", oldValue.Value)
                Assert.Equal("New App Name", newValue.Value)
            | _ -> Assert.True(false, "Expected NameChanged event")
        | _ -> Assert.True(false, "Expected AppUpdatedEvent")
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``App inputs update should generate correct event`` () =
    // Arrange
    let folderId = FolderId.NewId()

    let initialInputs = [
        {
            Title = "Name"
            Type = InputType.Text(100) |> Result.defaultValue (InputType.Email())
        }
    ]

    let app = App.create "Test App" folderId initialInputs |> unwrapResult

    let newInputs = [
        {
            Title = "First Name"
            Type = InputType.Text(50) |> Result.defaultValue (InputType.Email())
        }
        {
            Title = "Last Name"
            Type = InputType.Text(50) |> Result.defaultValue (InputType.Email())
        }
        {
            Title = "Age"
            Type = InputType.Integer()
        }
    ]

    // Act
    let result = App.updateInputs newInputs app

    // Assert
    match result with
    | Ok updatedApp ->
        let events = App.getUncommittedEvents updatedApp
        Assert.Equal(2, events.Length) // Creation event + update event

        match events.[1] with // The update event is the second one
        | :? AppUpdatedEvent as event ->
            Assert.Single(event.Changes) |> ignore

            match event.Changes.[0] with
            | AppChange.InputsChanged(oldInputs, newInputs) ->
                Assert.Single(oldInputs) |> ignore
                Assert.Equal("Name", oldInputs.[0].Title)
                Assert.Equal(3, newInputs.Length)
                Assert.Equal("First Name", newInputs.[0].Title)
                Assert.Equal("Last Name", newInputs.[1].Title)
                Assert.Equal("Age", newInputs.[2].Title)
            | _ -> Assert.True(false, "Expected InputsChanged event")
        | _ -> Assert.True(false, "Expected AppUpdatedEvent")
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``App creation should reject empty name`` () =
    // Arrange
    let folderId = FolderId.NewId()

    // Act
    let result = App.create "" folderId []

    // Assert
    match result with
    | Error(ValidationError msg) -> Assert.Equal("App name cannot be empty", msg)
    | _ -> Assert.True(false, "Expected validation error for empty name")

[<Fact>]
let ``App creation should reject name longer than 100 characters`` () =
    // Arrange
    let folderId = FolderId.NewId()
    let longName = String.replicate 101 "a"

    // Act
    let result = App.create longName folderId []

    // Assert
    match result with
    | Error(ValidationError msg) -> Assert.Equal("App name cannot exceed 100 characters", msg)
    | _ -> Assert.True(false, "Expected validation error for long name")

[<Fact>]
let ``App deletion should generate AppDeletedEvent`` () =
    // Arrange
    let folderId = FolderId.NewId()
    let app = App.create "Test App" folderId [] |> unwrapResult

    // Act
    let deletedApp = App.markForDeletion app

    // Assert
    let events = App.getUncommittedEvents deletedApp
    Assert.Equal(2, events.Length) // Creation event + deletion event

    match events.[1] with // The deletion event is the second one
    | :? AppDeletedEvent as event -> Assert.Equal(app.State.Id, event.AppId)
    | _ -> Assert.True(false, "Expected AppDeletedEvent")

[<Fact>]
let ``App validation should reject invalid input title`` () =
    // Arrange
    let folderId = FolderId.NewId()
    let invalidInputs = [ { Title = ""; Type = InputType.Email() } ] // Empty title

    // Act
    let result = App.create "Test App" folderId invalidInputs

    // Assert
    match result with
    | Error(ValidationError msg) -> Assert.Equal("Input title cannot be empty", msg)
    | _ -> Assert.True(false, "Expected validation error for invalid input title")