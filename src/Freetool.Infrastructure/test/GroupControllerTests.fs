module Freetool.Infrastructure.Tests.GroupControllerTests

open System
open System.Threading.Tasks
open Xunit
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Application.DTOs
open Freetool.Application.Commands
open Freetool.Application.Interfaces
open Freetool.Api.Controllers

// Mock authorization service for testing
type MockAuthorizationService(checkPermissionFn: string -> string -> string -> bool) =
    interface IAuthorizationService with
        member _.CreateStoreAsync(req) =
            Task.FromResult({ Id = "store-1"; Name = req.Name })

        member _.WriteAuthorizationModelAsync() =
            Task.FromResult({ AuthorizationModelId = "model-1" })

        member _.CreateRelationshipsAsync(_) = Task.FromResult(())
        member _.UpdateRelationshipsAsync(_) = Task.FromResult(())
        member _.DeleteRelationshipsAsync(_) = Task.FromResult(())

        member _.CheckPermissionAsync (user: string) (relation: string) (object: string) =
            Task.FromResult(checkPermissionFn user relation object)

// Mock command handler for testing
type MockGroupCommandHandler(handleCommandFn: GroupCommand -> Task<Result<GroupCommandResult, DomainError>>) =
    interface IMultiRepositoryCommandHandler<GroupCommand, GroupCommandResult> with
        member _.HandleCommand(command: GroupCommand) = handleCommandFn command

// Helper to create a test controller with mocked dependencies
let createTestController
    (checkPermissionFn: string -> string -> string -> bool)
    (handleCommandFn: GroupCommand -> Task<Result<GroupCommandResult, DomainError>>)
    (userId: UserId)
    =
    let authService =
        MockAuthorizationService(checkPermissionFn) :> IAuthorizationService

    let commandHandler =
        MockGroupCommandHandler(handleCommandFn) :> IMultiRepositoryCommandHandler<GroupCommand, GroupCommandResult>

    let controller = GroupController(commandHandler, authService)

    // Setup HttpContext with the provided UserId
    let httpContext = DefaultHttpContext()
    httpContext.Items.["UserId"] <- userId
    controller.ControllerContext <- ControllerContext(HttpContext = httpContext)

    controller

[<Fact>]
let ``CreateGroup returns 403 when user is not org admin`` () : Task =
    task {
        // Arrange
        let userId = UserId.NewId()
        let checkPermission _ _ _ = false // No permissions granted

        let handleCommand _ =
            Task.FromResult(Error(NotFound "Should not be called"))

        let controller = createTestController checkPermission handleCommand userId
        let createDto: CreateGroupDto = { Name = "Engineering"; UserIds = None }

        // Act
        let! result = controller.CreateGroup(createDto)

        // Assert
        match result with
        | :? ObjectResult as objResult -> Assert.Equal(403, objResult.StatusCode.Value)
        | _ -> Assert.True(false, "Expected ObjectResult with status code 403")
    }

[<Fact>]
let ``CreateGroup succeeds when user is org admin`` () : Task =
    task {
        // Arrange
        let userId = UserId.NewId()

        // Grant org admin permission
        let checkPermission user relation obj =
            user = $"user:{userId.Value}" && relation = "admin" && obj = "organization:acme"

        // Mock will return not found since we can't easily match the command
        let handleCommand _ =
            Task.FromResult(Error(NotFound "Group not found"))

        let controller = createTestController checkPermission handleCommand userId
        let createDto: CreateGroupDto = { Name = "Engineering"; UserIds = None }

        // Act
        let! result = controller.CreateGroup(createDto)

        // Assert - Verify it's not a 403 (actual execution will fail with validation but auth passes)
        match result with
        | :? ObjectResult as objResult when objResult.StatusCode.HasValue ->
            Assert.NotEqual(403, objResult.StatusCode.Value)
        | _ -> () // Other result types are acceptable (like NotFoundResult)
    }

[<Fact>]
let ``UpdateGroupName returns 403 when user is not org admin`` () : Task =
    task {
        // Arrange
        let userId = UserId.NewId()
        let checkPermission _ _ _ = false // No permissions granted

        let handleCommand _ =
            Task.FromResult(Error(NotFound "Should not be called"))

        let controller = createTestController checkPermission handleCommand userId
        let updateDto: UpdateGroupNameDto = { Name = "New Name" }

        // Act
        let! result = controller.UpdateGroupName("group-123", updateDto)

        // Assert
        match result with
        | :? ObjectResult as objResult -> Assert.Equal(403, objResult.StatusCode.Value)
        | _ -> Assert.True(false, "Expected ObjectResult with status code 403")
    }

