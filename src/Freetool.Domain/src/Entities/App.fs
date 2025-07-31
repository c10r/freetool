namespace Freetool.Domain.Entities

open System
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events

// Core app data that's shared across all states
type AppData = {
    Id: AppId
    Name: string
    FolderId: FolderId
    Inputs: Input list
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

// App type parameterized by validation state
type App<'State> =
    | App of AppData

    interface IEntity<AppId> with
        member this.Id =
            let (App appData) = this
            appData.Id

// Type aliases for clarity
type UnvalidatedApp = App<Unvalidated> // From DTOs - potentially unsafe
type ValidatedApp = App<Validated> // Validated domain model and database data

module App =
    // Validate input
    let private validateInput (input: Input) : Result<Input, DomainError> =
        match InputTitle.Create(input.Title) with
        | Error err -> Error err
        | Ok validTitle -> Ok { input with Title = validTitle.Value }

    // Validate unvalidated app -> validated app
    let validate (App appData: UnvalidatedApp) : Result<ValidatedApp, DomainError> =
        match AppName.Create(appData.Name) with
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
                Ok(
                    App {
                        appData with
                            Name = validName.Value
                            Inputs = validInputs
                    }
                )

    // Business logic operations on validated apps
    let updateName (newName: string) (App appData: ValidatedApp) : Result<ValidatedApp, DomainError> =
        match AppName.Create(newName) with
        | Error err -> Error err
        | Ok validName ->
            Ok(
                App {
                    appData with
                        Name = validName.Value
                        UpdatedAt = DateTime.UtcNow
                }
            )

    let updateInputs (newInputs: Input list) (App appData: ValidatedApp) : Result<ValidatedApp, DomainError> =
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
            Ok(
                App {
                    appData with
                        Inputs = validInputs
                        UpdatedAt = DateTime.UtcNow
                }
            )

    // Create a new validated app
    let create (name: string) (folderId: FolderId) (inputs: Input list) : Result<ValidatedApp, DomainError> =
        let unvalidatedApp =
            App {
                Id = AppId.NewId()
                Name = name
                FolderId = folderId
                Inputs = inputs
                CreatedAt = DateTime.UtcNow
                UpdatedAt = DateTime.UtcNow
            }

        validate unvalidatedApp

    // Utility functions for accessing data from any state
    let getId (App appData: App<'State>) : AppId = appData.Id

    let getName (App appData: App<'State>) : string = appData.Name

    let getFolderId (App appData: App<'State>) : FolderId = appData.FolderId

    let getInputs (App appData: App<'State>) : Input list = appData.Inputs

    let getCreatedAt (App appData: App<'State>) : DateTime = appData.CreatedAt

    let getUpdatedAt (App appData: App<'State>) : DateTime = appData.UpdatedAt