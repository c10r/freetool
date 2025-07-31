namespace Freetool.Infrastructure.Database.Repositories

open System.Linq
open System.Threading.Tasks
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Infrastructure.Database
open Freetool.Infrastructure.Database.Mappers

type AppRepository(context: FreetoolDbContext) =

    interface IAppRepository with

        member _.GetByIdAsync(appId: AppId) : Task<ValidatedApp option> = task {
            let guidId = appId.Value
            let! appEntity = context.Apps.FirstOrDefaultAsync(fun a -> a.Id = guidId)

            return appEntity |> Option.ofObj |> Option.map AppEntityMapper.fromEntity
        }

        member _.GetByNameAndFolderIdAsync (appName: AppName) (folderId: FolderId) : Task<ValidatedApp option> = task {
            let nameStr = appName.Value
            let folderGuidId = folderId.Value
            let! appEntity = context.Apps.FirstOrDefaultAsync(fun a -> a.Name = nameStr && a.FolderId = folderGuidId)

            return appEntity |> Option.ofObj |> Option.map AppEntityMapper.fromEntity
        }

        member _.GetByFolderIdAsync (folderId: FolderId) (skip: int) (take: int) : Task<ValidatedApp list> = task {
            let folderGuidId = folderId.Value

            let! appEntities =
                context.Apps
                    .Where(fun a -> a.FolderId = folderGuidId)
                    .OrderBy(fun a -> a.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync()

            return appEntities |> Seq.map AppEntityMapper.fromEntity |> Seq.toList
        }

        member _.GetAllAsync (skip: int) (take: int) : Task<ValidatedApp list> = task {
            let! appEntities = context.Apps.OrderBy(fun a -> a.CreatedAt).Skip(skip).Take(take).ToListAsync()

            return appEntities |> Seq.map AppEntityMapper.fromEntity |> Seq.toList
        }

        member _.AddAsync(app: ValidatedApp) : Task<Result<unit, DomainError>> = task {
            try
                let appEntity = AppEntityMapper.toEntity app
                context.Apps.Add appEntity |> ignore
                let! _ = context.SaveChangesAsync()
                return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to add app: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.UpdateAsync(app: ValidatedApp) : Task<Result<unit, DomainError>> = task {
            try
                let guidId = (App.getId app).Value
                let! existingEntity = context.Apps.FirstOrDefaultAsync(fun a -> a.Id = guidId)

                match Option.ofObj existingEntity with
                | None -> return Error(NotFound "App not found")
                | Some entity ->
                    let appData = app.State
                    let inputsJson = appData.Inputs |> List.map AppEntityMapper.inputFromDomain
                    let inputsJsonString = System.Text.Json.JsonSerializer.Serialize(inputsJson)

                    entity.Name <- appData.Name
                    entity.Inputs <- inputsJsonString
                    entity.UpdatedAt <- appData.UpdatedAt

                    let! _ = context.SaveChangesAsync()
                    return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to update app: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.DeleteAsync(appId: AppId) : Task<Result<unit, DomainError>> = task {
            try
                let guidId = appId.Value
                let! appEntity = context.Apps.IgnoreQueryFilters().FirstOrDefaultAsync(fun a -> a.Id = guidId)

                match Option.ofObj appEntity with
                | None -> return Error(NotFound "App not found")
                | Some entity ->
                    // Soft delete: set IsDeleted flag instead of removing entity
                    entity.IsDeleted <- true
                    entity.UpdatedAt <- System.DateTime.UtcNow
                    let! _ = context.SaveChangesAsync()
                    return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to delete app: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.ExistsAsync(appId: AppId) : Task<bool> = task {
            let guidId = appId.Value
            return! context.Apps.AnyAsync(fun a -> a.Id = guidId)
        }

        member _.ExistsByNameAndFolderIdAsync (appName: AppName) (folderId: FolderId) : Task<bool> = task {
            let nameStr = appName.Value
            let folderGuidId = folderId.Value
            return! context.Apps.AnyAsync(fun a -> a.Name = nameStr && a.FolderId = folderGuidId)
        }

        member _.GetCountAsync() : Task<int> = task { return! context.Apps.CountAsync() }

        member _.GetCountByFolderIdAsync(folderId: FolderId) : Task<int> = task {
            let folderGuidId = folderId.Value
            return! context.Apps.CountAsync(fun a -> a.FolderId = folderGuidId)
        }