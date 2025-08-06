namespace Freetool.Domain.Entities

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open System.Text.Json.Serialization
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events

[<Table("Folders")>]
[<Index([| "Name"; "ParentId" |], IsUnique = true, Name = "IX_Folders_Name_ParentId")>]
type FolderData = {
    [<Key>]
    Id: FolderId

    [<Required>]
    [<MaxLength(100)>]
    Name: FolderName

    ParentId: FolderId option

    [<Required>]
    [<JsonIgnore>]
    CreatedAt: DateTime

    [<Required>]
    [<JsonIgnore>]
    UpdatedAt: DateTime

    [<NotMapped>]
    Children: FolderData list option

    [<JsonIgnore>]
    IsDeleted: bool
}

type Folder = EventSourcingAggregate<FolderData>

module FolderAggregateHelpers =
    let getEntityId (folder: Folder) : FolderId = folder.State.Id

    let implementsIEntity (folder: Folder) =
        { new IEntity<FolderId> with
            member _.Id = folder.State.Id
        }

type UnvalidatedFolder = Folder // From DTOs - potentially unsafe
type ValidatedFolder = Folder // Validated domain model and database data

module Folder =
    let fromData (folderData: FolderData) : ValidatedFolder = {
        State = folderData
        UncommittedEvents = []
    }

    let create (name: string) (parentId: FolderId option) : Result<ValidatedFolder, DomainError> =
        match FolderName.Create(Some name) with
        | Error err -> Error err
        | Ok validName ->
            let folderData = {
                Id = FolderId.NewId()
                Name = validName
                ParentId = parentId
                CreatedAt = DateTime.UtcNow
                UpdatedAt = DateTime.UtcNow
                Children = None
                IsDeleted = false
            }

            let folderCreatedEvent = FolderEvents.folderCreated folderData.Id validName parentId

            Ok {
                State = folderData
                UncommittedEvents = [ folderCreatedEvent :> IDomainEvent ]
            }

    let updateName (newName: string) (folder: ValidatedFolder) : Result<ValidatedFolder, DomainError> =
        match FolderName.Create(Some newName) with
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

    let markForDeletion (folder: ValidatedFolder) : ValidatedFolder =
        let folderDeletedEvent = FolderEvents.folderDeleted folder.State.Id

        {
            folder with
                UncommittedEvents = folder.UncommittedEvents @ [ folderDeletedEvent :> IDomainEvent ]
        }

    let getUncommittedEvents (folder: ValidatedFolder) : IDomainEvent list = folder.UncommittedEvents

    let markEventsAsCommitted (folder: ValidatedFolder) : ValidatedFolder = { folder with UncommittedEvents = [] }

    let getId (folder: Folder) : FolderId = folder.State.Id

    let getName (folder: Folder) : string = folder.State.Name.Value

    let getParentId (folder: Folder) : FolderId option = folder.State.ParentId

    let getCreatedAt (folder: Folder) : DateTime = folder.State.CreatedAt

    let getUpdatedAt (folder: Folder) : DateTime = folder.State.UpdatedAt

    let isRoot (folder: Folder) : bool = getParentId folder |> Option.isNone