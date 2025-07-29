namespace Freetool.Infrastructure.Database.Repositories

open System
open System.Linq
open System.Threading.Tasks
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Infrastructure.Database
open Freetool.Infrastructure.Database.Mappers

type FolderRepository(context: FreetoolDbContext) =

    interface IFolderRepository with

        member _.GetByIdAsync(folderId: FolderId) : Task<ValidatedFolder option> = task {
            let guidId = folderId.Value
            let! folderEntity = context.Folders.FirstOrDefaultAsync(fun f -> f.Id = guidId)

            return folderEntity |> Option.ofObj |> Option.map FolderEntityMapper.fromEntity
        }

        member _.GetChildrenAsync(folderId: FolderId) : Task<ValidatedFolder list> = task {
            let guidId = folderId.Value

            let! folderEntities =
                context.Folders
                    .Where(fun f -> f.ParentId.HasValue && f.ParentId.Value = guidId)
                    .OrderBy(fun f -> f.Name)
                    .ToListAsync()

            return folderEntities |> Seq.map FolderEntityMapper.fromEntity |> Seq.toList
        }

        member _.GetRootFoldersAsync (skip: int) (take: int) : Task<ValidatedFolder list> = task {
            let! folderEntities =
                context.Folders
                    .Where(fun f -> not f.ParentId.HasValue)
                    .OrderBy(fun f -> f.Name)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync()

            return folderEntities |> Seq.map FolderEntityMapper.fromEntity |> Seq.toList
        }

        member _.GetChildFoldersAsync (parentId: FolderId) (skip: int) (take: int) : Task<ValidatedFolder list> = task {
            let guidId = parentId.Value

            let! folderEntities =
                context.Folders
                    .Where(fun f -> f.ParentId.HasValue && f.ParentId.Value = guidId)
                    .OrderBy(fun f -> f.Name)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync()

            return folderEntities |> Seq.map FolderEntityMapper.fromEntity |> Seq.toList
        }

        member _.GetAllAsync (skip: int) (take: int) : Task<ValidatedFolder list> = task {
            let! folderEntities = context.Folders.OrderBy(fun f -> f.Name).Skip(skip).Take(take).ToListAsync()

            return folderEntities |> Seq.map FolderEntityMapper.fromEntity |> Seq.toList
        }

        member _.AddAsync(folder: ValidatedFolder) : Task<Result<unit, DomainError>> = task {
            try
                let folderEntity = FolderEntityMapper.toEntity folder
                context.Folders.Add folderEntity |> ignore
                let! _ = context.SaveChangesAsync()
                return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to add folder: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.UpdateAsync(folder: ValidatedFolder) : Task<Result<unit, DomainError>> = task {
            try
                let guidId = (Folder.getId folder).Value
                let! existingEntity = context.Folders.FirstOrDefaultAsync(fun f -> f.Id = guidId)

                match Option.ofObj existingEntity with
                | None -> return Error(NotFound "Folder not found")
                | Some entity ->
                    let (Folder folderData) = folder
                    entity.Name <- folderData.Name.Value

                    entity.ParentId <-
                        match folderData.ParentId with
                        | Some pid -> Nullable(pid.Value)
                        | None -> Nullable()

                    entity.UpdatedAt <- folderData.UpdatedAt

                    let! _ = context.SaveChangesAsync()
                    return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to update folder: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.DeleteAsync(folderId: FolderId) : Task<Result<unit, DomainError>> = task {
            try
                let guidId = folderId.Value
                let! folderEntity = context.Folders.FirstOrDefaultAsync(fun f -> f.Id = guidId)

                match Option.ofObj folderEntity with
                | None -> return Error(NotFound "Folder not found")
                | Some entity ->
                    context.Folders.Remove entity |> ignore
                    let! _ = context.SaveChangesAsync()
                    return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to delete folder: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.ExistsAsync(folderId: FolderId) : Task<bool> = task {
            let guidId = folderId.Value
            return! context.Folders.AnyAsync(fun f -> f.Id = guidId)
        }

        member _.ExistsByNameInParentAsync (folderName: FolderName) (parentId: FolderId option) : Task<bool> = task {
            let nameStr = folderName.Value

            match parentId with
            | None ->
                // Check root folders
                return! context.Folders.AnyAsync(fun f -> f.Name = nameStr && not f.ParentId.HasValue)
            | Some pid ->
                // Check folders with specific parent
                let guidId = pid.Value

                return!
                    context.Folders.AnyAsync(fun f ->
                        f.Name = nameStr && f.ParentId.HasValue && f.ParentId.Value = guidId)
        }

        member _.GetCountAsync() : Task<int> = task { return! context.Folders.CountAsync() }

        member _.GetRootCountAsync() : Task<int> = task {
            return! context.Folders.CountAsync(fun f -> not f.ParentId.HasValue)
        }

        member _.GetChildCountAsync(parentId: FolderId) : Task<int> = task {
            let guidId = parentId.Value
            return! context.Folders.CountAsync(fun f -> f.ParentId.HasValue && f.ParentId.Value = guidId)
        }