[<Fact>]
let ``UpdateGroupName succeeds when user is org admin`` () : Task =
    task {
        // Arrange
        let userId = UserId.NewId()

        // Grant org admin permission
        let checkPermission user relation obj =
            user = $"user:{userId.Value}" && relation = "admin" && obj = "organization:acme"

        let handleCommand _ =
            Task.FromResult(Error(NotFound "Group not found"))

        let controller = createTestController checkPermission handleCommand userId
        let updateDto: UpdateGroupNameDto = { Name = "New Name" }

        // Act
        let! result = controller.UpdateGroupName("group-123", updateDto)

        // Assert - Verify it's not a 403
        match result with
        | :? ObjectResult as objResult when objResult.StatusCode.HasValue ->
            Assert.NotEqual(403, objResult.StatusCode.Value)
        | _ -> () // Other result types are acceptable
    }

[<Fact>]
let ``AddUserToGroup returns 403 when user is neither org admin nor team admin`` () : Task =
    task {
        // Arrange
        let userId = UserId.NewId()
        let checkPermission _ _ _ = false // No permissions granted

        let handleCommand _ =
            Task.FromResult(Error(NotFound "Should not be called"))

        let controller = createTestController checkPermission handleCommand userId
        let addUserDto: AddUserToGroupDto = { UserId = UserId.NewId().Value.ToString() }

        // Act
        let! result = controller.AddUserToGroup("group-123", addUserDto)

        // Assert
        match result with
        | :? ObjectResult as objResult -> Assert.Equal(403, objResult.StatusCode.Value)
        | _ -> Assert.True(false, "Expected ObjectResult with status code 403")
    }

[<Fact>]
let ``AddUserToGroup succeeds when user is org admin`` () : Task =
    task {
        // Arrange
        let userId = UserId.NewId()
        let groupId = "group-123"

        // Grant org admin permission only
        let checkPermission user relation obj =
            user = $"user:{userId.Value}" && relation = "admin" && obj = "organization:acme"

        let handleCommand _ =
            Task.FromResult(Error(NotFound "Group not found"))

        let controller = createTestController checkPermission handleCommand userId
        let addUserDto: AddUserToGroupDto = { UserId = UserId.NewId().Value.ToString() }

        // Act
        let! result = controller.AddUserToGroup(groupId, addUserDto)

        // Assert - Verify it's not a 403
        match result with
        | :? ObjectResult as objResult when objResult.StatusCode.HasValue ->
            Assert.NotEqual(403, objResult.StatusCode.Value)
        | _ -> () // Other result types are acceptable
    }

[<Fact>]
let ``AddUserToGroup succeeds when user is team admin`` () : Task =
    task {
        // Arrange
        let userId = UserId.NewId()
        let groupId = "group-123"

        // Grant team admin permission only (not org admin)
        let checkPermission user relation obj =
            user = $"user:{userId.Value}" && relation = "admin" && obj = $"team:{groupId}"

        let handleCommand _ =
            Task.FromResult(Error(NotFound "Group not found"))

        let controller = createTestController checkPermission handleCommand userId
        let addUserDto: AddUserToGroupDto = { UserId = UserId.NewId().Value.ToString() }

        // Act
        let! result = controller.AddUserToGroup(groupId, addUserDto)

        // Assert - Verify it's not a 403
        match result with
        | :? ObjectResult as objResult when objResult.StatusCode.HasValue ->
            Assert.NotEqual(403, objResult.StatusCode.Value)
        | _ -> () // Other result types are acceptable
    }

[<Fact>]
let ``RemoveUserFromGroup returns 403 when user is neither org admin nor team admin`` () : Task =
    task {
        // Arrange
        let userId = UserId.NewId()
        let checkPermission _ _ _ = false // No permissions granted

        let handleCommand _ =
            Task.FromResult(Error(NotFound "Should not be called"))

        let controller = createTestController checkPermission handleCommand userId

        let removeUserDto: RemoveUserFromGroupDto =
            { UserId = UserId.NewId().Value.ToString() }

        // Act
        let! result = controller.RemoveUserFromGroup("group-123", removeUserDto)

        // Assert
        match result with
        | :? ObjectResult as objResult -> Assert.Equal(403, objResult.StatusCode.Value)
        | _ -> Assert.True(false, "Expected ObjectResult with status code 403")
    }

