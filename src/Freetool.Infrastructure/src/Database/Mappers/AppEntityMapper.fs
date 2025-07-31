namespace Freetool.Infrastructure.Database.Mappers

open System.Text.Json
open Freetool.Domain.Entities
open Freetool.Domain.Events
open Freetool.Domain.ValueObjects
open Freetool.Infrastructure.Database

// JSON serialization types for inputs
type InputTypeJson =
    | Email
    | Date
    | Text of MaxLength: int
    | Integer
    | Boolean
    | MultiChoice of Choices: InputTypeJson list

type InputJson = { Title: string; Type: InputTypeJson }

module AppEntityMapper =
    // Helper functions for input type conversion
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
    }

    let inputToDomain (inputJson: InputJson) : Input = {
        Title = inputJson.Title
        Type = inputTypeToDomain inputJson.Type
    }

    // Entity -> Domain conversions (database data is trusted, so directly to ValidatedApp)
    let fromEntity (entity: AppEntity) : ValidatedApp =
        let inputsJson = JsonSerializer.Deserialize<InputJson list>(entity.Inputs)
        let inputs = inputsJson |> List.map inputToDomain

        App {
            Id = AppId.FromGuid(entity.Id)
            Name = entity.Name
            FolderId = FolderId.FromGuid(entity.FolderId)
            Inputs = inputs
            CreatedAt = entity.CreatedAt
            UpdatedAt = entity.UpdatedAt
        }

    // Domain -> Entity conversions (can convert from any validation state)
    let toEntity (App appData: App<'State>) : AppEntity =
        let inputsJson = appData.Inputs |> List.map inputFromDomain
        let inputsJsonString = JsonSerializer.Serialize(inputsJson)

        let entity = AppEntity()
        entity.Id <- appData.Id.Value
        entity.Name <- appData.Name
        entity.FolderId <- appData.FolderId.Value
        entity.Inputs <- inputsJsonString
        entity.CreatedAt <- appData.CreatedAt
        entity.UpdatedAt <- appData.UpdatedAt
        entity