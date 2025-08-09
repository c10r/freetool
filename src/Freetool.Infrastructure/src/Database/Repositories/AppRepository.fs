namespace Freetool.Infrastructure.Database.Repositories

open System.Linq
open System.Threading.Tasks
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Infrastructure.Database

type AppRepository(context: FreetoolDbContext, eventRepository: IEventRepository) =

    interface IAppRepository with

        member _.GetByIdAsync(appId: AppId) : Task<ValidatedApp option> = task {
            let guidId = appId.Value
            let! appData = context.Apps.FirstOrDefaultAsync(fun a -> a.Id.Value = guidId)

            return appData |> Option.ofObj |> Option.map (fun data -> App.fromData data)
        }

        member _.GetByNameAndFolderIdAsync (appName: AppName) (folderId: FolderId) : Task<ValidatedApp option> = task {
            let nameStr = appName.Value
            let folderGuidId = folderId.Value

            let! appData =
                context.Apps.FirstOrDefaultAsync(fun a -> a.Name = nameStr && a.FolderId.Value = folderGuidId)

            return appData |> Option.ofObj |> Option.map (fun data -> App.fromData data)
        }

        member _.GetByFolderIdAsync (folderId: FolderId) (skip: int) (take: int) : Task<ValidatedApp list> = task {
            let folderGuidId = folderId.Value

            let! appDatas =
                context.Apps
                    .Where(fun a -> a.FolderId.Value = folderGuidId)
                    .OrderBy(fun a -> a.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync()

            return appDatas |> Seq.map (fun data -> App.fromData data) |> Seq.toList
        }

        member _.GetAllAsync (skip: int) (take: int) : Task<ValidatedApp list> = task {
            let! appDatas = context.Apps.OrderBy(fun a -> a.CreatedAt).Skip(skip).Take(take).ToListAsync()

            return appDatas |> Seq.map (fun data -> App.fromData data) |> Seq.toList
        }

        member _.AddAsync(app: ValidatedApp) : Task<Result<unit, DomainError>> = task {
            try
                // 1. Save app to database
                context.Apps.Add app.State |> ignore

                // 2. Save events to audit log in SAME transaction
                let events = App.getUncommittedEvents app

                for event in events do
                    do! eventRepository.SaveEventAsync event

                // 3. Commit everything atomically
                let! _ = context.SaveChangesAsync()
                return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to add app: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.UpdateAsync(app: ValidatedApp) : Task<Result<unit, DomainError>> = task {
            try
                let guidId = (App.getId app).Value
                let! existingData = context.Apps.FirstOrDefaultAsync(fun a -> a.Id.Value = guidId)

                match Option.ofObj existingData with
                | None -> return Error(NotFound "App not found")
                | Some _ ->
                    // Update entity directly
                    context.Apps.Update(app.State) |> ignore

                    // Save events to audit log in SAME transaction
                    let events = App.getUncommittedEvents app

                    for event in events do
                        do! eventRepository.SaveEventAsync event

                    // Save to the database
                    let! _ = context.SaveChangesAsync()
                    return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to update app: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.DeleteAsync(appWithDeleteEvent: ValidatedApp) : Task<Result<unit, DomainError>> = task {
            try
                // Soft delete: create updated record with IsDeleted flag
                let updatedData = {
                    appWithDeleteEvent.State with
                        IsDeleted = true
                        UpdatedAt = System.DateTime.UtcNow
                }

                context.Apps.Update(updatedData) |> ignore

                // Save all uncommitted events
                let events = App.getUncommittedEvents appWithDeleteEvent

                for event in events do
                    do! eventRepository.SaveEventAsync event

                // Save to the database
                let! _ = context.SaveChangesAsync()
                return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to delete app: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.ExistsAsync(appId: AppId) : Task<bool> = task {
            let guidId = appId.Value
            return! context.Apps.AnyAsync(fun a -> a.Id.Value = guidId)
        }

        member _.ExistsByNameAndFolderIdAsync (appName: AppName) (folderId: FolderId) : Task<bool> = task {
            let nameStr = appName.Value
            let folderGuidId = folderId.Value
            return! context.Apps.AnyAsync(fun a -> a.Name = nameStr && a.FolderId.Value = folderGuidId)
        }

        member _.GetCountAsync() : Task<int> = task { return! context.Apps.CountAsync() }

        member _.GetCountByFolderIdAsync(folderId: FolderId) : Task<int> = task {
            let folderGuidId = folderId.Value
            return! context.Apps.CountAsync(fun a -> a.FolderId.Value = folderGuidId)
        }

        member _.GetByResourceIdAsync(resourceId: ResourceId) : Task<ValidatedApp list> = task {
            let resourceGuidId = resourceId.Value
            let! appDatas = context.Apps.Where(fun a -> a.ResourceId.Value = resourceGuidId).ToListAsync()
            return appDatas |> Seq.map (fun data -> App.fromData data) |> Seq.toList
        }