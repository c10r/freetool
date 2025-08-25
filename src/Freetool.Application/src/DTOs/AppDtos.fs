namespace Freetool.Application.DTOs

open System.ComponentModel.DataAnnotations

type AppInputDto = {
    [<Required>]
    Input: InputDto

    [<Required>]
    Required: bool
}

type CreateAppDto = {
    [<Required>]
    [<StringLength(ValidationConstants.NameMaxLength,
                   MinimumLength = ValidationConstants.NameMinLength,
                   ErrorMessage = ValidationConstants.NameErrorMessage)>]
    Name: string

    [<Required>]
    FolderId: string

    [<Required>]
    ResourceId: string

    Inputs: AppInputDto list

    // Intentionally not moved to SharedDtos yet - this is only the first usage
    UrlPath: string option

    UrlParameters: KeyValuePairDto list

    Headers: KeyValuePairDto list

    Body: KeyValuePairDto list
}

type UpdateAppNameDto = {
    [<Required>]
    [<StringLength(ValidationConstants.NameMaxLength,
                   MinimumLength = ValidationConstants.NameMinLength,
                   ErrorMessage = ValidationConstants.NameErrorMessage)>]
    Name: string
}

type UpdateAppInputsDto = {
    [<Required>]
    Inputs: AppInputDto list
}

type UpdateAppQueryParametersDto = { UrlParameters: KeyValuePairDto list }

type UpdateAppBodyDto = { Body: KeyValuePairDto list }

type UpdateAppHeadersDto = { Headers: KeyValuePairDto list }

type UpdateAppUrlPathDto = { UrlPath: string option }