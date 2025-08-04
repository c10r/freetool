namespace Freetool.Application.DTOs

open System
open System.ComponentModel.DataAnnotations

// Input DTOs
type RunInputValueDto = {
    [<Required>]
    [<StringLength(255, MinimumLength = 1, ErrorMessage = "Input title must be between 1 and 255 characters")>]
    Title: string

    [<Required>]
    Value: string
}

type CreateRunDto = {
    [<Required>]
    InputValues: RunInputValueDto list
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
    InputValues: RunInputValueDto list
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