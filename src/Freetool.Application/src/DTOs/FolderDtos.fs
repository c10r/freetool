namespace Freetool.Application.DTOs

open System
open System.ComponentModel.DataAnnotations

type CreateFolderDto = {
    [<Required>]
    [<StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")>]
    Name: string

    ParentId: string // Optional parent folder ID - null/empty for root folders
}

type UpdateFolderNameDto = {
    [<Required>]
    [<StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")>]
    Name: string
}

type MoveFolderDto = {
    ParentId: string // Optional parent folder ID - null/empty to move to root
}

type FolderDto = {
    Id: string
    Name: string
    ParentId: string // null if root folder
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

type FolderWithChildrenDto = {
    Id: string
    Name: string
    ParentId: string // null if root folder
    Children: FolderDto list
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

type PagedFoldersDto = {
    Folders: FolderDto list
    TotalCount: int
    Skip: int
    Take: int
}