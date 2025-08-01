namespace Freetool.Infrastructure.Database.Mappers

open System.Text.Json
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Infrastructure.Database

type KeyValuePairData = { Key: string; Value: string }

module ResourceEntityMapper =
    let serializeKeyValuePairs (pairs: (string * string) list) : string =
        let pairData = pairs |> List.map (fun (k, v) -> { Key = k; Value = v })
        JsonSerializer.Serialize(pairData)

    let deserializeKeyValuePairs (json: string) : (string * string) list =
        try
            if System.String.IsNullOrEmpty(json) || json = "[]" then
                []
            else
                let pairData = JsonSerializer.Deserialize<KeyValuePairData[]>(json)
                pairData |> Array.toList |> List.map (fun pair -> (pair.Key, pair.Value))
        with _ -> []

    // Entity -> Domain conversions (database data is trusted, so directly to ValidatedResource)
    let fromEntity (entity: ResourceEntity) : ValidatedResource =
        let name =
            ResourceName.Create(Some entity.Name)
            |> function
                | Ok n -> n
                | Error _ -> failwith "Invalid name in database"

        let description =
            ResourceDescription.Create(Some entity.Description)
            |> function
                | Ok d -> d
                | Error _ -> failwith "Invalid description in database"

        let baseUrl =
            BaseUrl.Create(Some entity.BaseUrl)
            |> function
                | Ok b -> b
                | Error _ -> failwith "Invalid base URL in database"

        let httpMethod =
            HttpMethod.Create(entity.HttpMethod)
            |> function
                | Ok h -> h
                | Error _ -> failwith "Invalid HTTP method in database"

        let urlParameters =
            deserializeKeyValuePairs entity.UrlParameters
            |> List.map (fun (k, v) ->
                KeyValuePair.Create(k, v)
                |> function
                    | Ok kvp -> kvp
                    | Error _ -> failwith "Invalid URL parameter in database")

        let headers =
            deserializeKeyValuePairs entity.Headers
            |> List.map (fun (k, v) ->
                KeyValuePair.Create(k, v)
                |> function
                    | Ok kvp -> kvp
                    | Error _ -> failwith "Invalid header in database")

        let body =
            deserializeKeyValuePairs entity.Body
            |> List.map (fun (k, v) ->
                KeyValuePair.Create(k, v)
                |> function
                    | Ok kvp -> kvp
                    | Error _ -> failwith "Invalid body parameter in database")

        Resource.fromData {
            Id = ResourceId.FromGuid(entity.Id)
            Name = name
            Description = description
            HttpMethod = httpMethod
            BaseUrl = baseUrl
            UrlParameters = urlParameters
            Headers = headers
            Body = body
            CreatedAt = entity.CreatedAt
            UpdatedAt = entity.UpdatedAt
        }

    // Domain -> Entity conversions (can convert from any validation state)
    let toEntity (resource: Resource) : ResourceEntity =
        let entity = ResourceEntity()
        entity.Id <- resource.State.Id.Value
        entity.Name <- resource.State.Name.Value
        entity.Description <- resource.State.Description.Value
        entity.BaseUrl <- resource.State.BaseUrl.Value
        entity.HttpMethod <- resource.State.HttpMethod.ToString()

        entity.UrlParameters <-
            serializeKeyValuePairs (resource.State.UrlParameters |> List.map (fun kvp -> (kvp.Key, kvp.Value)))

        entity.Headers <- serializeKeyValuePairs (resource.State.Headers |> List.map (fun kvp -> (kvp.Key, kvp.Value)))
        entity.Body <- serializeKeyValuePairs (resource.State.Body |> List.map (fun kvp -> (kvp.Key, kvp.Value)))
        entity.CreatedAt <- resource.State.CreatedAt
        entity.UpdatedAt <- resource.State.UpdatedAt
        entity