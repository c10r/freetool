namespace Freetool.Domain.Entities

open System
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events

// Core folder data that's shared across all states
type FolderData = {
    Id: FolderId
    Name: FolderName
    ParentId: FolderId option
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

// Folder type with event collection
type Folder = EventSourcingAggregate<FolderData>

module FolderAggregateHelpers =
    let getEntityId (folder: Folder) : FolderId = folder.State.Id

    let implementsIEntity (folder: Folder) =
        { new IEntity<FolderId> with
            member _.Id = folder.State.Id
        }

// Type aliases for clarity
type UnvalidatedFolder = Folder // From DTOs - potentially unsafe
type ValidatedFolder = Folder // Validated domain model and database data

module Folder =
    // Create folder from existing data without events (for loading from database)
    let fromData (folderData: FolderData) : ValidatedFolder = {
        State = folderData
        UncommittedEvents = []
    }

    // Create a new validated folder with events
    let create (name: string) (parentId: FolderId option) : Result<ValidatedFolder, DomainError> =
        match FolderName.Create(name) with
        | Error err -> Error err
        | Ok validName ->
            let folderData = {
                Id = FolderId.NewId()
                Name = validName
                ParentId = parentId
                CreatedAt = DateTime.UtcNow
                UpdatedAt = DateTime.UtcNow
            }

            let folderCreatedEvent = FolderEvents.folderCreated folderData.Id validName parentId

            Ok {
                State = folderData
                UncommittedEvents = [ folderCreatedEvent :> IDomainEvent ]
            }

    // Business logic operations on validated folders with event tracking
    let updateName (newName: string) (folder: ValidatedFolder) : Result<ValidatedFolder, DomainError> =
        match FolderName.Create(newName) with
        | Error err -> Error err
        | Ok validName ->
            let oldName = folder.State.Name

            let updatedFolderData = {
                folder.State with
                    Name = validName
                    UpdatedAt = DateTime.UtcNow
            }

            let nameChangedEvent =
                FolderEvents.folderUpdated folder.State.Id [ FolderChange.NameChanged(oldName, validName) ]

            Ok {
                State = updatedFolderData
                UncommittedEvents = folder.UncommittedEvents @ [ nameChangedEvent :> IDomainEvent ]
            }

    let moveToParent (newParentId: FolderId option) (folder: ValidatedFolder) : ValidatedFolder =
        let oldParentId = folder.State.ParentId

        let updatedFolderData = {
            folder.State with
                ParentId = newParentId
                UpdatedAt = DateTime.UtcNow
        }

        let parentChangedEvent =
            FolderEvents.folderUpdated folder.State.Id [ FolderChange.ParentChanged(oldParentId, newParentId) ]

        {
            State = updatedFolderData
            UncommittedEvents = folder.UncommittedEvents @ [ parentChangedEvent :> IDomainEvent ]
        }

    // Delete folder with event tracking
    let markForDeletion (folder: ValidatedFolder) : ValidatedFolder =
        let folderDeletedEvent = FolderEvents.folderDeleted folder.State.Id

        {
            folder with
                UncommittedEvents = folder.UncommittedEvents @ [ folderDeletedEvent :> IDomainEvent ]
        }

    // Event management functions
    let getUncommittedEvents (folder: ValidatedFolder) : IDomainEvent list = folder.UncommittedEvents

    let markEventsAsCommitted (folder: ValidatedFolder) : ValidatedFolder = { folder with UncommittedEvents = [] }

    // Utility functions for accessing data from any state
    let getId (folder: Folder) : FolderId = folder.State.Id

    let getName (folder: Folder) : string = folder.State.Name.Value

    let getParentId (folder: Folder) : FolderId option = folder.State.ParentId

    let getCreatedAt (folder: Folder) : DateTime = folder.State.CreatedAt

    let getUpdatedAt (folder: Folder) : DateTime = folder.State.UpdatedAt

    // Helper function to check if a folder is a root folder (no parent)
    let isRoot (folder: Folder) : bool = getParentId folder |> Option.isNone