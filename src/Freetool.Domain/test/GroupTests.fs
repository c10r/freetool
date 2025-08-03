module Freetool.Domain.Tests.GroupTests

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
let ``Group creation should generate GroupCreatedEvent`` () =
    // Act
    let result = Group.create "Development Team"

    // Assert
    match result with
    | Ok group ->
        let events = Group.getUncommittedEvents group
        Assert.Single(events) |> ignore

        match events.[0] with
        | :? GroupCreatedEvent as event ->
            Assert.Equal("Development Team", event.Name)
            Assert.Equal(group.State.Id, event.GroupId)
        | _ -> Assert.True(false, "Expected GroupCreatedEvent")

        // Verify group state
        Assert.Equal("Development Team", Group.getName group)
        Assert.Empty(Group.getUserIds group)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Group creation with empty name should fail`` () =
    // Act
    let result = Group.create ""

    // Assert
    match result with
    | Error(ValidationError message) -> Assert.Equal("Group name cannot be empty", message)
    | Ok _ -> Assert.True(false, "Expected validation error for empty name")
    | Error error -> Assert.True(false, $"Expected ValidationError but got: {error}")

[<Fact>]
let ``Group creation with name exceeding 100 characters should fail`` () =
    // Arrange
    let longName = String.replicate 101 "a"

    // Act
    let result = Group.create longName

    // Assert
    match result with
    | Error(ValidationError message) -> Assert.Equal("Group name cannot exceed 100 characters", message)
    | Ok _ -> Assert.True(false, "Expected validation error for long name")
    | Error error -> Assert.True(false, $"Expected ValidationError but got: {error}")

[<Fact>]
let ``Group validation should trim name and succeed`` () =
    // Arrange
    let group = {
        State = {
            Id = GroupId.NewId()
            Name = "  Marketing Team  "
            UserIds = []
            CreatedAt = DateTime.UtcNow
            UpdatedAt = DateTime.UtcNow
        }
        UncommittedEvents = []
    }

    // Act
    let result = Group.validate group

    // Assert
    match result with
    | Ok validatedGroup -> Assert.Equal("Marketing Team", validatedGroup.State.Name)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Group name update should generate correct event`` () =
    // Arrange
    let group = Group.create "Old Team Name" |> unwrapResult

    // Act
    let result = Group.updateName "New Team Name" group

    // Assert
    match result with
    | Ok updatedGroup ->
        let events = Group.getUncommittedEvents updatedGroup
        Assert.Equal(2, events.Length) // Creation event + update event

        match events.[1] with // The update event is the second one
        | :? GroupUpdatedEvent as event ->
            Assert.Single(event.Changes) |> ignore

            match event.Changes.[0] with
            | GroupChange.NameChanged(oldValue, newValue) ->
                Assert.Equal("Old Team Name", oldValue)
                Assert.Equal("New Team Name", newValue)
            | _ -> Assert.True(false, "Expected NameChanged event")
        | _ -> Assert.True(false, "Expected GroupUpdatedEvent")

        // Verify updated state
        Assert.Equal("New Team Name", Group.getName updatedGroup)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Group name update with same name should not generate event`` () =
    // Arrange
    let group = Group.create "Team Name" |> unwrapResult

    // Act
    let result = Group.updateName "Team Name" group

    // Assert
    match result with
    | Ok updatedGroup ->
        let events = Group.getUncommittedEvents updatedGroup
        Assert.Single(events) |> ignore // Only creation event, no update event
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Group name update with empty name should fail`` () =
    // Arrange
    let group = Group.create "Team Name" |> unwrapResult

    // Act
    let result = Group.updateName "" group

    // Assert
    match result with
    | Error(ValidationError message) -> Assert.Equal("Group name cannot be empty", message)
    | Ok _ -> Assert.True(false, "Expected validation error for empty name")
    | Error error -> Assert.True(false, $"Expected ValidationError but got: {error}")

[<Fact>]
let ``Add user to group should generate correct event`` () =
    // Arrange
    let group = Group.create "Development Team" |> unwrapResult
    let userId = UserId.NewId()

    // Act
    let result = Group.addUser userId group

    // Assert
    match result with
    | Ok updatedGroup ->
        let events = Group.getUncommittedEvents updatedGroup
        Assert.Equal(2, events.Length) // Creation event + update event

        match events.[1] with
        | :? GroupUpdatedEvent as event ->
            Assert.Single(event.Changes) |> ignore

            match event.Changes.[0] with
            | GroupChange.UserAdded(addedUserId) -> Assert.Equal(userId, addedUserId)
            | _ -> Assert.True(false, "Expected UserAdded event")
        | _ -> Assert.True(false, "Expected GroupUpdatedEvent")

        // Verify updated state
        let userIds = Group.getUserIds updatedGroup
        Assert.Single(userIds) |> ignore
        Assert.Equal(userId, userIds.[0])
        Assert.True(Group.hasUser userId updatedGroup)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Add duplicate user to group should fail`` () =
    // Arrange
    let group = Group.create "Development Team" |> unwrapResult
    let userId = UserId.NewId()
    let groupWithUser = Group.addUser userId group |> unwrapResult

    // Act
    let result = Group.addUser userId groupWithUser

    // Assert
    match result with
    | Error(Conflict message) -> Assert.Equal("User is already a member of this group", message)
    | Ok _ -> Assert.True(false, "Expected conflict error for duplicate user")
    | Error error -> Assert.True(false, $"Expected Conflict but got: {error}")

