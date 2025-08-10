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
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())

    // Act
    let result = Group.create actorUserId "Development Team" None

    // Assert
    match result with
    | Ok group ->
        let events = Group.getUncommittedEvents group
        Assert.Single(events) |> ignore

        match events.[0] with
        | :? GroupCreatedEvent as event ->
            Assert.Equal("Development Team", event.Name)
            Assert.Equal(group.State.Id, event.GroupId)
            Assert.Empty(event.InitialUserIds) // No users for this test
        | _ -> Assert.True(false, "Expected GroupCreatedEvent")

        // Verify group state
        Assert.Equal("Development Team", Group.getName group)
        Assert.Empty(Group.getUserIds group)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Group creation with empty name should fail`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())

    // Act
    let result = Group.create actorUserId "" None

    // Assert
    match result with
    | Error(ValidationError message) -> Assert.Equal("Group name cannot be empty", message)
    | Ok _ -> Assert.True(false, "Expected validation error for empty name")
    | Error error -> Assert.True(false, $"Expected ValidationError but got: {error}")

[<Fact>]
let ``Group creation with name exceeding 100 characters should fail`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let longName = String.replicate 101 "a"

    // Act
    let result = Group.create actorUserId longName None

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
            CreatedAt = DateTime.UtcNow
            UpdatedAt = DateTime.UtcNow
            IsDeleted = false
            UserIds = []
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
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let group = Group.create actorUserId "Old Team Name" None |> unwrapResult

    // Act
    let result = Group.updateName actorUserId "New Team Name" group

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
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let group = Group.create actorUserId "Team Name" None |> unwrapResult

    // Act
    let result = Group.updateName actorUserId "Team Name" group

    // Assert
    match result with
    | Ok updatedGroup ->
        let events = Group.getUncommittedEvents updatedGroup
        Assert.Single(events) |> ignore // Only creation event, no update event
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Group name update with empty name should fail`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let group = Group.create actorUserId "Team Name" None |> unwrapResult

    // Act
    let result = Group.updateName actorUserId "" group

    // Assert
    match result with
    | Error(ValidationError message) -> Assert.Equal("Group name cannot be empty", message)
    | Ok _ -> Assert.True(false, "Expected validation error for empty name")
    | Error error -> Assert.True(false, $"Expected ValidationError but got: {error}")

[<Fact>]
let ``Add user to group should generate correct event`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let group = Group.create actorUserId "Development Team" None |> unwrapResult
    let userId = UserId.NewId()

    // Act
    let result = Group.addUser actorUserId userId group

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
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let group = Group.create actorUserId "Development Team" None |> unwrapResult
    let userId = UserId.NewId()
    let groupWithUser = Group.addUser actorUserId userId group |> unwrapResult

    // Act
    let result = Group.addUser actorUserId userId groupWithUser

    // Assert
    match result with
    | Error(Conflict message) -> Assert.Equal("User is already a member of this group", message)
    | Ok _ -> Assert.True(false, "Expected conflict error for duplicate user")
    | Error error -> Assert.True(false, $"Expected Conflict but got: {error}")

[<Fact>]
let ``Remove user from group should generate correct event`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let group = Group.create actorUserId "Development Team" None |> unwrapResult
    let userId = UserId.NewId()
    let groupWithUser = Group.addUser actorUserId userId group |> unwrapResult

    // Act
    let result = Group.removeUser actorUserId userId groupWithUser

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
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let group = Group.create actorUserId "Development Team" None |> unwrapResult
    let userId = UserId.NewId()

    // Act
    let result = Group.removeUser actorUserId userId group

    // Assert
    match result with
    | Error(NotFound message) -> Assert.Equal("User is not a member of this group", message)
    | Ok _ -> Assert.True(false, "Expected not found error for non-member user")
    | Error error -> Assert.True(false, $"Expected NotFound but got: {error}")

[<Fact>]
let ``Group with multiple users should track all members`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let group = Group.create actorUserId "Development Team" None |> unwrapResult
    let user1 = UserId.NewId()
    let user2 = UserId.NewId()
    let user3 = UserId.NewId()

    // Act
    let groupWith1 = Group.addUser actorUserId user1 group |> unwrapResult
    let groupWith2 = Group.addUser actorUserId user2 groupWith1 |> unwrapResult
    let groupWith3 = Group.addUser actorUserId user3 groupWith2 |> unwrapResult

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
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let group = Group.create actorUserId "Development Team" None |> unwrapResult

    // Act
    let groupForDeletion = Group.markForDeletion actorUserId group

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
        CreatedAt = DateTime.UtcNow
        UpdatedAt = DateTime.UtcNow
        IsDeleted = false
        UserIds = []
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
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let group = Group.create actorUserId "Development Team" None |> unwrapResult
    let userId = UserId.NewId()
    let groupWithUser = Group.addUser actorUserId userId group |> unwrapResult

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

