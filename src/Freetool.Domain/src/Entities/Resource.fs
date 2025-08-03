namespace Freetool.Domain.Entities

open System
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events

type ResourceData = {
    Id: ResourceId
    Name: ResourceName
    Description: ResourceDescription
    HttpMethod: HttpMethod
    BaseUrl: BaseUrl
    UrlParameters: KeyValuePair list
    Headers: KeyValuePair list
    Body: KeyValuePair list
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

type Resource = EventSourcingAggregate<ResourceData>

module ResourceAggregateHelpers =
    let getEntityId (resource: Resource) : ResourceId = resource.State.Id

    let implementsIEntity (resource: Resource) =
        { new IEntity<ResourceId> with
            member _.Id = resource.State.Id
        }

// Type aliases for clarity
type UnvalidatedResource = Resource // From DTOs - potentially unsafe
type ValidatedResource = Resource // Validated domain model and database data

module Resource =
    let fromData (resourceData: ResourceData) : ValidatedResource = {
        State = resourceData
        UncommittedEvents = []
    }

    let create
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
                                    let resourceData = {
                                        Id = ResourceId.NewId()
                                        Name = validName
                                        Description = validDescription
                                        HttpMethod = validHttpMethod
                                        BaseUrl = validBaseUrl
                                        UrlParameters = validUrlParams
                                        Headers = validHeaders
                                        Body = validBody
                                        CreatedAt = DateTime.UtcNow
                                        UpdatedAt = DateTime.UtcNow
                                    }

                                    let resourceCreatedEvent =
                                        ResourceEvents.resourceCreated
                                            resourceData.Id
                                            validName
                                            validDescription
                                            validBaseUrl
                                            validUrlParams
                                            validHeaders
                                            validBody
                                            validHttpMethod

                                    Ok {
                                        State = resourceData
                                        UncommittedEvents = [ resourceCreatedEvent :> IDomainEvent ]
                                    }

    let updateName (newName: string) (resource: ValidatedResource) : Result<ValidatedResource, DomainError> =
        match ResourceName.Create(Some newName) with
        | Error err -> Error err
        | Ok validName ->
            let oldName = resource.State.Name

            let updatedResourceData = {
                resource.State with
                    Name = validName
                    UpdatedAt = DateTime.UtcNow
            }

            let nameChangedEvent =
                ResourceEvents.resourceUpdated resource.State.Id [ ResourceChange.NameChanged(oldName, validName) ]

            Ok {
                State = updatedResourceData
                UncommittedEvents = resource.UncommittedEvents @ [ nameChangedEvent :> IDomainEvent ]
            }

    let updateDescription
        (newDescription: string)
        (resource: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        match ResourceDescription.Create(Some newDescription) with
        | Error err -> Error err
        | Ok validDescription ->
            let oldDescription = resource.State.Description

            let updatedResourceData = {
                resource.State with
                    Description = validDescription
                    UpdatedAt = DateTime.UtcNow
            }

            let descriptionChangedEvent =
                ResourceEvents.resourceUpdated resource.State.Id [
                    ResourceChange.DescriptionChanged(oldDescription, validDescription)
                ]

            Ok {
                State = updatedResourceData
                UncommittedEvents = resource.UncommittedEvents @ [ descriptionChangedEvent :> IDomainEvent ]
            }

    let updateBaseUrl (newBaseUrl: string) (resource: ValidatedResource) : Result<ValidatedResource, DomainError> =
        match BaseUrl.Create(Some newBaseUrl) with
        | Error err -> Error err
        | Ok validBaseUrl ->
            let oldBaseUrl = resource.State.BaseUrl

            let updatedResourceData = {
                resource.State with
                    BaseUrl = validBaseUrl
                    UpdatedAt = DateTime.UtcNow
            }

            let baseUrlChangedEvent =
                ResourceEvents.resourceUpdated resource.State.Id [
                    ResourceChange.BaseUrlChanged(oldBaseUrl, validBaseUrl)
                ]

            Ok {
                State = updatedResourceData
                UncommittedEvents = resource.UncommittedEvents @ [ baseUrlChangedEvent :> IDomainEvent ]
            }

    let private checkAppConflicts
        (apps: AppResourceConflictData list)
        (urlParameters: (string * string) list option)
        (headers: (string * string) list option)
        (body: (string * string) list option)
        : Result<unit, DomainError> =
        BusinessRules.checkAppToResourceConflicts apps urlParameters headers body

    let updateUrlParameters
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

                let updatedResourceData = {
                    resource.State with
                        UrlParameters = validUrlParams
                        UpdatedAt = DateTime.UtcNow
                }

                let urlParamsChangedEvent =
                    ResourceEvents.resourceUpdated resource.State.Id [
                        ResourceChange.UrlParametersChanged(oldUrlParams, validUrlParams)
                    ]

                Ok {
                    State = updatedResourceData
                    UncommittedEvents = resource.UncommittedEvents @ [ urlParamsChangedEvent :> IDomainEvent ]
                }

    let updateHeaders
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

                let updatedResourceData = {
                    resource.State with
                        Headers = validHeaders
                        UpdatedAt = DateTime.UtcNow
                }

                let headersChangedEvent =
                    ResourceEvents.resourceUpdated resource.State.Id [
                        ResourceChange.HeadersChanged(oldHeaders, validHeaders)
                    ]

                Ok {
                    State = updatedResourceData
                    UncommittedEvents = resource.UncommittedEvents @ [ headersChangedEvent :> IDomainEvent ]
                }

    let updateBody
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

                let updatedResourceData = {
                    resource.State with
                        Body = validBody
                        UpdatedAt = DateTime.UtcNow
                }

                let bodyChangedEvent =
                    ResourceEvents.resourceUpdated resource.State.Id [ ResourceChange.BodyChanged(oldBody, validBody) ]

                Ok {
                    State = updatedResourceData
                    UncommittedEvents = resource.UncommittedEvents @ [ bodyChangedEvent :> IDomainEvent ]
                }

    let markForDeletion (resource: ValidatedResource) : ValidatedResource =
        let resourceDeletedEvent = ResourceEvents.resourceDeleted resource.State.Id

        {
            resource with
                UncommittedEvents = resource.UncommittedEvents @ [ resourceDeletedEvent :> IDomainEvent ]
        }

    let getUncommittedEvents (resource: ValidatedResource) : IDomainEvent list = resource.UncommittedEvents

    let markEventsAsCommitted (resource: ValidatedResource) : ValidatedResource = {
        resource with
            UncommittedEvents = []
    }

    let getId (resource: Resource) : ResourceId = resource.State.Id

    let getName (resource: Resource) : string = resource.State.Name.Value

    let getDescription (resource: Resource) : string = resource.State.Description.Value

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

    let toConflictData (resource: Resource) : ResourceAppConflictData = {
        UrlParameters = getUrlParameters resource
        Headers = getHeaders resource
        Body = getBody resource
    }