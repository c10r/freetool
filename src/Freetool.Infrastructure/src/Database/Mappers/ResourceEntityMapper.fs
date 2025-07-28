namespace Freetool.Infrastructure.Database.Mappers

open System.Text.Json
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Infrastructure.Database

type KeyValuePairData = { Key: string; Value: string }

module ResourceEntityMapper =
    // Helper functions for JSON serialization/deserialization
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
        // Since we're reading from the database, we trust the data is valid
        // We reconstruct the value objects from their string representations
        let name =
            ResourceName.Create(entity.Name)
            |> function
                | Ok n -> n
                | Error _ -> failwith "Invalid name in database"

        let description =
            ResourceDescription.Create(entity.Description)
            |> function
                | Ok d -> d
                | Error _ -> failwith "Invalid description in database"

        let baseUrl =
            BaseUrl.Create(entity.BaseUrl)
            |> function
                | Ok b -> b
                | Error _ -> failwith "Invalid base URL in database"

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

        Resource {
            Id = ResourceId.FromGuid(entity.Id)
            Name = name
            Description = description
            BaseUrl = baseUrl
            UrlParameters = urlParameters
            Headers = headers
            Body = body
            CreatedAt = entity.CreatedAt
            UpdatedAt = entity.UpdatedAt
        }

    // Domain -> Entity conversions (can convert from any validation state)
    let toEntity (Resource resourceData: Resource<'State>) : ResourceEntity =
        let entity = ResourceEntity()
        entity.Id <- resourceData.Id.Value
        entity.Name <- resourceData.Name.Value
        entity.Description <- resourceData.Description.Value
        entity.BaseUrl <- resourceData.BaseUrl.Value

        entity.UrlParameters <-
            serializeKeyValuePairs (resourceData.UrlParameters |> List.map (fun kvp -> (kvp.Key, kvp.Value)))

        entity.Headers <- serializeKeyValuePairs (resourceData.Headers |> List.map (fun kvp -> (kvp.Key, kvp.Value)))
        entity.Body <- serializeKeyValuePairs (resourceData.Body |> List.map (fun kvp -> (kvp.Key, kvp.Value)))
        entity.CreatedAt <- resourceData.CreatedAt
        entity.UpdatedAt <- resourceData.UpdatedAt
        entity