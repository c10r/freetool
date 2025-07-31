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
        | InputTypeValue.MultiChoice choices ->
            let dtoChoices =
                choices
                |> List.map (fun choice ->
                    match choice with
                    | InputTypeValue.Email -> Email
                    | InputTypeValue.Date -> Date
                    | InputTypeValue.Text maxLength -> Text(MaxLength = maxLength)
                    | InputTypeValue.Integer -> Integer
                    | InputTypeValue.Boolean -> Boolean
                    | InputTypeValue.MultiChoice _ -> failwith "Nested MultiChoice not allowed")

            MultiChoice(Choices = dtoChoices)

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

    let inputToDto (input: Input) : InputDto = {
        Title = input.Title
        Type = inputTypeToDtoType input.Type
    }

    let inputFromDto (inputDto: InputDto) : Input = {
        Title = inputDto.Title
        Type = inputTypeFromDtoType inputDto.Type
    }

    let fromCreateDto (dto: CreateAppDto) : UnvalidatedApp =
        let folderId =
            match Guid.TryParse dto.FolderId with
            | true, guid -> FolderId.FromGuid(guid)
            | false, _ -> failwith "Invalid folder ID format"

        let inputs = dto.Inputs |> List.map inputFromDto

        {
            State = {
                Id = AppId.NewId()
                Name = dto.Name
                FolderId = folderId
                Inputs = inputs
                CreatedAt = DateTime.UtcNow
                UpdatedAt = DateTime.UtcNow
            }
            UncommittedEvents = []
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

    let toDto (app: ValidatedApp) : AppDto = {
        Id = app.State.Id.Value.ToString()
        Name = app.State.Name
        FolderId = app.State.FolderId.Value.ToString()
        Inputs = app.State.Inputs |> List.map inputToDto
        CreatedAt = app.State.CreatedAt
        UpdatedAt = app.State.UpdatedAt
    }

    let toPagedDto (apps: ValidatedApp list) (totalCount: int) (skip: int) (take: int) : PagedAppsDto = {
        Apps = apps |> List.map toDto
        TotalCount = totalCount
        Skip = skip
        Take = take
    }