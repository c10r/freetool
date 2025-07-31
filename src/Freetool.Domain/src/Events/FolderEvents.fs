namespace Freetool.Domain.Events

open System
open Freetool.Domain
open Freetool.Domain.ValueObjects

type FolderChange =
    | NameChanged of oldValue: FolderName * newValue: FolderName
    | ParentChanged of oldParentId: FolderId option * newParentId: FolderId option

type FolderCreatedEvent = {
    FolderId: FolderId
    Name: FolderName
    ParentId: FolderId option
    OccurredAt: DateTime
    EventId: Guid
} with

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId

type FolderUpdatedEvent = {
    FolderId: FolderId
    Changes: FolderChange list
    OccurredAt: DateTime
    EventId: Guid
} with

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId

type FolderDeletedEvent = {
    FolderId: FolderId
    OccurredAt: DateTime
    EventId: Guid
} with

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId

module FolderEvents =
    let folderCreated (folderId: FolderId) (name: FolderName) (parentId: FolderId option) =
        {
            FolderId = folderId
            Name = name
            ParentId = parentId
            OccurredAt = DateTime.UtcNow
            EventId = Guid.NewGuid()
        }
        : FolderCreatedEvent

    let folderUpdated (folderId: FolderId) (changes: FolderChange list) =
        {
            FolderId = folderId
            Changes = changes
            OccurredAt = DateTime.UtcNow
            EventId = Guid.NewGuid()
        }
        : FolderUpdatedEvent

    let folderDeleted (folderId: FolderId) =
        {
            FolderId = folderId
            OccurredAt = DateTime.UtcNow
            EventId = Guid.NewGuid()
        }
        : FolderDeletedEvent