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
            let! folderData = context.Folders.FirstOrDefaultAsync(fun f -> f.Id = folderId)
            let folderDataOption = Option.ofObj folderData

            match folderDataOption with
            | None -> return None
            | Some data ->
                // Initialize Children immediately after EF materialization to prevent null reference in GetHashCode
                data.Children <- []
                // Load children for this folder
                let! childrenData =
                    context.Folders
                        .Where(fun f -> f.ParentId = Some(folderId))
                        .OrderBy(fun f -> f.Name)
                        .ToListAsync()

                // Initialize children as empty list first
                data.Children <- []
                // Populate children field
                data.Children <- childrenData |> Seq.toList
                return Some(Folder.fromData data)
        }

        member _.GetChildrenAsync(folderId: FolderId) : Task<ValidatedFolder list> = task {
            let! folderDatas =
                context.Folders
                    .Where(fun f -> f.ParentId = Some(folderId))
                    .OrderBy(fun f -> f.Name)
                    .ToListAsync()

            // Initialize Children for all retrieved folders
            folderDatas |> Seq.iter (fun data -> data.Children <- [])

            return folderDatas |> Seq.map (fun data -> Folder.fromData data) |> Seq.toList
        }

        member _.GetRootFoldersAsync (skip: int) (take: int) : Task<ValidatedFolder list> = task {
            let! folderDatas =
                context.Folders
                    .Where(fun f -> f.ParentId = None)
                    .OrderBy(fun f -> f.Name)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync()

            // Initialize Children for all retrieved folders
            folderDatas |> Seq.iter (fun data -> data.Children <- [])

            // Load children for each root folder
            let folders = []
            let mutable folderList = folders

            for data in folderDatas do
                // Load children for this folder
                let dataId = data.Id

                let! childrenData =
                    context.Folders
                        .Where(fun f -> f.ParentId = Some(dataId))
                        .OrderBy(fun f -> f.Name)
                        .ToListAsync()

                // Initialize children for retrieved child data
                childrenData |> Seq.iter (fun childData -> childData.Children <- [])
                // Populate children field
                data.Children <- childrenData |> Seq.toList
                let folder = Folder.fromData data
                folderList <- folder :: folderList

            return List.rev folderList
        }

        member _.GetAllAsync (skip: int) (take: int) : Task<ValidatedFolder list> = task {
            let! folderDatas = context.Folders.OrderBy(fun f -> f.Name).Skip(skip).Take(take).ToListAsync()

            // Initialize Children for all retrieved folders
            folderDatas |> Seq.iter (fun data -> data.Children <- [])

            // Load children for each folder
            let folders = []
            let mutable folderList = folders

            for data in folderDatas do
                // Load children for this folder
                let dataId = data.Id

                let! childrenData =
                    context.Folders
                        .Where(fun f -> f.ParentId = Some(dataId))
                        .OrderBy(fun f -> f.Name)
                        .ToListAsync()

                // Initialize children for retrieved child data
                childrenData |> Seq.iter (fun childData -> childData.Children <- [])
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
                let folderId = Folder.getId folder
                let! existingData = context.Folders.FirstOrDefaultAsync(fun f -> f.Id = folderId)
                let existingDataOption = Option.ofObj existingData

                match existingDataOption with
                | None -> return Error(NotFound "Folder not found")
                | Some existingEntity ->
                    // Update the already-tracked entity to avoid tracking conflicts
                    context.Entry(existingEntity).CurrentValues.SetValues(folder.State)

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
                let folderId = folderWithDeleteEvent.State.Id.Value

                // Use interpolated SQL with CTE to recursively soft delete all descendants
                let updatedAt = System.DateTime.UtcNow

                let! rowsAffected =
                    context.Database.ExecuteSqlInterpolatedAsync(
                        $"""
                    WITH FolderHierarchy AS (
                        -- Base case: the folder to delete
                        SELECT Id FROM Folders WHERE Id = {folderId}

                        UNION ALL

                        -- Recursive case: all children
                        SELECT f.Id
                        FROM Folders f
                        INNER JOIN FolderHierarchy fh ON f.ParentId = fh.Id
                        WHERE f.IsDeleted = 0
                    )
                    UPDATE Folders
                    SET IsDeleted = 1, UpdatedAt = {updatedAt}
                    WHERE Id IN (SELECT Id FROM FolderHierarchy) AND IsDeleted = 0
                """
                    )

                if rowsAffected = 0 then
                    return Error(NotFound "Folder not found")
                else
                    // Save all uncommitted events
                    let events = Folder.getUncommittedEvents folderWithDeleteEvent

                    for event in events do
                        do! eventRepository.SaveEventAsync event

                    return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to delete folder: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.ExistsAsync(folderId: FolderId) : Task<bool> = task {
            return! context.Folders.AnyAsync(fun f -> f.Id = folderId)
        }

        member _.ExistsByNameInParentAsync (folderName: FolderName) (parentId: FolderId option) : Task<bool> = task {
            match parentId with
            | None ->
                // Check root folders - load to client side then filter
                let! rootFolders = context.Folders.Where(fun f -> f.ParentId = None).ToListAsync()
                return rootFolders |> Seq.exists (fun f -> f.Name = folderName)
            | Some pid ->
                // Check folders with specific parent - load to client side then filter
                let! childFolders = context.Folders.Where(fun f -> f.ParentId = Some pid).ToListAsync()
                return childFolders |> Seq.exists (fun f -> f.Name = folderName)
        }

        member _.GetCountAsync() : Task<int> = task { return! context.Folders.CountAsync() }

        member _.GetRootCountAsync() : Task<int> = task {
            return! context.Folders.CountAsync(fun f -> f.ParentId = None)
        }

        member _.GetChildCountAsync(parentId: FolderId) : Task<int> = task {
            return! context.Folders.CountAsync(fun f -> f.ParentId = Some(parentId))
        }