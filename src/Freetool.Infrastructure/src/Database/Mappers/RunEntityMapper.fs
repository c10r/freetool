namespace Freetool.Infrastructure.Database.Mappers

open System
open System.Text.Json
open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Infrastructure.Database

type RunInputValueJson = { Title: string; Value: string }

type ExecutableHttpRequestJson = {
    BaseUrl: string
    UrlParameters: (string * string) list
    Headers: (string * string) list
    Body: (string * string) list
    HttpMethod: string
}

module RunEntityMapper =
    let runInputValueFromDomain (inputValue: RunInputValue) : RunInputValueJson = {
        Title = inputValue.Title
        Value = inputValue.Value
    }

    let runInputValueToDomain (inputValueJson: RunInputValueJson) : RunInputValue = {
        Title = inputValueJson.Title
        Value = inputValueJson.Value
    }

    let executableHttpRequestFromDomain (request: ExecutableHttpRequest) : ExecutableHttpRequestJson = {
        BaseUrl = request.BaseUrl
        UrlParameters = request.UrlParameters
        Headers = request.Headers
        Body = request.Body
        HttpMethod = request.HttpMethod
    }

    let executableHttpRequestToDomain (requestJson: ExecutableHttpRequestJson) : ExecutableHttpRequest = {
        BaseUrl = requestJson.BaseUrl
        UrlParameters = requestJson.UrlParameters
        Headers = requestJson.Headers
        Body = requestJson.Body
        HttpMethod = requestJson.HttpMethod
    }

    let toEntity (run: ValidatedRun) : RunEntity =
        let entity = RunEntity()
        entity.Id <- (Run.getId run).Value
        entity.AppId <- (Run.getAppId run).Value
        entity.Status <- (Run.getStatus run).ToString()

        let inputValuesJson = Run.getInputValues run |> List.map runInputValueFromDomain
        entity.InputValues <- JsonSerializer.Serialize(inputValuesJson)

        match Run.getExecutableRequest run with
        | Some execRequest ->
            let requestJson = executableHttpRequestFromDomain execRequest
            entity.ExecutableRequest <- JsonSerializer.Serialize(requestJson)
        | None -> entity.ExecutableRequest <- null

        entity.Response <- Run.getResponse run |> Option.defaultValue null
        entity.ErrorMessage <- Run.getErrorMessage run |> Option.defaultValue null

        match Run.getStartedAt run with
        | Some startedAt -> entity.StartedAt <- Nullable(startedAt)
        | None -> entity.StartedAt <- Nullable()

        match Run.getCompletedAt run with
        | Some completedAt -> entity.CompletedAt <- Nullable(completedAt)
        | None -> entity.CompletedAt <- Nullable()

        entity.CreatedAt <- Run.getCreatedAt run
        entity

    let fromEntity (entity: RunEntity) : ValidatedRun =
        let inputValuesJson =
            JsonSerializer.Deserialize<RunInputValueJson list>(entity.InputValues)

        let inputValues = inputValuesJson |> List.map runInputValueToDomain

        let executableRequest =
            if String.IsNullOrWhiteSpace(entity.ExecutableRequest) then
                None
            else
                let requestJson =
                    JsonSerializer.Deserialize<ExecutableHttpRequestJson>(entity.ExecutableRequest)

                Some(executableHttpRequestToDomain requestJson)

        let response =
            if String.IsNullOrEmpty(entity.Response) then
                None
            else
                Some entity.Response

        let errorMessage =
            if String.IsNullOrEmpty(entity.ErrorMessage) then
                None
            else
                Some entity.ErrorMessage

        let startedAt =
            if entity.StartedAt.HasValue then
                Some entity.StartedAt.Value
            else
                None

        let completedAt =
            if entity.CompletedAt.HasValue then
                Some entity.CompletedAt.Value
            else
                None

        let status =
            RunStatus.Create(entity.Status) |> Result.defaultValue RunStatus.Pending

        let runData = {
            Id = RunId.FromGuid(entity.Id)
            AppId = AppId.FromGuid(entity.AppId)
            Status = status
            InputValues = inputValues
            ExecutableRequest = executableRequest
            Response = response
            ErrorMessage = errorMessage
            StartedAt = startedAt
            CompletedAt = completedAt
            CreatedAt = entity.CreatedAt
        }

        Run.fromData runData