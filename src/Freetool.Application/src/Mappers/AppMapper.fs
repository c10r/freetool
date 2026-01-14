namespace Freetool.Application.Mappers

open System
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events
open Freetool.Application.DTOs
open Freetool.Domain

module AppMapper =
    /// Combine a list of Results into a Result containing a list
    let private sequenceResults (results: Result<'T, DomainError> list) : Result<'T list, DomainError> =
        let folder acc item =
            match acc, item with
            | Ok items, Ok item -> Ok(item :: items)
            | Error e, _ -> Error e
            | _, Error e -> Error e

        results |> List.fold folder (Ok []) |> Result.map List.rev

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
        | InputTypeValue.Radio options ->
            let optionDtos =
                options
                |> List.map (fun o ->
                    { RadioOptionDto.Value = o.Value
                      Label = o.Label })

            Radio(Options = optionDtos)

    let rec inputTypeFromDtoType (inputTypeDto: InputTypeDto) : Result<InputType, DomainError> =
        match inputTypeDto with
        | Email -> Ok(InputType.Email())
        | Date -> Ok(InputType.Date())
        | Text maxLength ->
            InputType.Text(maxLength)
            |> Result.mapError (fun _ -> ValidationError $"Invalid text max length: {maxLength}")
        | Integer -> Ok(InputType.Integer())
        | Boolean -> Ok(InputType.Boolean())
        | MultiEmail emailStrings ->
            let emailResults =
                emailStrings
                |> List.map (fun emailStr ->
                    Email.Create(Some emailStr)
                    |> Result.mapError (fun _ -> ValidationError $"Invalid email: {emailStr}"))

            emailResults
            |> sequenceResults
            |> Result.bind (fun emails ->
                InputType.MultiEmail(emails)
                |> Result.mapError (fun _ -> ValidationError "Invalid MultiEmail configuration"))
        | MultiDate dateStrings ->
            let dateResults =
                dateStrings
                |> List.map (fun dateStr ->
                    match DateTime.TryParse(dateStr) with
                    | (true, date) -> Ok date
                    | (false, _) -> Error(ValidationError $"Invalid date: {dateStr}"))

            dateResults
            |> sequenceResults
            |> Result.bind (fun dates ->
                InputType.MultiDate(dates)
                |> Result.mapError (fun _ -> ValidationError "Invalid MultiDate configuration"))
        | MultiText(maxLength, allowedValues) ->
            InputType.MultiText(maxLength, allowedValues)
            |> Result.mapError (fun _ -> ValidationError "Invalid MultiText configuration")
        | MultiInteger allowedIntegers ->
            InputType.MultiInteger(allowedIntegers)
            |> Result.mapError (fun _ -> ValidationError "Invalid MultiInteger configuration")
        | Radio optionDtos ->
            let options =
                optionDtos
                |> List.map (fun dto ->
                    { RadioOption.Value = dto.Value
                      Label = dto.Label })

            InputType.Radio(options)
            |> Result.mapError (fun _ -> ValidationError "Invalid Radio configuration")

    let inputToDto (input: Input) : AppInputDto =
        { Input =
            { Title = input.Title
              Type = inputTypeToDtoType input.Type }
          Required = input.Required
          DefaultValue = input.DefaultValue |> Option.map (fun dv -> dv.ToRawString()) }

    let inputFromDto (inputDto: AppInputDto) : Result<Input, DomainError> =
        inputTypeFromDtoType inputDto.Input.Type
        |> Result.bind (fun inputType ->
            // Parse and validate the default value against the input type
            let defaultValueResult =
                match inputDto.DefaultValue with
                | None -> Ok None
                | Some rawValue ->
                    if inputDto.Required then
                        // Required inputs shouldn't have defaults - ignore silently
                        Ok None
                    else
                        DefaultValue.Create(inputType, rawValue) |> Result.map Some

            defaultValueResult
            |> Result.map (fun defaultValue ->
                { Title = inputDto.Input.Title
                  Type = inputType
                  Required = inputDto.Required
                  DefaultValue = defaultValue }))

    let keyValuePairToDto (kvp: KeyValuePair) : KeyValuePairDto = { Key = kvp.Key; Value = kvp.Value }

    let keyValuePairFromDto (dto: KeyValuePairDto) : (string * string) = (dto.Key, dto.Value)

    type CreateAppRequest =
        { Name: string
          FolderId: string
          ResourceId: string
          HttpMethod: string
          Inputs: Input list
          UrlPath: string option
          UrlParameters: (string * string) list
          Headers: (string * string) list
          Body: (string * string) list
          UseDynamicJsonBody: bool }

    let fromCreateDto (dto: CreateAppDto) : Result<CreateAppRequest, DomainError> =
        dto.Inputs
        |> List.map inputFromDto
        |> sequenceResults
        |> Result.map (fun inputs ->
            let urlParameters = dto.UrlParameters |> List.map keyValuePairFromDto
            let headers = dto.Headers |> List.map keyValuePairFromDto
            let body = dto.Body |> List.map keyValuePairFromDto

            { Name = dto.Name
              FolderId = dto.FolderId
              ResourceId = dto.ResourceId
              HttpMethod = dto.HttpMethod
              Inputs = inputs
              UrlPath = dto.UrlPath
              UrlParameters = urlParameters
              Headers = headers
              Body = body
              UseDynamicJsonBody = dto.UseDynamicJsonBody })

    let fromUpdateNameDto (dto: UpdateAppNameDto) (app: ValidatedApp) : UnvalidatedApp =
        { State =
            { app.State with
                Name = dto.Name
                UpdatedAt = DateTime.UtcNow }
          UncommittedEvents = app.UncommittedEvents }

    let fromUpdateInputsDto (dto: UpdateAppInputsDto) (app: ValidatedApp) : Result<UnvalidatedApp, DomainError> =
        dto.Inputs
        |> List.map inputFromDto
        |> sequenceResults
        |> Result.map (fun inputs ->
            { State =
                { app.State with
                    Inputs = inputs
                    UpdatedAt = DateTime.UtcNow }
              UncommittedEvents = app.UncommittedEvents })
