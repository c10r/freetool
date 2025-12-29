module Freetool.Application.Tests.EventEnhancementServiceTests

open System
open System.Threading.Tasks
open System.Text.Json
open System.Text.Json.Serialization
open Xunit
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Domain.Events
open Freetool.Application.Interfaces
open Freetool.Application.Services

// Shared JSON options for serializing events (matches EventRepository)
let jsonOptions =
    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())
    options

// Mock implementations for repositories
type MockUserRepository() =
    interface IUserRepository with
        member _.GetByIdAsync(userId: UserId) = task { return None }
        member _.GetByEmailAsync(email: Email) = task { return None }
        member _.GetAllAsync skip take = task { return [] }
        member _.AddAsync(user: ValidatedUser) = task { return Ok user }
        member _.UpdateAsync(user: ValidatedUser) = task { return Ok user }
        member _.DeleteAsync(user: ValidatedUser) = task { return Ok() }
        member _.ExistsAsync(userId: UserId) = task { return false }
        member _.ExistsByEmailAsync(email: Email) = task { return false }
        member _.GetCountAsync() = task { return 0 }

type MockAppRepository() =
    interface IAppRepository with
        member _.GetByIdAsync(appId: AppId) = task { return None }
        member _.GetByNameAndFolderIdAsync name folderId = task { return None }
        member _.GetByFolderIdAsync folderId skip take = task { return [] }
        member _.GetAllAsync skip take = task { return [] }
        member _.GetByWorkspaceIdsAsync workspaceIds skip take = task { return [] }
        member _.AddAsync(app: ValidatedApp) = task { return Ok() }
        member _.UpdateAsync(app: ValidatedApp) = task { return Ok() }
        member _.DeleteAsync appId userId = task { return Ok() }
        member _.ExistsAsync(appId: AppId) = task { return false }
        member _.ExistsByNameAndFolderIdAsync name folderId = task { return false }
        member _.GetCountAsync() = task { return 0 }
        member _.GetCountByFolderIdAsync folderId = task { return 0 }
        member _.GetCountByWorkspaceIdsAsync workspaceIds = task { return 0 }
        member _.GetByResourceIdAsync resourceId = task { return [] }

type MockGroupRepository() =
    interface IGroupRepository with
        member _.GetByIdAsync(groupId: GroupId) = task { return None }
        member _.GetByNameAsync(name: string) = task { return None }
        member _.GetAllAsync skip take = task { return [] }
        member _.AddAsync(group: ValidatedGroup) = task { return Ok() }
        member _.UpdateAsync(group: ValidatedGroup) = task { return Ok() }
        member _.DeleteAsync(group: ValidatedGroup) = task { return Ok() }
        member _.ExistsAsync(groupId: GroupId) = task { return false }
        member _.ExistsByNameAsync(name: string) = task { return false }
        member _.GetCountAsync() = task { return 0 }
        member _.GetByUserIdAsync userId = task { return [] }

type MockFolderRepository() =
    interface IFolderRepository with
        member _.GetByIdAsync(folderId: FolderId) = task { return None }
        member _.GetChildrenAsync(folderId: FolderId) = task { return [] }
        member _.GetRootFoldersAsync skip take = task { return [] }
        member _.GetAllAsync skip take = task { return [] }
        member _.GetByWorkspaceAsync workspaceId skip take = task { return [] }
        member _.AddAsync(folder: ValidatedFolder) = task { return Ok() }
        member _.UpdateAsync(folder: ValidatedFolder) = task { return Ok() }
        member _.DeleteAsync(folder: ValidatedFolder) = task { return Ok() }
        member _.ExistsAsync(folderId: FolderId) = task { return false }
        member _.ExistsByNameInParentAsync name parentId = task { return false }
        member _.GetCountAsync() = task { return 0 }
        member _.GetCountByWorkspaceAsync workspaceId = task { return 0 }
        member _.GetRootCountAsync() = task { return 0 }
        member _.GetChildCountAsync parentId = task { return 0 }

