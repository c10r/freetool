namespace Freetool.Infrastructure.Database.Mappers

open System
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Infrastructure.Database

module FolderEntityMapper =
    // Entity -> Domain conversions (database data is trusted, so directly to ValidatedFolder)
    let fromEntity (entity: FolderEntity) : ValidatedFolder =
        // Since we're reading from the database, we trust the data is valid
        let name =
            FolderName.Create(entity.Name)
            |> function
                | Ok n -> n
                | Error _ -> failwith "Invalid name in database"

        let parentId =
            if entity.ParentId.HasValue then
                Some(FolderId.FromGuid(entity.ParentId.Value))
            else
                None

        Folder {
            Id = FolderId.FromGuid(entity.Id)
            Name = name
            ParentId = parentId
            CreatedAt = entity.CreatedAt
            UpdatedAt = entity.UpdatedAt
        }

    // Domain -> Entity conversions (can convert from any validation state)
    let toEntity (Folder folderData: Folder<'State>) : FolderEntity =
        let entity = FolderEntity()
        entity.Id <- folderData.Id.Value
        entity.Name <- folderData.Name.Value

        entity.ParentId <-
            match folderData.ParentId with
            | Some pid -> Nullable(pid.Value)
            | None -> Nullable()

        entity.CreatedAt <- folderData.CreatedAt
        entity.UpdatedAt <- folderData.UpdatedAt
        entity