namespace Freetool.Infrastructure.Database.Repositories

open System.Linq
open System.Threading.Tasks
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Infrastructure.Database

type FolderRepository(context: FreetoolDbContext, eventRepository: IEventRepository) =

    interface IFolderRepository with

        member _.GetByIdAsync(folderId: FolderId) : Task<ValidatedFolder option> = task {
            let guidId = folderId.Value
            let! folderData = context.Folders.FirstOrDefaultAsync(fun f -> f.Id.Value = guidId)

            match Option.ofObj folderData with
            | None -> return None
            | Some data ->
                // Load children for this folder
                let! childrenData =
                    context.Folders
                        .Where(fun f -> f.ParentId.IsSome && f.ParentId.Value.Value = guidId)
                        .OrderBy(fun f -> f.Name)
                        .ToListAsync()

                // Initialize children as empty list first
                data.Children <- []
                // Populate children field
                data.Children <- childrenData |> Seq.toList
                return Some(Folder.fromData data)
        }

        member _.GetChildrenAsync(folderId: FolderId) : Task<ValidatedFolder list> = task {
            let guidId = folderId.Value

            let! folderDatas =
                context.Folders
                    .Where(fun f -> f.ParentId.IsSome && f.ParentId.Value.Value = guidId)
                    .OrderBy(fun f -> f.Name)
                    .ToListAsync()

            return folderDatas |> Seq.map (fun data -> Folder.fromData data) |> Seq.toList
        }

        member _.GetRootFoldersAsync (skip: int) (take: int) : Task<ValidatedFolder list> = task {
            let! folderDatas =
                context.Folders
                    .Where(fun f -> f.ParentId.IsNone)
                    .OrderBy(fun f -> f.Name)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync()

            // Load children for each root folder
            let folders = []
            let mutable folderList = folders

            for data in folderDatas do
                let guidId = data.Id.Value

                // Load children for this folder
                let! childrenData =
                    context.Folders
                        .Where(fun f -> f.ParentId.IsSome && f.ParentId.Value.Value = guidId)
                        .OrderBy(fun f -> f.Name)
                        .ToListAsync()

                // Initialize children as empty list first
                data.Children <- []
                // Populate children field
                data.Children <- childrenData |> Seq.toList
                let folder = Folder.fromData data
                folderList <- folder :: folderList

            return List.rev folderList
        }

        member _.GetChildFoldersAsync (parentId: FolderId) (skip: int) (take: int) : Task<ValidatedFolder list> = task {
            let guidId = parentId.Value

            let! folderDatas =
                context.Folders
                    .Where(fun f -> f.ParentId.IsSome && f.ParentId.Value.Value = guidId)
                    .OrderBy(fun f -> f.Name)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync()

            return folderDatas |> Seq.map (fun data -> Folder.fromData data) |> Seq.toList
        }

        member _.GetAllAsync (skip: int) (take: int) : Task<ValidatedFolder list> = task {
            let! folderDatas = context.Folders.OrderBy(fun f -> f.Name).Skip(skip).Take(take).ToListAsync()

            // Load children for each folder
            let folders = []
            let mutable folderList = folders

            for data in folderDatas do
                let guidId = data.Id.Value

                // Load children for this folder
                let! childrenData =
                    context.Folders
                        .Where(fun f -> f.ParentId.IsSome && f.ParentId.Value.Value = guidId)
                        .OrderBy(fun f -> f.Name)
                        .ToListAsync()

                // Initialize children as empty list first
                data.Children <- []
                // Populate children field
                data.Children <- childrenData |> Seq.toList
                let folder = Folder.fromData data
                folderList <- folder :: folderList

            return List.rev folderList
        }

        member _.AddAsync(folder: ValidatedFolder) : Task<Result<unit, DomainError>> = task {
            try
                // 1. Add folder
                context.Folders.Add folder.State |> ignore

                // 2. Save events to audit log
                let events = Folder.getUncommittedEvents folder

                for event in events do
                    do! eventRepository.SaveEventAsync event

                // 3. Commit transaction
                let! _ = context.SaveChangesAsync()
                return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to add folder: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.UpdateAsync(folder: ValidatedFolder) : Task<Result<unit, DomainError>> = task {
            try
                let guidId = (Folder.getId folder).Value
                let! existingData = context.Folders.FirstOrDefaultAsync(fun f -> f.Id.Value = guidId)

                match Option.ofObj existingData with
                | None -> return Error(NotFound "Folder not found")
                | Some _ ->
                    // Update entity directly
                    context.Folders.Update(folder.State) |> ignore

                    // Save events to audit log
                    let events = Folder.getUncommittedEvents folder

                    for event in events do
                        do! eventRepository.SaveEventAsync event

                    // Commit transaction
                    let! _ = context.SaveChangesAsync()
                    return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to update folder: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.DeleteAsync(folderWithDeleteEvent: ValidatedFolder) : Task<Result<unit, DomainError>> = task {
            try
                // Soft delete: create updated record with IsDeleted flag
                let updatedData = {
                    folderWithDeleteEvent.State with
                        IsDeleted = true
                        UpdatedAt = System.DateTime.UtcNow
                }

                context.Folders.Update(updatedData) |> ignore

                // Save all uncommitted events
                let events = Folder.getUncommittedEvents folderWithDeleteEvent

                for event in events do
                    do! eventRepository.SaveEventAsync event

                // Commit transaction
                let! _ = context.SaveChangesAsync()
                return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to delete folder: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.ExistsAsync(folderId: FolderId) : Task<bool> = task {
            let guidId = folderId.Value
            return! context.Folders.AnyAsync(fun f -> f.Id.Value = guidId)
        }

        member _.ExistsByNameInParentAsync (folderName: FolderName) (parentId: FolderId option) : Task<bool> = task {
            let nameStr = folderName.Value

            match parentId with
            | None ->
                // Check root folders
                return! context.Folders.AnyAsync(fun f -> f.Name.Value = nameStr && f.ParentId.IsNone)
            | Some pid ->
                // Check folders with specific parent
                let guidId = pid.Value

                return!
                    context.Folders.AnyAsync(fun f ->
                        f.Name.Value = nameStr && f.ParentId.IsSome && f.ParentId.Value.Value = guidId)
        }

        member _.GetCountAsync() : Task<int> = task { return! context.Folders.CountAsync() }

        member _.GetRootCountAsync() : Task<int> = task {
            return! context.Folders.CountAsync(fun f -> f.ParentId.IsNone)
        }

        member _.GetChildCountAsync(parentId: FolderId) : Task<int> = task {
            let guidId = parentId.Value
            return! context.Folders.CountAsync(fun f -> f.ParentId.IsSome && f.ParentId.Value.Value = guidId)
        }