type MockResourceRepository() =
    interface IResourceRepository with
        member _.GetByIdAsync(resourceId: ResourceId) = task { return None }
        member _.GetAllAsync skip take = task { return [] }
        member _.AddAsync(resource: ValidatedResource) = task { return Ok() }
        member _.UpdateAsync(resource: ValidatedResource) = task { return Ok() }
        member _.DeleteAsync(resource: ValidatedResource) = task { return Ok() }
        member _.ExistsAsync(resourceId: ResourceId) = task { return false }
        member _.ExistsByNameAsync(name: ResourceName) = task { return false }
        member _.GetCountAsync() = task { return 0 }

let createService () =
    EventEnhancementService(
        MockUserRepository(),
        MockAppRepository(),
        MockGroupRepository(),
        MockFolderRepository(),
        MockResourceRepository()
    )
    :> IEventEnhancementService

// Helper to create EventData from a domain event
let createEventData (event: 'T :> IDomainEvent) (eventType: EventType) (entityType: EntityType) (entityId: string) =
    let eventDataStr = JsonSerializer.Serialize(event, jsonOptions)

    { Id = Guid.NewGuid()
      EventId = (event :> IDomainEvent).EventId.ToString()
      EventType = eventType
      EntityType = entityType
      EntityId = entityId
      EventData = eventDataStr
      OccurredAt = (event :> IDomainEvent).OccurredAt
      CreatedAt = DateTime.UtcNow
      UserId = (event :> IDomainEvent).UserId }

[<Fact>]
let ``FolderCreatedEvent extracts name correctly`` () =
    task {
        let service = createService ()
        let folderId = FolderId.NewId()

        let folderName =
            FolderName.Create(Some "My Test Folder") |> Result.toOption |> Option.get

        let actorUserId = UserId.NewId()

        let event = FolderEvents.folderCreated actorUserId folderId folderName None

        let eventData =
            createEventData event (FolderEvents FolderCreatedEvent) EntityType.Folder (folderId.Value.ToString())

        let! enhanced = service.EnhanceEventAsync(eventData)

        Assert.Equal("My Test Folder", enhanced.EntityName)
        Assert.Contains("created folder", enhanced.EventSummary)
    }

[<Fact>]
let ``FolderDeletedEvent extracts name from event`` () =
    task {
        let service = createService ()
        let folderId = FolderId.NewId()

        let folderName =
            FolderName.Create(Some "Deleted Folder") |> Result.toOption |> Option.get

        let actorUserId = UserId.NewId()

        let event = FolderEvents.folderDeleted actorUserId folderId folderName

        let eventData =
            createEventData event (FolderEvents FolderDeletedEvent) EntityType.Folder (folderId.Value.ToString())

        let! enhanced = service.EnhanceEventAsync(eventData)

        Assert.Equal("Deleted Folder", enhanced.EntityName)
        Assert.Contains("deleted folder", enhanced.EventSummary)
    }

[<Fact>]
let ``ResourceDeletedEvent extracts name from event`` () =
    task {
        let service = createService ()
        let resourceId = ResourceId.NewId()

        let resourceName =
            ResourceName.Create(Some "My Resource") |> Result.toOption |> Option.get

        let actorUserId = UserId.NewId()

        let event = ResourceEvents.resourceDeleted actorUserId resourceId resourceName

        let eventData =
            createEventData
                event
                (ResourceEvents ResourceDeletedEvent)
                EntityType.Resource
                (resourceId.Value.ToString())

        let! enhanced = service.EnhanceEventAsync(eventData)

        Assert.Equal("My Resource", enhanced.EntityName)
        Assert.Contains("deleted resource", enhanced.EventSummary)
    }

[<Fact>]
let ``AppDeletedEvent extracts name from event`` () =
    task {
        let service = createService ()
        let appId = AppId.NewId()
        let appName = AppName.Create(Some "My App") |> Result.toOption |> Option.get
        let actorUserId = UserId.NewId()

        let event = AppEvents.appDeleted actorUserId appId appName

        let eventData =
            createEventData event (AppEvents AppDeletedEvent) EntityType.App (appId.Value.ToString())

        let! enhanced = service.EnhanceEventAsync(eventData)

        Assert.Equal("My App", enhanced.EntityName)
        Assert.Contains("deleted app", enhanced.EventSummary)
    }
