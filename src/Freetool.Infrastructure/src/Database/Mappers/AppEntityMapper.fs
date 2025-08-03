namespace Freetool.Infrastructure.Database.Mappers

open System.Text.Json
open Freetool.Domain.Entities
open Freetool.Domain.Events
open Freetool.Domain.ValueObjects
open Freetool.Infrastructure.Database

type InputTypeJson =
    | Email
    | Date
    | Text of MaxLength: int
    | Integer
    | Boolean
    | MultiChoice of Choices: InputTypeJson list

type InputJson = {
    Title: string
    Type: InputTypeJson
    Required: bool
}

type KeyValuePairJson = { Key: string; Value: string }

module AppEntityMapper =
    let rec inputTypeFromDomain (inputType: InputType) : InputTypeJson =
        match inputType.Value with
        | InputTypeValue.Email -> Email
        | InputTypeValue.Date -> Date
        | InputTypeValue.Text maxLength -> Text(MaxLength = maxLength)
        | InputTypeValue.Integer -> Integer
        | InputTypeValue.Boolean -> Boolean
        | InputTypeValue.MultiChoice choices ->
            let jsonChoices =
                choices
                |> List.map (fun choice ->
                    match choice with
                    | InputTypeValue.Email -> Email
                    | InputTypeValue.Date -> Date
                    | InputTypeValue.Text maxLength -> Text(MaxLength = maxLength)
                    | InputTypeValue.Integer -> Integer
                    | InputTypeValue.Boolean -> Boolean
                    | InputTypeValue.MultiChoice _ -> failwith "Nested MultiChoice not allowed")

            MultiChoice(Choices = jsonChoices)

    let rec inputTypeToDomain (inputTypeJson: InputTypeJson) : InputType =
        match inputTypeJson with
        | Email -> InputType.Email()
        | Date -> InputType.Date()
        | Text maxLength ->
            match InputType.Text(maxLength) with
            | Ok inputType -> inputType
            | Error _ -> failwith (sprintf "Invalid text max length: %d" maxLength)
        | Integer -> InputType.Integer()
        | Boolean -> InputType.Boolean()
        | MultiChoice choices ->
            let domainChoices =
                choices
                |> List.map (fun choice ->
                    match choice with
                    | Email -> InputTypeValue.Email
                    | Date -> InputTypeValue.Date
                    | Text maxLength -> InputTypeValue.Text(maxLength)
                    | Integer -> InputTypeValue.Integer
                    | Boolean -> InputTypeValue.Boolean
                    | MultiChoice _ -> failwith "Nested MultiChoice not allowed")

            match InputType.MultiChoice(domainChoices) with
            | Ok inputType -> inputType
            | Error _ -> failwith "Invalid MultiChoice configuration"

    let inputFromDomain (input: Input) : InputJson = {
        Title = input.Title
        Type = inputTypeFromDomain input.Type
        Required = input.Required
    }

    let inputToDomain (inputJson: InputJson) : Input = {
        Title = inputJson.Title
        Type = inputTypeToDomain inputJson.Type
        Required = inputJson.Required
    }

    let keyValuePairFromJson (kvpJson: KeyValuePairJson) : KeyValuePair =
        match KeyValuePair.Create(kvpJson.Key, kvpJson.Value) with
        | Ok kvp -> kvp
        | Error _ -> failwith $"Invalid key-value pair: {kvpJson.Key}={kvpJson.Value}"

    let keyValuePairToJson (kvp: KeyValuePair) : KeyValuePairJson = { Key = kvp.Key; Value = kvp.Value }

    // Entity -> Domain conversions (database data is trusted, so directly to ValidatedApp)
    let fromEntity (entity: AppEntity) : ValidatedApp =
        let inputsJson = JsonSerializer.Deserialize<InputJson list>(entity.Inputs)
        let inputs = inputsJson |> List.map inputToDomain

        let urlParametersJson =
            JsonSerializer.Deserialize<KeyValuePairJson list>(entity.UrlParameters)

        let urlParameters = urlParametersJson |> List.map keyValuePairFromJson

        let headersJson = JsonSerializer.Deserialize<KeyValuePairJson list>(entity.Headers)
        let headers = headersJson |> List.map keyValuePairFromJson

        let bodyJson = JsonSerializer.Deserialize<KeyValuePairJson list>(entity.Body)
        let body = bodyJson |> List.map keyValuePairFromJson

        let urlPath =
            if System.String.IsNullOrEmpty(entity.UrlPath) then
                None
            else
                Some(entity.UrlPath)

        let appData = {
            Id = AppId.FromGuid(entity.Id)
            Name = entity.Name
            FolderId = FolderId.FromGuid(entity.FolderId)
            ResourceId = ResourceId.FromGuid(entity.ResourceId)
            Inputs = inputs
            UrlPath = urlPath
            UrlParameters = urlParameters
            Headers = headers
            Body = body
            CreatedAt = entity.CreatedAt
            UpdatedAt = entity.UpdatedAt
        }

        App.fromData appData

    // Domain -> Entity conversions (can convert from any validation state)
    let toEntity (app: App) : AppEntity =
        let appData = app.State
        let inputsJson = appData.Inputs |> List.map inputFromDomain
        let inputsJsonString = JsonSerializer.Serialize(inputsJson)

        let urlParametersJson = appData.UrlParameters |> List.map keyValuePairToJson
        let urlParametersJsonString = JsonSerializer.Serialize(urlParametersJson)

        let headersJson = appData.Headers |> List.map keyValuePairToJson
        let headersJsonString = JsonSerializer.Serialize(headersJson)

        let bodyJson = appData.Body |> List.map keyValuePairToJson
        let bodyJsonString = JsonSerializer.Serialize(bodyJson)

        let entity = AppEntity()
        entity.Id <- appData.Id.Value
        entity.Name <- appData.Name
        entity.FolderId <- appData.FolderId.Value
        entity.ResourceId <- appData.ResourceId.Value
        entity.Inputs <- inputsJsonString
        entity.UrlPath <- (appData.UrlPath |> Option.defaultValue "")
        entity.UrlParameters <- urlParametersJsonString
        entity.Headers <- headersJsonString
        entity.Body <- bodyJsonString
        entity.CreatedAt <- appData.CreatedAt
        entity.UpdatedAt <- appData.UpdatedAt
        entity