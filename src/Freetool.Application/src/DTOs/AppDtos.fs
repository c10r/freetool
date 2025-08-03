namespace Freetool.Application.DTOs

open System
open System.ComponentModel.DataAnnotations

// Input DTOs
type InputTypeDto =
    | Email
    | Date
    | Text of MaxLength: int
    | Integer
    | Boolean
    | MultiChoice of Choices: InputTypeDto list

type InputDto = {
    [<Required>]
    [<StringLength(255, MinimumLength = 1, ErrorMessage = "Input title must be between 1 and 255 characters")>]
    Title: string

    [<Required>]
    Type: InputTypeDto

    [<Required>]
    Required: bool
}

// App DTOs
type CreateAppDto = {
    [<Required>]
    [<StringLength(100, MinimumLength = 1, ErrorMessage = "App name must be between 1 and 100 characters")>]
    Name: string

    [<Required>]
    FolderId: string

    [<Required>]
    ResourceId: string

    Inputs: InputDto list

    [<StringLength(500, ErrorMessage = "URL path cannot exceed 500 characters")>]
    UrlPath: string option

    UrlParameters: KeyValuePairDto list

    Headers: KeyValuePairDto list

    Body: KeyValuePairDto list
}

type UpdateAppNameDto = {
    [<Required>]
    [<StringLength(100, MinimumLength = 1, ErrorMessage = "App name must be between 1 and 100 characters")>]
    Name: string
}

type UpdateAppInputsDto = {
    [<Required>]
    Inputs: InputDto list
}

type AppDto = {
    Id: string
    Name: string
    FolderId: string
    ResourceId: string
    Inputs: InputDto list
    UrlPath: string option
    UrlParameters: KeyValuePairDto list
    Headers: KeyValuePairDto list
    Body: KeyValuePairDto list
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

type PagedAppsDto = {
    Apps: AppDto list
    TotalCount: int
    Skip: int
    Take: int
}