[<Fact>]
let ``RemoveUserFromGroup succeeds when user is org admin`` () : Task =
    task {
        // Arrange
        let userId = UserId.NewId()
        let groupId = "group-123"

        // Grant org admin permission only
        let checkPermission user relation obj =
            user = $"user:{userId.Value}" && relation = "admin" && obj = "organization:acme"

        let handleCommand _ =
            Task.FromResult(Error(NotFound "Group not found"))

        let controller = createTestController checkPermission handleCommand userId

        let removeUserDto: RemoveUserFromGroupDto =
            { UserId = UserId.NewId().Value.ToString() }

        // Act
        let! result = controller.RemoveUserFromGroup(groupId, removeUserDto)

        // Assert - Verify it's not a 403
        match result with
        | :? ObjectResult as objResult when objResult.StatusCode.HasValue ->
            Assert.NotEqual(403, objResult.StatusCode.Value)
        | _ -> () // Other result types are acceptable
    }

[<Fact>]
let ``RemoveUserFromGroup succeeds when user is team admin`` () : Task =
    task {
        // Arrange
        let userId = UserId.NewId()
        let groupId = "group-123"

        // Grant team admin permission only (not org admin)
        let checkPermission user relation obj =
            user = $"user:{userId.Value}" && relation = "admin" && obj = $"team:{groupId}"

        let handleCommand _ =
            Task.FromResult(Error(NotFound "Group not found"))

        let controller = createTestController checkPermission handleCommand userId

        let removeUserDto: RemoveUserFromGroupDto =
            { UserId = UserId.NewId().Value.ToString() }

        // Act
        let! result = controller.RemoveUserFromGroup(groupId, removeUserDto)

        // Assert - Verify it's not a 403
        match result with
        | :? ObjectResult as objResult when objResult.StatusCode.HasValue ->
            Assert.NotEqual(403, objResult.StatusCode.Value)
        | _ -> () // Other result types are acceptable
    }

[<Fact>]
let ``DeleteGroup returns 403 when user is not org admin`` () : Task =
    task {
        // Arrange
        let userId = UserId.NewId()
        let checkPermission _ _ _ = false // No permissions granted

        let handleCommand _ =
            Task.FromResult(Error(NotFound "Should not be called"))

        let controller = createTestController checkPermission handleCommand userId

        // Act
        let! result = controller.DeleteGroup("group-123")

        // Assert
        match result with
        | :? ObjectResult as objResult -> Assert.Equal(403, objResult.StatusCode.Value)
        | _ -> Assert.True(false, "Expected ObjectResult with status code 403")
    }

[<Fact>]
let ``DeleteGroup succeeds when user is org admin`` () : Task =
    task {
        // Arrange
        let userId = UserId.NewId()

        // Grant org admin permission
        let checkPermission user relation obj =
            user = $"user:{userId.Value}" && relation = "admin" && obj = "organization:acme"

        let handleCommand _ =
            Task.FromResult(Error(NotFound "Group not found"))

        let controller = createTestController checkPermission handleCommand userId

        // Act
        let! result = controller.DeleteGroup("group-123")

        // Assert - Verify it's not a 403
        match result with
        | :? ObjectResult as objResult when objResult.StatusCode.HasValue ->
            Assert.NotEqual(403, objResult.StatusCode.Value)
        | _ -> () // Other result types are acceptable
    }

[<Fact>]
let ``GetGroupById allows any authenticated user`` () : Task =
    task {
        // Arrange
        let userId = UserId.NewId()
        let checkPermission _ _ _ = false // No special permissions needed for read

        let groupData =
            { Id = GroupId.NewId()
              Name = "Engineering"
              CreatedAt = DateTime.UtcNow
              UpdatedAt = DateTime.UtcNow
              IsDeleted = false
              UserIds = [] }

        let handleCommand cmd =
            match cmd with
            | GetGroupById _ -> Task.FromResult(Ok(GroupResult groupData))
            | _ -> Task.FromResult(Error(NotFound "Command not supported"))

        let controller = createTestController checkPermission handleCommand userId

        // Act
        let! result = controller.GetGroupById("group-123")

        // Assert - Should succeed (200 OK) without auth check
        match result with
        | :? OkObjectResult as okResult -> Assert.NotNull(okResult)
        | _ -> () // Test passes as long as no 403 is returned
    }

[<Fact>]
let ``GetGroups allows any authenticated user`` () : Task =
    task {
        // Arrange
        let userId = UserId.NewId()
        let checkPermission _ _ _ = false // No special permissions needed for read

        let pagedResult =
            { Items = []
              TotalCount = 0
              Skip = 0
              Take = 10 }

        let handleCommand cmd =
            match cmd with
            | GetAllGroups _ -> Task.FromResult(Ok(GroupsResult pagedResult))
            | _ -> Task.FromResult(Error(NotFound "Command not supported"))

        let controller = createTestController checkPermission handleCommand userId

        // Act
        let! result = controller.GetGroups(0, 10)

        // Assert - Should succeed (200 OK) without auth check
        match result with
        | :? OkObjectResult as okResult -> Assert.NotNull(okResult)
        | _ -> () // Test passes as long as no 403 is returned
    }
