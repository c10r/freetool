namespace Freetool.Domain.Entities

open System
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events

type AppData = {
    Id: AppId
    Name: string
    FolderId: FolderId
    Inputs: Input list
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

type App = EventSourcingAggregate<AppData>

module AppAggregateHelpers =
    let getEntityId (app: App) : AppId = app.State.Id

    let implementsIEntity (app: App) =
        { new IEntity<AppId> with
            member _.Id = app.State.Id
        }

// Type aliases for clarity
type UnvalidatedApp = App // From DTOs - potentially unsafe
type ValidatedApp = App // Validated domain model and database data

module App =
    // Validate input
    let private validateInput (input: Input) : Result<Input, DomainError> =
        match InputTitle.Create(Some input.Title) with
        | Error err -> Error err
        | Ok validTitle -> Ok { input with Title = validTitle.Value }

    let fromData (appData: AppData) : ValidatedApp = {
        State = appData
        UncommittedEvents = []
    }

    let validate (app: UnvalidatedApp) : Result<ValidatedApp, DomainError> =
        let appData = app.State

        match AppName.Create(Some appData.Name) with
        | Error err -> Error err
        | Ok validName ->
            // Validate all inputs
            let validateInputs inputs =
                let rec validateList acc remaining =
                    match remaining with
                    | [] -> Ok(List.rev acc)
                    | input :: rest ->
                        match validateInput input with
                        | Error err -> Error err
                        | Ok validInput -> validateList (validInput :: acc) rest

                validateList [] inputs

            match validateInputs appData.Inputs with
            | Error err -> Error err
            | Ok validInputs ->
                Ok {
                    State = {
                        appData with
                            Name = validName.Value
                            Inputs = validInputs
                    }
                    UncommittedEvents = app.UncommittedEvents
                }

    let updateName (newName: string) (app: ValidatedApp) : Result<ValidatedApp, DomainError> =
        match AppName.Create(Some newName) with
        | Error err -> Error err
        | Ok validName ->
            let oldName =
                AppName.Create(Some app.State.Name) |> Result.defaultValue (AppName(""))

            let updatedAppData = {
                app.State with
                    Name = validName.Value
                    UpdatedAt = DateTime.UtcNow
            }

            let nameChangedEvent =
                AppEvents.appUpdated app.State.Id [ AppChange.NameChanged(oldName, validName) ]

            Ok {
                State = updatedAppData
                UncommittedEvents = app.UncommittedEvents @ [ nameChangedEvent :> IDomainEvent ]
            }

    let updateInputs (newInputs: Input list) (app: ValidatedApp) : Result<ValidatedApp, DomainError> =
        let validateInputs inputs =
            let rec validateList acc remaining =
                match remaining with
                | [] -> Ok(List.rev acc)
                | input :: rest ->
                    match validateInput input with
                    | Error err -> Error err
                    | Ok validInput -> validateList (validInput :: acc) rest

            validateList [] inputs

        match validateInputs newInputs with
        | Error err -> Error err
        | Ok validInputs ->
            let oldInputs = app.State.Inputs

            let updatedAppData = {
                app.State with
                    Inputs = validInputs
                    UpdatedAt = DateTime.UtcNow
            }

            let inputsChangedEvent =
                AppEvents.appUpdated app.State.Id [ AppChange.InputsChanged(oldInputs, validInputs) ]

            Ok {
                State = updatedAppData
                UncommittedEvents = app.UncommittedEvents @ [ inputsChangedEvent :> IDomainEvent ]
            }

    let create (name: string) (folderId: FolderId) (inputs: Input list) : Result<ValidatedApp, DomainError> =
        let appData = {
            Id = AppId.NewId()
            Name = name
            FolderId = folderId
            Inputs = inputs
            CreatedAt = DateTime.UtcNow
            UpdatedAt = DateTime.UtcNow
        }

        let unvalidatedApp = {
            State = appData
            UncommittedEvents = []
        }

        match validate unvalidatedApp with
        | Error err -> Error err
        | Ok validatedApp ->
            let validName = AppName.Create(Some name) |> Result.defaultValue (AppName(""))

            let appCreatedEvent =
                AppEvents.appCreated appData.Id validName (Some folderId) inputs

            Ok {
                validatedApp with
                    UncommittedEvents = [ appCreatedEvent :> IDomainEvent ]
            }

    let markForDeletion (app: ValidatedApp) : ValidatedApp =
        let appDeletedEvent = AppEvents.appDeleted app.State.Id

        {
            app with
                UncommittedEvents = app.UncommittedEvents @ [ appDeletedEvent :> IDomainEvent ]
        }

    let getUncommittedEvents (app: ValidatedApp) : IDomainEvent list = app.UncommittedEvents

    let markEventsAsCommitted (app: ValidatedApp) : ValidatedApp = { app with UncommittedEvents = [] }

    let getId (app: App) : AppId = app.State.Id

    let getName (app: App) : string = app.State.Name

    let getFolderId (app: App) : FolderId = app.State.FolderId

    let getInputs (app: App) : Input list = app.State.Inputs

    let getCreatedAt (app: App) : DateTime = app.State.CreatedAt

    let getUpdatedAt (app: App) : DateTime = app.State.UpdatedAt