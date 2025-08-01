namespace Freetool.Application.DTOs

open System
open System.ComponentModel.DataAnnotations

type KeyValuePairDto = {
    [<Required>]
    [<StringLength(100, MinimumLength = 1, ErrorMessage = "Key must be between 1 and 100 characters")>]
    Key: string

    [<Required>]
    [<StringLength(1000, ErrorMessage = "Value cannot exceed 1000 characters")>]
    Value: string
}

type CreateResourceDto = {
    [<Required>]
    [<StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")>]
    Name: string

    [<Required>]
    [<StringLength(500, MinimumLength = 1, ErrorMessage = "Description must be between 1 and 500 characters")>]
    Description: string

    [<Required>]
    [<StringLength(1000, MinimumLength = 1, ErrorMessage = "Base URL must be between 1 and 1000 characters")>]
    [<Url(ErrorMessage = "Base URL must be a valid URL")>]
    BaseUrl: string

    [<Required>]
    [<StringLength(10, MinimumLength = 1, ErrorMessage = "HTTP method must be between 1 and 10 characters")>]
    HttpMethod: string

    UrlParameters: KeyValuePairDto list

    Headers: KeyValuePairDto list

    Body: KeyValuePairDto list
}

type UpdateResourceNameDto = {
    [<Required>]
    [<StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")>]
    Name: string
}

type UpdateResourceDescriptionDto = {
    [<Required>]
    [<StringLength(500, MinimumLength = 1, ErrorMessage = "Description must be between 1 and 500 characters")>]
    Description: string
}

type UpdateResourceBaseUrlDto = {
    [<Required>]
    [<StringLength(1000, MinimumLength = 1, ErrorMessage = "Base URL must be between 1 and 1000 characters")>]
    [<Url(ErrorMessage = "Base URL must be a valid URL")>]
    BaseUrl: string
}

type UpdateResourceUrlParametersDto = { UrlParameters: KeyValuePairDto list }

type UpdateResourceHeadersDto = { Headers: KeyValuePairDto list }

type UpdateResourceBodyDto = { Body: KeyValuePairDto list }

type ResourceDto = {
    Id: string
    Name: string
    Description: string
    BaseUrl: string
    HttpMethod: string
    UrlParameters: KeyValuePairDto list
    Headers: KeyValuePairDto list
    Body: KeyValuePairDto list
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

type PagedResourcesDto = {
    Resources: ResourceDto list
    TotalCount: int
    Skip: int
    Take: int
}