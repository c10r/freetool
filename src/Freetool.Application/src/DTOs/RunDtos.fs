namespace Freetool.Application.DTOs

open System
open System.ComponentModel.DataAnnotations

type RunInputDto = {
    [<Required>]
    [<StringLength(ValidationConstants.InputTitleMaxLength,
                   MinimumLength = ValidationConstants.InputTitleMinLength,
                   ErrorMessage = ValidationConstants.InputTitleErrorMessage)>]
    Title: string

    [<Required>]
    [<StringLength(ValidationConstants.InputValueMaxLength, ErrorMessage = ValidationConstants.InputValueErrorMessage)>]
    Value: string
}

type CreateRunDto = {
    [<Required>]
    InputValues: RunInputDto list
}

// Output DTOs
type ExecutableHttpRequestDto = {
    BaseUrl: string
    UrlParameters: KeyValuePairDto list
    Headers: KeyValuePairDto list
    Body: KeyValuePairDto list
    HttpMethod: string
}

type RunDto = {
    Id: string
    AppId: string
    Status: string
    InputValues: RunInputDto list
    ExecutableRequest: ExecutableHttpRequestDto option
    Response: string option
    ErrorMessage: string option
    StartedAt: DateTime option
    CompletedAt: DateTime option
    CreatedAt: DateTime
}

type PagedRunsDto = {
    Runs: RunDto list
    TotalCount: int
    Skip: int
    Take: int
}