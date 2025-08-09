module Freetool.Domain.Tests.FolderTests

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
let ``Folder creation should generate FolderCreatedEvent`` () =
    // Arrange & Act
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let result = Folder.create actorUserId "My Documents" None

    // Assert
    match result with
    | Ok folder ->
        let events = Folder.getUncommittedEvents folder
        Assert.Single(events) |> ignore

        match events.[0] with
        | :? FolderCreatedEvent as event ->
            Assert.Equal("My Documents", event.Name.Value)
            Assert.Equal(None, event.ParentId)
        | _ -> Assert.True(false, "Expected FolderCreatedEvent")
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Folder creation with parent should generate correct event`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let parentId = FolderId.NewId()

    // Act
    let result = Folder.create actorUserId "Sub Folder" (Some parentId)

    // Assert
    match result with
    | Ok folder ->
        let events = Folder.getUncommittedEvents folder
        Assert.Single(events) |> ignore

        match events.[0] with
        | :? FolderCreatedEvent as event ->
            Assert.Equal("Sub Folder", event.Name.Value)
            Assert.Equal(Some parentId, event.ParentId)
        | _ -> Assert.True(false, "Expected FolderCreatedEvent")
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Folder name update should generate correct event`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let folder = Folder.create actorUserId "Old Folder Name" None |> unwrapResult

    // Act
    let result = Folder.updateName actorUserId "New Folder Name" folder

    // Assert
    match result with
    | Ok updatedFolder ->
        let events = Folder.getUncommittedEvents updatedFolder
        Assert.Equal(2, events.Length) // Creation event + update event

        match events.[1] with // The update event is the second one
        | :? FolderUpdatedEvent as event ->
            Assert.Single(event.Changes) |> ignore

            match event.Changes.[0] with
            | FolderChange.NameChanged(oldValue, newValue) ->
                Assert.Equal("Old Folder Name", oldValue.Value)
                Assert.Equal("New Folder Name", newValue.Value)
            | _ -> Assert.True(false, "Expected NameChanged event")
        | _ -> Assert.True(false, "Expected FolderUpdatedEvent")
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Folder move to parent should generate correct event`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let folder = Folder.create actorUserId "Mobile Folder" None |> unwrapResult
    let newParentId = FolderId.NewId()

    // Act
    let movedFolder = Folder.moveToParent actorUserId (Some newParentId) folder

    // Assert
    let events = Folder.getUncommittedEvents movedFolder
    Assert.Equal(2, events.Length) // Creation event + update event

    match events.[1] with // The update event is the second one
    | :? FolderUpdatedEvent as event ->
        Assert.Single(event.Changes) |> ignore

        match event.Changes.[0] with
        | FolderChange.ParentChanged(oldParentId, newParentIdValue) ->
            Assert.Equal(None, oldParentId)
            Assert.Equal(Some newParentId, newParentIdValue)
        | _ -> Assert.True(false, "Expected ParentChanged event")
    | _ -> Assert.True(false, "Expected FolderUpdatedEvent")

[<Fact>]
let ``Folder move to root should generate correct event`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let parentId = FolderId.NewId()

    let folder =
        Folder.create actorUserId "Child Folder" (Some parentId) |> unwrapResult

    // Act
    let movedFolder = Folder.moveToParent actorUserId None folder

    // Assert
    let events = Folder.getUncommittedEvents movedFolder
    Assert.Equal(2, events.Length) // Creation event + update event

    match events.[1] with // The update event is the second one
    | :? FolderUpdatedEvent as event ->
        Assert.Single(event.Changes) |> ignore

        match event.Changes.[0] with
        | FolderChange.ParentChanged(oldParentId, newParentIdValue) ->
            Assert.Equal(Some parentId, oldParentId)
            Assert.Equal(None, newParentIdValue)
        | _ -> Assert.True(false, "Expected ParentChanged event")
    | _ -> Assert.True(false, "Expected FolderUpdatedEvent")

[<Fact>]
let ``Folder creation should reject empty name`` () =
    // Act
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let result = Folder.create actorUserId "" None

    // Assert
    match result with
    | Error(ValidationError msg) -> Assert.Equal("Folder name cannot be empty", msg)
    | _ -> Assert.True(false, "Expected validation error for empty name")

[<Fact>]
let ``Folder creation should reject name longer than 100 characters`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let longName = String.replicate 101 "a"

    // Act
    let result = Folder.create actorUserId longName None

    // Assert
    match result with
    | Error(ValidationError msg) -> Assert.Equal("Folder name cannot exceed 100 characters", msg)
    | _ -> Assert.True(false, "Expected validation error for long name")

[<Fact>]
let ``Folder deletion should generate FolderDeletedEvent`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let folder = Folder.create actorUserId "Test Folder" None |> unwrapResult

    // Act
    let deletedFolder = Folder.markForDeletion actorUserId folder

    // Assert
    let events = Folder.getUncommittedEvents deletedFolder
    Assert.Equal(2, events.Length) // Creation event + deletion event

    match events.[1] with // The deletion event is the second one
    | :? FolderDeletedEvent as event -> Assert.Equal(folder.State.Id, event.FolderId)
    | _ -> Assert.True(false, "Expected FolderDeletedEvent")

[<Fact>]
let ``Root folder should be identified correctly`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let rootFolder = Folder.create actorUserId "Root Folder" None |> unwrapResult
    let parentId = FolderId.NewId()

    let childFolder =
        Folder.create actorUserId "Child Folder" (Some parentId) |> unwrapResult

    // Act & Assert
    Assert.True(Folder.isRoot rootFolder)
    Assert.False(Folder.isRoot childFolder)