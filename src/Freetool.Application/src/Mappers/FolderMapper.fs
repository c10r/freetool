namespace Freetool.Application.Mappers

open System
open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Application.DTOs

module FolderMapper =

    let fromCreateDto (dto: CreateFolderDto) : Result<ValidatedFolder, DomainError> =
        let parentId =
            match dto.Location with
            | RootFolder -> None
            | ChildFolder parentId ->
                match Guid.TryParse(parentId) with
                | true, guid -> Some(FolderId.FromGuid(guid))
                | false, _ -> None // Will be validated in handler

        Folder.create dto.Name parentId

    let fromUpdateNameDto (dto: UpdateFolderNameDto) (folder: ValidatedFolder) : Result<ValidatedFolder, DomainError> =
        Folder.updateName dto.Name folder

    let fromMoveDto (dto: MoveFolderDto) (folder: ValidatedFolder) : ValidatedFolder =
        let parentId =
            match dto.ParentId with
            | RootFolder -> None
            | ChildFolder parentId ->
                match Guid.TryParse(parentId) with
                | true, guid -> Some(FolderId.FromGuid(guid))
                | false, _ -> None // Will be validated in handler

        Folder.moveToParent parentId folder

    // Domain -> DTO conversions (for API responses)
    let toDto (folder: ValidatedFolder) : FolderDto = {
        Id = (Folder.getId folder).Value.ToString()
        Name = Folder.getName folder
        ParentId =
            match Folder.getParentId folder with
            | Some parentId -> ChildFolder(parentId.Value.ToString())
            | None -> RootFolder
        CreatedAt = Folder.getCreatedAt folder
        UpdatedAt = Folder.getUpdatedAt folder
    }

    let toWithChildrenDto (folder: ValidatedFolder) (children: ValidatedFolder list) : FolderWithChildrenDto = {
        Id = (Folder.getId folder).Value.ToString()
        Name = Folder.getName folder
        ParentId =
            match Folder.getParentId folder with
            | Some parentId -> ChildFolder(parentId.Value.ToString())
            | None -> RootFolder
        Children = children |> List.map toDto
        CreatedAt = Folder.getCreatedAt folder
        UpdatedAt = Folder.getUpdatedAt folder
    }

    let toPagedDto (folders: ValidatedFolder list) (totalCount: int) (skip: int) (take: int) : PagedFoldersDto = {
        Folders = folders |> List.map toDto
        TotalCount = totalCount
        Skip = skip
        Take = take
    }