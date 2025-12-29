namespace Freetool.Domain.Entities

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open System.Text.Json.Serialization
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events

[<Table("Resources")>]
[<Index([| "Name"; "SpaceId" |], IsUnique = true, Name = "IX_Resources_Name_SpaceId")>]
// CLIMutable for EntityFramework
[<CLIMutable>]
type ResourceData =
    { [<Key>]
      Id: ResourceId

      [<Required>]
      [<MaxLength(100)>]
      Name: ResourceName

      [<Required>]
      [<MaxLength(500)>]
      Description: ResourceDescription

      [<Required>]
      SpaceId: SpaceId

      [<Required>]
      [<MaxLength(10)>]
      HttpMethod: HttpMethod

      [<Required>]
      [<MaxLength(1_000)>]
      BaseUrl: BaseUrl

      [<Required>]
      [<Column(TypeName = "TEXT")>] // JSON string
      UrlParameters: KeyValuePair list

      [<Required>]
      [<Column(TypeName = "TEXT")>] // JSON string
      Headers: KeyValuePair list

      [<Required>]
      [<Column(TypeName = "TEXT")>] // JSON string
      Body: KeyValuePair list

      [<Required>]
      [<JsonIgnore>]
      CreatedAt: DateTime

      [<Required>]
      [<JsonIgnore>]
      UpdatedAt: DateTime

      [<JsonIgnore>]
      IsDeleted: bool }

type Resource = EventSourcingAggregate<ResourceData>

module ResourceAggregateHelpers =
    let getEntityId (resource: Resource) : ResourceId = resource.State.Id

    let implementsIEntity (resource: Resource) =
        { new IEntity<ResourceId> with
            member _.Id = resource.State.Id }

// Type aliases for clarity
type UnvalidatedResource = Resource // From DTOs - potentially unsafe
type ValidatedResource = Resource // Validated domain model and database data

