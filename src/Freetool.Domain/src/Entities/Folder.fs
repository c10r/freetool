namespace Freetool.Domain.Entities

open System
open Freetool.Domain
open Freetool.Domain.ValueObjects

// Core folder data that's shared across all states
type FolderData = {
    Id: FolderId
    Name: FolderName
    ParentId: FolderId option
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

// Folder type parameterized by validation state
type Folder<'State> =
    | Folder of FolderData

    interface IEntity<FolderId> with
        member this.Id =
            let (Folder folderData) = this
            folderData.Id

// Type aliases for clarity
type UnvalidatedFolder = Folder<Unvalidated> // From DTOs - potentially unsafe
type ValidatedFolder = Folder<Validated> // Validated domain model and database data

module Folder =
    // Create a new validated folder from primitive values
    let create (name: string) (parentId: FolderId option) : Result<ValidatedFolder, DomainError> =
        match FolderName.Create(name) with
        | Error err -> Error err
        | Ok validName ->
            Ok(
                Folder {
                    Id = FolderId.NewId()
                    Name = validName
                    ParentId = parentId
                    CreatedAt = DateTime.UtcNow
                    UpdatedAt = DateTime.UtcNow
                }
            )

    // Business logic operations on validated folders
    let updateName (newName: string) (Folder folderData: ValidatedFolder) : Result<ValidatedFolder, DomainError> =
        match FolderName.Create(newName) with
        | Error err -> Error err
        | Ok validName ->
            Ok(
                Folder {
                    folderData with
                        Name = validName
                        UpdatedAt = DateTime.UtcNow
                }
            )

    let moveToParent (newParentId: FolderId option) (Folder folderData: ValidatedFolder) : ValidatedFolder =
        Folder {
            folderData with
                ParentId = newParentId
                UpdatedAt = DateTime.UtcNow
        }

    // Utility functions for accessing data from any state
    let getId (Folder folderData: Folder<'State>) : FolderId = folderData.Id

    let getName (Folder folderData: Folder<'State>) : string = folderData.Name.Value

    let getParentId (Folder folderData: Folder<'State>) : FolderId option = folderData.ParentId

    let getCreatedAt (Folder folderData: Folder<'State>) : DateTime = folderData.CreatedAt

    let getUpdatedAt (Folder folderData: Folder<'State>) : DateTime = folderData.UpdatedAt

    // Helper function to check if a folder is a root folder (no parent)
    let isRoot (folder: Folder<'State>) : bool = getParentId folder |> Option.isNone