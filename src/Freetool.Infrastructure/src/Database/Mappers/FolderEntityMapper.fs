namespace Freetool.Infrastructure.Database.Mappers

open System
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Infrastructure.Database

module FolderEntityMapper =
    // Entity -> Domain conversions (database data is trusted, so directly to ValidatedFolder)
    let fromEntity (entity: FolderEntity) : ValidatedFolder =
        let name =
            FolderName.Create(Some entity.Name)
            |> function
                | Ok n -> n
                | Error _ -> failwith "Invalid name in database"

        let parentId =
            if entity.ParentId.HasValue then
                Some(FolderId.FromGuid(entity.ParentId.Value))
            else
                None

        {
            State = {
                Id = FolderId.FromGuid(entity.Id)
                Name = name
                ParentId = parentId
                CreatedAt = entity.CreatedAt
                UpdatedAt = entity.UpdatedAt
            }
            UncommittedEvents = []
        }

    // Domain -> Entity conversions (can convert from any validation state)
    let toEntity (folder: ValidatedFolder) : FolderEntity =
        let entity = FolderEntity()
        entity.Id <- folder.State.Id.Value
        entity.Name <- folder.State.Name.Value

        entity.ParentId <-
            match folder.State.ParentId with
            | Some pid -> Nullable(pid.Value)
            | None -> Nullable()

        entity.CreatedAt <- folder.State.CreatedAt
        entity.UpdatedAt <- folder.State.UpdatedAt
        entity