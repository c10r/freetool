namespace Freetool.Application.Mappers

open System
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events
open Freetool.Application.DTOs

module AppMapper =
    let rec inputTypeToDtoType (inputType: InputType) : InputTypeDto =
        match inputType.Value with
        | InputTypeValue.Email -> Email
        | InputTypeValue.Date -> Date
        | InputTypeValue.Text maxLength -> Text(MaxLength = maxLength)
        | InputTypeValue.Integer -> Integer
        | InputTypeValue.Boolean -> Boolean
        | InputTypeValue.MultiEmail allowedEmails ->
            let emailStrings = allowedEmails |> List.map (fun e -> e.ToString())
            MultiEmail(AllowedEmails = emailStrings)
        | InputTypeValue.MultiDate allowedDates ->
            let dateStrings = allowedDates |> List.map (fun d -> d.ToString("yyyy-MM-dd"))
            MultiDate(AllowedDates = dateStrings)
        | InputTypeValue.MultiText(maxLength, allowedValues) ->
            MultiText(MaxLength = maxLength, AllowedValues = allowedValues)
        | InputTypeValue.MultiInteger allowedIntegers -> MultiInteger(AllowedIntegers = allowedIntegers)

    let rec inputTypeFromDtoType (inputTypeDto: InputTypeDto) : InputType =
        match inputTypeDto with
        | Email -> InputType.Email()
        | Date -> InputType.Date()
        | Text maxLength ->
            match InputType.Text(maxLength) with
            | Ok inputType -> inputType
            | Error _ -> failwith (sprintf "Invalid text max length: %d" maxLength)
        | Integer -> InputType.Integer()
        | Boolean -> InputType.Boolean()
        | MultiEmail emailStrings ->
            let emails =
                emailStrings
                |> List.map (fun emailStr ->
                    match Email.Create(Some emailStr) with
                    | Ok email -> email
                    | Error _ -> failwith (sprintf "Invalid email: %s" emailStr))

            match InputType.MultiEmail(emails) with
            | Ok inputType -> inputType
            | Error _ -> failwith "Invalid MultiEmail configuration"
        | MultiDate dateStrings ->
            let dates =
                dateStrings
                |> List.map (fun dateStr ->
                    match DateTime.TryParse(dateStr) with
                    | (true, date) -> date
                    | (false, _) -> failwith (sprintf "Invalid date: %s" dateStr))

            match InputType.MultiDate(dates) with
            | Ok inputType -> inputType
            | Error _ -> failwith "Invalid MultiDate configuration"
        | MultiText(maxLength, allowedValues) ->
            match InputType.MultiText(maxLength, allowedValues) with
            | Ok inputType -> inputType
            | Error _ -> failwith "Invalid MultiText configuration"
        | MultiInteger allowedIntegers ->
            match InputType.MultiInteger(allowedIntegers) with
            | Ok inputType -> inputType
            | Error _ -> failwith "Invalid MultiInteger configuration"

    let inputToDto (input: Input) : AppInputDto = {
        Input = {
            Title = input.Title
            Type = inputTypeToDtoType input.Type
        }
        Required = input.Required
    }

    let inputFromDto (inputDto: AppInputDto) : Input = {
        Title = inputDto.Input.Title
        Type = inputTypeFromDtoType inputDto.Input.Type
        Required = inputDto.Required
    }

    let keyValuePairToDto (kvp: KeyValuePair) : KeyValuePairDto = { Key = kvp.Key; Value = kvp.Value }

    let keyValuePairFromDto (dto: KeyValuePairDto) : (string * string) = (dto.Key, dto.Value)

    type CreateAppRequest = {
        Name: string
        FolderId: string
        ResourceId: string
        Inputs: Input list
        UrlPath: string option
        UrlParameters: (string * string) list
        Headers: (string * string) list
        Body: (string * string) list
    }

    let fromCreateDto (dto: CreateAppDto) : CreateAppRequest =
        let inputs = dto.Inputs |> List.map inputFromDto
        let urlParameters = dto.UrlParameters |> List.map keyValuePairFromDto
        let headers = dto.Headers |> List.map keyValuePairFromDto
        let body = dto.Body |> List.map keyValuePairFromDto

        {
            Name = dto.Name
            FolderId = dto.FolderId
            ResourceId = dto.ResourceId
            Inputs = inputs
            UrlPath = dto.UrlPath
            UrlParameters = urlParameters
            Headers = headers
            Body = body
        }

    let fromUpdateNameDto (dto: UpdateAppNameDto) (app: ValidatedApp) : UnvalidatedApp = {
        State = {
            app.State with
                Name = dto.Name
                UpdatedAt = DateTime.UtcNow
        }
        UncommittedEvents = app.UncommittedEvents
    }

    let fromUpdateInputsDto (dto: UpdateAppInputsDto) (app: ValidatedApp) : UnvalidatedApp =
        let inputs = dto.Inputs |> List.map inputFromDto

        {
            State = {
                app.State with
                    Inputs = inputs
                    UpdatedAt = DateTime.UtcNow
            }
            UncommittedEvents = app.UncommittedEvents
        }