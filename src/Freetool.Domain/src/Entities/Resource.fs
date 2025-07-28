namespace Freetool.Domain.Entities

open System
open Freetool.Domain
open Freetool.Domain.ValueObjects

// Core resource data that's shared across all states
type ResourceData = {
    Id: ResourceId
    Name: ResourceName
    Description: ResourceDescription
    BaseUrl: BaseUrl
    UrlParameters: KeyValuePair list
    Headers: KeyValuePair list
    Body: KeyValuePair list
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

// Resource type parameterized by validation state
type Resource<'State> =
    | Resource of ResourceData

    interface IEntity<ResourceId> with
        member this.Id =
            let (Resource resourceData) = this
            resourceData.Id

// Type aliases for clarity
type UnvalidatedResource = Resource<Unvalidated> // From DTOs - potentially unsafe
type ValidatedResource = Resource<Validated> // Validated domain model and database data

module Resource =
    // Create a new validated resource from primitive values
    let create
        (name: string)
        (description: string)
        (baseUrl: string)
        (urlParameters: (string * string) list)
        (headers: (string * string) list)
        (body: (string * string) list)
        : Result<ValidatedResource, DomainError> =
        // Validate name
        match ResourceName.Create(name) with
        | Error err -> Error err
        | Ok validName ->
            // Validate description
            match ResourceDescription.Create(description) with
            | Error err -> Error err
            | Ok validDescription ->
                // Validate base URL
                match BaseUrl.Create(baseUrl) with
                | Error err -> Error err
                | Ok validBaseUrl ->
                    // Validate URL parameters
                    let validateKeyValuePairs pairs =
                        pairs
                        |> List.fold
                            (fun acc (key, value) ->
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
                                Ok(
                                    Resource {
                                        Id = ResourceId.NewId()
                                        Name = validName
                                        Description = validDescription
                                        BaseUrl = validBaseUrl
                                        UrlParameters = validUrlParams
                                        Headers = validHeaders
                                        Body = validBody
                                        CreatedAt = DateTime.UtcNow
                                        UpdatedAt = DateTime.UtcNow
                                    }
                                )

    // Business logic operations on validated resources
    let updateName
        (newName: string)
        (Resource resourceData: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        match ResourceName.Create(newName) with
        | Error err -> Error err
        | Ok validName ->
            Ok(
                Resource {
                    resourceData with
                        Name = validName
                        UpdatedAt = DateTime.UtcNow
                }
            )

    let updateDescription
        (newDescription: string)
        (Resource resourceData: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        match ResourceDescription.Create(newDescription) with
        | Error err -> Error err
        | Ok validDescription ->
            Ok(
                Resource {
                    resourceData with
                        Description = validDescription
                        UpdatedAt = DateTime.UtcNow
                }
            )

    let updateBaseUrl
        (newBaseUrl: string)
        (Resource resourceData: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        match BaseUrl.Create(newBaseUrl) with
        | Error err -> Error err
        | Ok validBaseUrl ->
            Ok(
                Resource {
                    resourceData with
                        BaseUrl = validBaseUrl
                        UpdatedAt = DateTime.UtcNow
                }
            )

    let updateUrlParameters
        (newUrlParameters: (string * string) list)
        (Resource resourceData: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        let validateKeyValuePairs pairs =
            pairs
            |> List.fold
                (fun acc (key, value) ->
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
            Ok(
                Resource {
                    resourceData with
                        UrlParameters = validUrlParams
                        UpdatedAt = DateTime.UtcNow
                }
            )

    let updateHeaders
        (newHeaders: (string * string) list)
        (Resource resourceData: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        let validateKeyValuePairs pairs =
            pairs
            |> List.fold
                (fun acc (key, value) ->
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
            Ok(
                Resource {
                    resourceData with
                        Headers = validHeaders
                        UpdatedAt = DateTime.UtcNow
                }
            )

    let updateBody
        (newBody: (string * string) list)
        (Resource resourceData: ValidatedResource)
        : Result<ValidatedResource, DomainError> =
        let validateKeyValuePairs pairs =
            pairs
            |> List.fold
                (fun acc (key, value) ->
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
            Ok(
                Resource {
                    resourceData with
                        Body = validBody
                        UpdatedAt = DateTime.UtcNow
                }
            )

    // Utility functions for accessing data from any state
    let getId (Resource resourceData: Resource<'State>) : ResourceId = resourceData.Id

    let getName (Resource resourceData: Resource<'State>) : string = resourceData.Name.Value

    let getDescription (Resource resourceData: Resource<'State>) : string = resourceData.Description.Value

    let getBaseUrl (Resource resourceData: Resource<'State>) : string = resourceData.BaseUrl.Value

    let getUrlParameters (Resource resourceData: Resource<'State>) : (string * string) list =
        resourceData.UrlParameters |> List.map (fun kvp -> (kvp.Key, kvp.Value))

    let getHeaders (Resource resourceData: Resource<'State>) : (string * string) list =
        resourceData.Headers |> List.map (fun kvp -> (kvp.Key, kvp.Value))

    let getBody (Resource resourceData: Resource<'State>) : (string * string) list =
        resourceData.Body |> List.map (fun kvp -> (kvp.Key, kvp.Value))

    let getCreatedAt (Resource resourceData: Resource<'State>) : DateTime = resourceData.CreatedAt

    let getUpdatedAt (Resource resourceData: Resource<'State>) : DateTime = resourceData.UpdatedAt