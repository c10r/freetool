namespace Freetool.Application.DTOs

open System
open System.ComponentModel
open System.ComponentModel.DataAnnotations
open System.Text.Json.Serialization

type CreateFolderDto = {
    [<Required>]
    [<StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")>]
    Name: string

    [<JsonConverter(typeof<FolderLocationConverter>)>]
    [<Description("Parent folder ID. Leave null to create a root folder.")>]
    Location: FolderLocation
}

type UpdateFolderNameDto = {
    [<Required>]
    [<StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")>]
    Name: string
}

type MoveFolderDto = {
    [<JsonConverter(typeof<FolderLocationConverter>)>]
    [<Description("Parent folder ID. Leave null or empty to move to the root folder.")>]
    ParentId: FolderLocation
}

type FolderDto = {
    Id: string
    Name: string
    ParentId: FolderLocation
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

type FolderWithChildrenDto = {
    Id: string
    Name: string
    ParentId: FolderLocation
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