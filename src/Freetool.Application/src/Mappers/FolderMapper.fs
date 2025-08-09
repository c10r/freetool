namespace Freetool.Application.Mappers

open System
open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Application.DTOs

module FolderMapper =

    let fromCreateDto (actorUserId: UserId) (dto: CreateFolderDto) : Result<ValidatedFolder, DomainError> =
        let parentId =
            match dto.Location with
            | RootFolder -> None
            | ChildFolder parentId ->
                match Guid.TryParse(parentId) with
                | true, guid -> Some(FolderId.FromGuid(guid))
                | false, _ -> None // Will be validated in handler

        Folder.create actorUserId dto.Name parentId

    let fromUpdateNameDto
        (actorUserId: UserId)
        (dto: UpdateFolderNameDto)
        (folder: ValidatedFolder)
        : Result<ValidatedFolder, DomainError> =
        Folder.updateName actorUserId dto.Name folder

    let fromMoveDto (actorUserId: UserId) (dto: MoveFolderDto) (folder: ValidatedFolder) : ValidatedFolder =
        let parentId =
            match dto.ParentId with
            | RootFolder -> None
            | ChildFolder parentId ->
                match Guid.TryParse(parentId) with
                | true, guid -> Some(FolderId.FromGuid(guid))
                | false, _ -> None // Will be validated in handler

        Folder.moveToParent actorUserId parentId folder

    let toDataWithChildren (folder: ValidatedFolder) (children: ValidatedFolder list) : FolderData =
        // Since Children is now a computed property that returns None,
        // we just return the folder data and handle children separately
        folder.State