module Resource =
    let fromData (resourceData: ResourceData) : ValidatedResource =
        { State = resourceData
          UncommittedEvents = [] }

    let create
        (actorUserId: UserId)
        (spaceId: SpaceId)
        (name: string)
        (description: string)
        (baseUrl: string)
        (urlParameters: (string * string) list)
        (headers: (string * string) list)
        (body: (string * string) list)
        (httpMethod: string)
        : Result<ValidatedResource, DomainError> =
        // Validate name
        match ResourceName.Create(Some name) with
        | Error err -> Error err
        | Ok validName ->
            // Validate description
            match ResourceDescription.Create(Some description) with
            | Error err -> Error err
            | Ok validDescription ->
                // Validate base URL
                match BaseUrl.Create(Some baseUrl) with
                | Error err -> Error err
                | Ok validBaseUrl ->
                    // Validate URL parameters
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
                    | Ok validUrlParams ->
                        match validateKeyValuePairs headers with
                        | Error err -> Error err
                        | Ok validHeaders ->
                            match validateKeyValuePairs body with
                            | Error err -> Error err
                            | Ok validBody ->
                                // Validate HTTP method
                                match HttpMethod.Create(httpMethod) with
                                | Error err -> Error err
                                | Ok validHttpMethod ->
                                    let resourceData =
                                        { Id = ResourceId.NewId()
                                          Name = validName
                                          Description = validDescription
                                          SpaceId = spaceId
                                          HttpMethod = validHttpMethod
                                          BaseUrl = validBaseUrl
                                          UrlParameters = validUrlParams
                                          Headers = validHeaders
                                          Body = validBody
                                          CreatedAt = DateTime.UtcNow
                                          UpdatedAt = DateTime.UtcNow
                                          IsDeleted = false }

                                    let resourceCreatedEvent =
                                        ResourceEvents.resourceCreated
                                            actorUserId
                                            resourceData.Id
                                            validName
                                            validDescription
                                            spaceId
                                            validBaseUrl
                                            validUrlParams
                                            validHeaders
                                            validBody
                                            validHttpMethod

                                    Ok
                                        { State = resourceData
                                          UncommittedEvents = [ resourceCreatedEvent :> IDomainEvent ] }

    let updateName
        (actorUserId: UserId)
        (newName: string)
        (resource: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        match ResourceName.Create(Some newName) with
        | Error err -> Error err
        | Ok validName ->
            let oldName = resource.State.Name

            let updatedResourceData =
                { resource.State with
                    Name = validName
                    UpdatedAt = DateTime.UtcNow }

            let nameChangedEvent =
                ResourceEvents.resourceUpdated
                    actorUserId
                    resource.State.Id
                    [ ResourceChange.NameChanged(oldName, validName) ]

            Ok
                { State = updatedResourceData
                  UncommittedEvents = resource.UncommittedEvents @ [ nameChangedEvent :> IDomainEvent ] }

    let updateDescription
        (actorUserId: UserId)
        (newDescription: string)
        (resource: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        match ResourceDescription.Create(Some newDescription) with
        | Error err -> Error err
        | Ok validDescription ->
            let oldDescription = resource.State.Description

            let updatedResourceData =
                { resource.State with
                    Description = validDescription
                    UpdatedAt = DateTime.UtcNow }

            let descriptionChangedEvent =
                ResourceEvents.resourceUpdated
                    actorUserId
                    resource.State.Id
                    [ ResourceChange.DescriptionChanged(oldDescription, validDescription) ]

            Ok
                { State = updatedResourceData
                  UncommittedEvents = resource.UncommittedEvents @ [ descriptionChangedEvent :> IDomainEvent ] }

    let updateBaseUrl
        (actorUserId: UserId)
        (newBaseUrl: string)
        (resource: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        match BaseUrl.Create(Some newBaseUrl) with
        | Error err -> Error err
        | Ok validBaseUrl ->
            let oldBaseUrl = resource.State.BaseUrl

            let updatedResourceData =
                { resource.State with
                    BaseUrl = validBaseUrl
                    UpdatedAt = DateTime.UtcNow }

            let baseUrlChangedEvent =
                ResourceEvents.resourceUpdated
                    actorUserId
                    resource.State.Id
                    [ ResourceChange.BaseUrlChanged(oldBaseUrl, validBaseUrl) ]

            Ok
                { State = updatedResourceData
                  UncommittedEvents = resource.UncommittedEvents @ [ baseUrlChangedEvent :> IDomainEvent ] }

    let private checkAppConflicts
        (apps: AppResourceConflictData list)
        (urlParameters: (string * string) list option)
        (headers: (string * string) list option)
        (body: (string * string) list option)
        : Result<unit, DomainError> =
        BusinessRules.checkAppToResourceConflicts apps urlParameters headers body

    let updateUrlParameters
        (actorUserId: UserId)
        (newUrlParameters: (string * string) list)
        (apps: AppResourceConflictData list)
        (resource: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
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
            match checkAppConflicts apps (Some newUrlParameters) None None with
            | Error err -> Error err
            | Ok() ->
                let oldUrlParams = resource.State.UrlParameters

                let updatedResourceData =
                    { resource.State with
                        UrlParameters = validUrlParams
                        UpdatedAt = DateTime.UtcNow }

                let urlParamsChangedEvent =
                    ResourceEvents.resourceUpdated
                        actorUserId
                        resource.State.Id
                        [ ResourceChange.UrlParametersChanged(oldUrlParams, validUrlParams) ]

                Ok
                    { State = updatedResourceData
                      UncommittedEvents = resource.UncommittedEvents @ [ urlParamsChangedEvent :> IDomainEvent ] }

    let updateHeaders
        (actorUserId: UserId)
        (newHeaders: (string * string) list)
        (apps: AppResourceConflictData list)
        (resource: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
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
            match checkAppConflicts apps None (Some newHeaders) None with
            | Error err -> Error err
            | Ok() ->
                let oldHeaders = resource.State.Headers

                let updatedResourceData =
                    { resource.State with
                        Headers = validHeaders
                        UpdatedAt = DateTime.UtcNow }

                let headersChangedEvent =
                    ResourceEvents.resourceUpdated
                        actorUserId
                        resource.State.Id
                        [ ResourceChange.HeadersChanged(oldHeaders, validHeaders) ]

                Ok
                    { State = updatedResourceData
                      UncommittedEvents = resource.UncommittedEvents @ [ headersChangedEvent :> IDomainEvent ] }

    let updateBody
        (actorUserId: UserId)
        (newBody: (string * string) list)
        (apps: AppResourceConflictData list)
        (resource: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
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
            match checkAppConflicts apps None None (Some newBody) with
            | Error err -> Error err
            | Ok() ->
                let oldBody = resource.State.Body

                let updatedResourceData =
                    { resource.State with
                        Body = validBody
                        UpdatedAt = DateTime.UtcNow }

                let bodyChangedEvent =
                    ResourceEvents.resourceUpdated
                        actorUserId
                        resource.State.Id
                        [ ResourceChange.BodyChanged(oldBody, validBody) ]

                Ok
                    { State = updatedResourceData
                      UncommittedEvents = resource.UncommittedEvents @ [ bodyChangedEvent :> IDomainEvent ] }

    let markForDeletion (actorUserId: UserId) (resource: ValidatedResource) : ValidatedResource =
        let resourceDeletedEvent =
            ResourceEvents.resourceDeleted actorUserId resource.State.Id resource.State.Name

        { resource with
            UncommittedEvents = resource.UncommittedEvents @ [ resourceDeletedEvent :> IDomainEvent ] }

    let restore (actorUserId: UserId) (newName: ResourceName option) (resource: ValidatedResource) : ValidatedResource =
        let finalName = newName |> Option.defaultValue resource.State.Name

        let resourceRestoredEvent =
            ResourceEvents.resourceRestored actorUserId resource.State.Id finalName

        { resource with
            State =
                { resource.State with
                    Name = finalName
                    IsDeleted = false
                    UpdatedAt = DateTime.UtcNow }
            UncommittedEvents = resource.UncommittedEvents @ [ resourceRestoredEvent :> IDomainEvent ] }

    let getUncommittedEvents (resource: ValidatedResource) : IDomainEvent list = resource.UncommittedEvents

    let markEventsAsCommitted (resource: ValidatedResource) : ValidatedResource =
        { resource with UncommittedEvents = [] }

    let getId (resource: Resource) : ResourceId = resource.State.Id

    let getName (resource: Resource) : string = resource.State.Name.Value

    let getDescription (resource: Resource) : string = resource.State.Description.Value

    let getSpaceId (resource: Resource) : SpaceId = resource.State.SpaceId

    let getBaseUrl (resource: Resource) : string = resource.State.BaseUrl.Value

    let getHttpMethod (resource: Resource) : string = resource.State.HttpMethod.ToString()

    let getUrlParameters (resource: Resource) : (string * string) list =
        resource.State.UrlParameters |> List.map (fun kvp -> (kvp.Key, kvp.Value))

    let getHeaders (resource: Resource) : (string * string) list =
        resource.State.Headers |> List.map (fun kvp -> (kvp.Key, kvp.Value))

    let getBody (resource: Resource) : (string * string) list =
        resource.State.Body |> List.map (fun kvp -> (kvp.Key, kvp.Value))

    let getCreatedAt (resource: Resource) : DateTime = resource.State.CreatedAt

    let getUpdatedAt (resource: Resource) : DateTime = resource.State.UpdatedAt

    let toConflictData (resource: Resource) : ResourceAppConflictData =
        { UrlParameters = getUrlParameters resource
          Headers = getHeaders resource
          Body = getBody resource }