[<Fact>]
let ``Group creation with single user should succeed`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let userId = UserId.NewId()
    let userIds = Some [ userId ]

    // Act
    let result = Group.create actorUserId "Development Team" userIds

    // Assert
    match result with
    | Ok group ->
        let events = Group.getUncommittedEvents group
        Assert.Single(events) |> ignore

        match events.[0] with
        | :? GroupCreatedEvent as event ->
            Assert.Equal("Development Team", event.Name)
            Assert.Equal(group.State.Id, event.GroupId)
            Assert.Single(event.InitialUserIds) |> ignore
            Assert.Equal(userId, event.InitialUserIds.[0])
        | _ -> Assert.True(false, "Expected GroupCreatedEvent")

        // Verify group state
        Assert.Equal("Development Team", Group.getName group)
        let groupUserIds = Group.getUserIds group
        Assert.Single(groupUserIds) |> ignore
        Assert.Equal(userId, groupUserIds.[0])
        Assert.True(Group.hasUser userId group)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Group creation with multiple users should succeed`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let user1 = UserId.NewId()
    let user2 = UserId.NewId()
    let user3 = UserId.NewId()
    let userIds = Some [ user1; user2; user3 ]

    // Act
    let result = Group.create actorUserId "Marketing Team" userIds

    // Assert
    match result with
    | Ok group ->
        // Verify group state
        Assert.Equal("Marketing Team", Group.getName group)
        let groupUserIds = Group.getUserIds group
        Assert.Equal(3, groupUserIds.Length)
        Assert.Contains(user1, groupUserIds)
        Assert.Contains(user2, groupUserIds)
        Assert.Contains(user3, groupUserIds)

        Assert.True(Group.hasUser user1 group)
        Assert.True(Group.hasUser user2 group)
        Assert.True(Group.hasUser user3 group)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Group creation with duplicate users should remove duplicates`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let user1 = UserId.NewId()
    let user2 = UserId.NewId()
    let userIds = Some [ user1; user2; user1; user2; user1 ] // Duplicates

    // Act
    let result = Group.create actorUserId "QA Team" userIds

    // Assert
    match result with
    | Ok group ->
        // Verify duplicates are removed
        let groupUserIds = Group.getUserIds group
        Assert.Equal(2, groupUserIds.Length) // Should only have 2 unique users
        Assert.Contains(user1, groupUserIds)
        Assert.Contains(user2, groupUserIds)

        Assert.True(Group.hasUser user1 group)
        Assert.True(Group.hasUser user2 group)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Group creation with empty user list should succeed`` () =
    // Arrange
    let actorUserId = UserId.FromGuid(Guid.NewGuid())
    let userIds = Some []

    // Act
    let result = Group.create actorUserId "Empty Team" userIds

    // Assert
    match result with
    | Ok group ->
        Assert.Equal("Empty Team", Group.getName group)
        let groupUserIds = Group.getUserIds group
        Assert.Empty(groupUserIds)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Group validation with duplicate UserIds should remove duplicates`` () =
    // Arrange
    let user1 = UserId.NewId()
    let user2 = UserId.NewId()

    let group = {
        State = {
            Id = GroupId.NewId()
            Name = "Test Team"
            CreatedAt = DateTime.UtcNow
            UpdatedAt = DateTime.UtcNow
            IsDeleted = false
            UserIds = [ user1; user2; user1; user2 ] // Duplicates for testing
        }
        UncommittedEvents = []
    }

    // Act
    let result = Group.validate group

    // Assert
    match result with
    | Ok validatedGroup ->
        let userIds = Group.getUserIds validatedGroup
        Assert.Equal(2, userIds.Length) // Should only have 2 unique users
        Assert.Contains(user1, userIds)
        Assert.Contains(user2, userIds)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Group validateUserIds with valid users should succeed`` () =
    // Arrange
    let user1 = UserId.NewId()
    let user2 = UserId.NewId()
    let userIds = [ user1; user2 ]
    let userExistsFunc userId = true // All users exist

    // Act
    let result = Group.validateUserIds userIds userExistsFunc

    // Assert
    match result with
    | Ok validatedIds ->
        Assert.Equal(2, validatedIds.Length)
        Assert.Contains(user1, validatedIds)
        Assert.Contains(user2, validatedIds)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Group validateUserIds with invalid users should fail`` () =
    // Arrange
    let user1 = UserId.NewId()
    let user2 = UserId.NewId()
    let user3 = UserId.NewId()
    let userIds = [ user1; user2; user3 ]

    // user2 and user3 don't exist
    let userExistsFunc userId = userId = user1

    // Act
    let result = Group.validateUserIds userIds userExistsFunc

    // Assert
    match result with
    | Error(ValidationError message) ->
        Assert.Contains(user2.Value.ToString(), message)
        Assert.Contains(user3.Value.ToString(), message)
        Assert.Contains("do not exist or are deleted", message)
    | Ok _ -> Assert.True(false, "Expected validation error for invalid users")
    | Error error -> Assert.True(false, $"Expected ValidationError but got: {error}")

[<Fact>]
let ``Group validateUserIds with duplicate users should remove duplicates`` () =
    // Arrange
    let user1 = UserId.NewId()
    let user2 = UserId.NewId()
    let userIds = [ user1; user2; user1; user2 ] // Duplicates
    let userExistsFunc userId = true // All users exist

    // Act
    let result = Group.validateUserIds userIds userExistsFunc

    // Assert
    match result with
    | Ok validatedIds ->
        Assert.Equal(2, validatedIds.Length) // Should only have 2 unique users
        Assert.Contains(user1, validatedIds)
        Assert.Contains(user2, validatedIds)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")

[<Fact>]
let ``Group validateUserIds with empty list should succeed`` () =
    // Arrange
    let userIds = []
    let userExistsFunc userId = true

    // Act
    let result = Group.validateUserIds userIds userExistsFunc

    // Assert
    match result with
    | Ok validatedIds -> Assert.Empty(validatedIds)
    | Error error -> Assert.True(false, $"Expected success but got error: {error}")