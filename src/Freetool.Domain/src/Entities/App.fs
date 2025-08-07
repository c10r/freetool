namespace Freetool.Domain.Entities

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open System.Text.Json.Serialization
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events

[<Table("Apps")>]
[<Index([| "Name"; "FolderId" |], IsUnique = true, Name = "IX_Apps_Name_FolderId")>]
// CLIMutable for EntityFramework
[<CLIMutable>]
type AppData = {
    [<Key>]
    Id: AppId

    [<Required>]
    [<MaxLength(100)>]
    Name: string

    [<Required>]
    FolderId: FolderId

    [<Required>]
    ResourceId: ResourceId

    [<Required>]
    [<Column(TypeName = "TEXT")>] // JSON serialized list of inputs
    Inputs: Input list

    [<MaxLength(500)>]
    UrlPath: string option

    [<Required>]
    [<Column(TypeName = "TEXT")>] // JSON serialized key-value pairs
    UrlParameters: KeyValuePair list

    [<Required>]
    [<Column(TypeName = "TEXT")>] // JSON serialized key-value pairs
    Headers: KeyValuePair list

    [<Required>]
    [<Column(TypeName = "TEXT")>] // JSON serialized key-value pairs
    Body: KeyValuePair list

    [<Required>]
    CreatedAt: DateTime

    [<Required>]
    UpdatedAt: DateTime

    [<JsonIgnore>]
    IsDeleted: bool
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

    let private checkResourceConflicts
        (resource: ResourceAppConflictData)
        (urlParameters: (string * string) list option)
        (headers: (string * string) list option)
        (body: (string * string) list option)
        : Result<unit, DomainError> =
        BusinessRules.checkResourceToAppConflicts resource urlParameters headers body

    let private create
        (name: string)
        (folderId: FolderId)
        (resourceId: ResourceId)
        (inputs: Input list)
        (urlPath: string option)
        (urlParameters: (string * string) list)
        (headers: (string * string) list)
        (body: (string * string) list)
        : Result<ValidatedApp, DomainError> =
        // Validate headers
        let validateKeyValuePairs pairs =
            pairs
            |> List.fold
                (fun acc (key: string, value: string) ->
                    match acc with
                    | Error err -> Error err
                    | Ok validPairs ->
                        match KeyValuePair.Create(key, value) with
                        | Error err -> Error err
                        | Ok validPair -> Ok(validPair :: validPairs))
                (Ok [])
            |> Result.map List.rev

        match validateKeyValuePairs urlParameters with
        | Error err -> Error err
        | Ok validUrlParameters ->
            match validateKeyValuePairs headers with
            | Error err -> Error err
            | Ok validHeaders ->
                match validateKeyValuePairs body with
                | Error err -> Error err
                | Ok validBody ->
                    let appData = {
                        Id = AppId.NewId()
                        Name = name
                        FolderId = folderId
                        ResourceId = resourceId
                        Inputs = inputs
                        UrlPath = urlPath
                        UrlParameters = validUrlParameters
                        Headers = validHeaders
                        Body = validBody
                        CreatedAt = DateTime.UtcNow
                        UpdatedAt = DateTime.UtcNow
                        IsDeleted = false
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
                            AppEvents.appCreated appData.Id validName (Some folderId) resourceId inputs

                        Ok {
                            validatedApp with
                                UncommittedEvents = [ appCreatedEvent :> IDomainEvent ]
                        }

    let createWithResource
        (name: string)
        (folderId: FolderId)
        (resource: ValidatedResource)
        (inputs: Input list)
        (urlPath: string option)
        (urlParameters: (string * string) list)
        (headers: (string * string) list)
        (body: (string * string) list)
        : Result<ValidatedApp, DomainError> =

        // Business rule: App cannot override existing Resource parameters
        let resourceConflictData = Resource.toConflictData resource

        match checkResourceConflicts resourceConflictData (Some urlParameters) (Some headers) (Some body) with
        | Error err -> Error err
        | Ok() ->
            // No conflicts, proceed with normal creation
            let resourceId = Resource.getId resource
            create name folderId resourceId inputs urlPath urlParameters headers body

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

    let getResourceId (app: App) : ResourceId = app.State.ResourceId

    let getInputs (app: App) : Input list = app.State.Inputs

    let getCreatedAt (app: App) : DateTime = app.State.CreatedAt

    let getUpdatedAt (app: App) : DateTime = app.State.UpdatedAt

    let getUrlPath (app: App) : string option = app.State.UrlPath

    let getUrlParameters (app: App) : (string * string) list =
        app.State.UrlParameters |> List.map (fun kvp -> (kvp.Key, kvp.Value))

    let getHeaders (app: App) : (string * string) list =
        app.State.Headers |> List.map (fun kvp -> (kvp.Key, kvp.Value))

    let getBody (app: App) : (string * string) list =
        app.State.Body |> List.map (fun kvp -> (kvp.Key, kvp.Value))

    let toConflictData (app: App) : AppResourceConflictData = {
        AppId = (getId app).ToString()
        UrlParameters = getUrlParameters app
        Headers = getHeaders app
        Body = getBody app
    }

    let updateUrlPath (newUrlPath: string option) (app: ValidatedApp) : Result<ValidatedApp, DomainError> =
        let updatedAppData = {
            app.State with
                UrlPath = newUrlPath
                UpdatedAt = DateTime.UtcNow
        }

        let urlPathChangedEvent =
            AppEvents.appUpdated app.State.Id [ AppChange.UrlPathChanged(app.State.UrlPath, newUrlPath) ]

        Ok {
            State = updatedAppData
            UncommittedEvents = app.UncommittedEvents @ [ urlPathChangedEvent :> IDomainEvent ]
        }

    let updateUrlParameters
        (newUrlParameters: (string * string) list)
        (resource: ResourceAppConflictData)
        (app: ValidatedApp)
        : Result<ValidatedApp, DomainError> =
        let validateKeyValuePairs pairs =
            pairs
            |> List.fold
                (fun acc (key: string, value: string) ->
                    match acc with
                    | Error err -> Error err
                    | Ok validPairs ->
                        match KeyValuePair.Create(key, value) with
                        | Error err -> Error err
                        | Ok validPair -> Ok(validPair :: validPairs))
                (Ok [])
            |> Result.map List.rev

        match validateKeyValuePairs newUrlParameters with
        | Error err -> Error err
        | Ok validUrlParams ->
            match checkResourceConflicts resource (Some newUrlParameters) None None with
            | Error err -> Error err
            | Ok() ->
                let oldUrlParams = app.State.UrlParameters

                let updatedAppData = {
                    app.State with
                        UrlParameters = validUrlParams
                        UpdatedAt = DateTime.UtcNow
                }

                let urlParamsChangedEvent =
                    AppEvents.appUpdated app.State.Id [ AppChange.UrlParametersChanged(oldUrlParams, validUrlParams) ]

                Ok {
                    State = updatedAppData
                    UncommittedEvents = app.UncommittedEvents @ [ urlParamsChangedEvent :> IDomainEvent ]
                }

    let updateHeaders
        (newHeaders: (string * string) list)
        (resource: ResourceAppConflictData)
        (app: ValidatedApp)
        : Result<ValidatedApp, DomainError> =
        let validateKeyValuePairs pairs =
            pairs
            |> List.fold
                (fun acc (key: string, value: string) ->
                    match acc with
                    | Error err -> Error err
                    | Ok validPairs ->
                        match KeyValuePair.Create(key, value) with
                        | Error err -> Error err
                        | Ok validPair -> Ok(validPair :: validPairs))
                (Ok [])
            |> Result.map List.rev

        match validateKeyValuePairs newHeaders with
        | Error err -> Error err
        | Ok validHeaders ->
            match checkResourceConflicts resource None (Some newHeaders) None with
            | Error err -> Error err
            | Ok() ->
                let oldHeaders = app.State.Headers

                let updatedAppData = {
                    app.State with
                        Headers = validHeaders
                        UpdatedAt = DateTime.UtcNow
                }

                let headersChangedEvent =
                    AppEvents.appUpdated app.State.Id [ AppChange.HeadersChanged(oldHeaders, validHeaders) ]

                Ok {
                    State = updatedAppData
                    UncommittedEvents = app.UncommittedEvents @ [ headersChangedEvent :> IDomainEvent ]
                }

    let updateBody
        (newBody: (string * string) list)
        (resource: ResourceAppConflictData)
        (app: ValidatedApp)
        : Result<ValidatedApp, DomainError> =
        let validateKeyValuePairs pairs =
            pairs
            |> List.fold
                (fun acc (key: string, value: string) ->
                    match acc with
                    | Error err -> Error err
                    | Ok validPairs ->
                        match KeyValuePair.Create(key, value) with
                        | Error err -> Error err
                        | Ok validPair -> Ok(validPair :: validPairs))
                (Ok [])
            |> Result.map List.rev

        match validateKeyValuePairs newBody with
        | Error err -> Error err
        | Ok validBody ->
            match checkResourceConflicts resource None None (Some newBody) with
            | Error err -> Error err
            | Ok() ->
                let oldBody = app.State.Body

                let updatedAppData = {
                    app.State with
                        Body = validBody
                        UpdatedAt = DateTime.UtcNow
                }

                let bodyChangedEvent =
                    AppEvents.appUpdated app.State.Id [ AppChange.BodyChanged(oldBody, validBody) ]

                Ok {
                    State = updatedAppData
                    UncommittedEvents = app.UncommittedEvents @ [ bodyChangedEvent :> IDomainEvent ]
                }