[<Fact>]
let ``Remove user from group should generate correct event`` () =
    // Arrange
    let group = Group.create "Development Team" |> unwrapResult
    let userId = UserId.NewId()
    let groupWithUser = Group.addUser userId group |> unwrapResult

    // Act
    let result = Group.removeUser userId groupWithUser

    // Assert
    match result with
    | Ok updatedGroup ->
        let events = Group.getUncommittedEvents updatedGroup
        Assert.Equal(3, events.Length) // Creation + add + remove events

        match events.[2] with
        | :? GroupUpdatedEvent as event ->
            Assert.Single(event.Changes) |> ignore

            match event.Changes.[0] with
            | GroupChange.UserRemoved(removedUserId) -> Assert.Equal(userId, removedUserId)
            | _ -> Assert.True(false, "Expected UserRemoved event")
        | _ -> Assert.True(false, "Expected GroupUpdatedEvent")

        // Verify updated state
        let userIds = Group.getUserIds updatedGroup
        Assert.Empty(userIds)
        Assert.False(Group.hasUser userId updatedGroup)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Remove non-member user from group should fail`` () =
    // Arrange
    let group = Group.create "Development Team" |> unwrapResult
    let userId = UserId.NewId()

    // Act
    let result = Group.removeUser userId group

    // Assert
    match result with
    | Error(NotFound message) -> Assert.Equal("User is not a member of this group", message)
    | Ok _ -> Assert.True(false, "Expected not found error for non-member user")
    | Error error -> Assert.True(false, $"Expected NotFound but got: {error}")

[<Fact>]
let ``Group with multiple users should track all members`` () =
    // Arrange
    let group = Group.create "Development Team" |> unwrapResult
    let user1 = UserId.NewId()
    let user2 = UserId.NewId()
    let user3 = UserId.NewId()

    // Act
    let groupWith1 = Group.addUser user1 group |> unwrapResult
    let groupWith2 = Group.addUser user2 groupWith1 |> unwrapResult
    let groupWith3 = Group.addUser user3 groupWith2 |> unwrapResult

    // Assert
    let userIds = Group.getUserIds groupWith3
    Assert.Equal(3, userIds.Length)
    Assert.Contains(user1, userIds)
    Assert.Contains(user2, userIds)
    Assert.Contains(user3, userIds)

    Assert.True(Group.hasUser user1 groupWith3)
    Assert.True(Group.hasUser user2 groupWith3)
    Assert.True(Group.hasUser user3 groupWith3)

    // Verify events
    let events = Group.getUncommittedEvents groupWith3
    Assert.Equal(4, events.Length) // 1 creation + 3 add events

[<Fact>]
let ``Mark group for deletion should generate GroupDeletedEvent`` () =
    // Arrange
    let group = Group.create "Development Team" |> unwrapResult

    // Act
    let groupForDeletion = Group.markForDeletion group

    // Assert
    let events = Group.getUncommittedEvents groupForDeletion
    Assert.Equal(2, events.Length) // Creation event + deletion event

    match events.[1] with
    | :? GroupDeletedEvent as event -> Assert.Equal(group.State.Id, event.GroupId)
    | _ -> Assert.True(false, "Expected GroupDeletedEvent")

[<Fact>]
let ``Group created from data should have no uncommitted events`` () =
    // Arrange
    let groupData = {
        Id = GroupId.NewId()
        Name = "Test Group"
        UserIds = []
        CreatedAt = DateTime.UtcNow
        UpdatedAt = DateTime.UtcNow
    }

    // Act
    let group = Group.fromData groupData

    // Assert
    let events = Group.getUncommittedEvents group
    Assert.Empty(events)
    Assert.Equal("Test Group", Group.getName group)

[<Fact>]
let ``Mark events as committed should clear uncommitted events`` () =
    // Arrange
    let group = Group.create "Development Team" |> unwrapResult
    let userId = UserId.NewId()
    let groupWithUser = Group.addUser userId group |> unwrapResult

    // Verify we have events before committing
    let eventsBefore = Group.getUncommittedEvents groupWithUser
    Assert.Equal(2, eventsBefore.Length)

    // Act
    let committedGroup = Group.markEventsAsCommitted groupWithUser

    // Assert
    let eventsAfter = Group.getUncommittedEvents committedGroup
    Assert.Empty(eventsAfter)

    // Verify state is preserved
    Assert.Equal("Development Team", Group.getName committedGroup)
    Assert.True(Group.hasUser userId committedGroup)