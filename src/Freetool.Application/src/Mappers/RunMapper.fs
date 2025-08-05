namespace Freetool.Application.Mappers

open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Application.DTOs

module RunMapper =
    let runInputValueFromDto (dto: RunInputDto) : RunInputValue = { Title = dto.Title; Value = dto.Value }

    let runInputValueToDto (inputValue: RunInputValue) : RunInputDto = {
        Title = inputValue.Title
        Value = inputValue.Value
    }

    let executableHttpRequestToDto (request: ExecutableHttpRequest) : ExecutableHttpRequestDto = {
        BaseUrl = request.BaseUrl
        UrlParameters = request.UrlParameters |> List.map (fun (k, v) -> { Key = k; Value = v })
        Headers = request.Headers |> List.map (fun (k, v) -> { Key = k; Value = v })
        Body = request.Body |> List.map (fun (k, v) -> { Key = k; Value = v })
        HttpMethod = request.HttpMethod
    }

    let fromCreateDto (dto: CreateRunDto) : RunInputValue list =
        dto.InputValues |> List.map runInputValueFromDto

    let toDto (run: ValidatedRun) : RunDto = {
        Id = (Run.getId run).ToString()
        AppId = (Run.getAppId run).ToString()
        Status = (Run.getStatus run).ToString()
        InputValues = Run.getInputValues run |> List.map runInputValueToDto
        ExecutableRequest = Run.getExecutableRequest run |> Option.map executableHttpRequestToDto
        Response = Run.getResponse run
        ErrorMessage = Run.getErrorMessage run
        StartedAt = Run.getStartedAt run
        CompletedAt = Run.getCompletedAt run
        CreatedAt = Run.getCreatedAt run
    }