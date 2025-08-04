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

type RunRepository(context: FreetoolDbContext, eventRepository: IEventRepository) =

    interface IRunRepository with

        member _.GetByIdAsync(runId: RunId) : Task<ValidatedRun option> = task {
            let guidId = runId.Value
            let! runEntity = context.Runs.FirstOrDefaultAsync(fun r -> r.Id = guidId)

            return runEntity |> Option.ofObj |> Option.map RunEntityMapper.fromEntity
        }

        member _.GetByAppIdAsync (appId: AppId) (skip: int) (take: int) : Task<ValidatedRun list> = task {
            let appGuidId = appId.Value

            let! runEntities =
                context.Runs
                    .Where(fun r -> r.AppId = appGuidId)
                    .OrderByDescending(fun r -> r.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync()

            return runEntities |> Seq.map RunEntityMapper.fromEntity |> Seq.toList
        }

        member _.GetByStatusAsync (status: RunStatus) (skip: int) (take: int) : Task<ValidatedRun list> = task {
            let statusString = status.ToString()

            let! runEntities =
                context.Runs
                    .Where(fun r -> r.Status = statusString)
                    .OrderByDescending(fun r -> r.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync()

            return runEntities |> Seq.map RunEntityMapper.fromEntity |> Seq.toList
        }

        member _.GetByAppIdAndStatusAsync
            (appId: AppId)
            (status: RunStatus)
            (skip: int)
            (take: int)
            : Task<ValidatedRun list> =
            task {
                let appGuidId = appId.Value
                let statusString = status.ToString()

                let! runEntities =
                    context.Runs
                        .Where(fun r -> r.AppId = appGuidId && r.Status = statusString)
                        .OrderByDescending(fun r -> r.CreatedAt)
                        .Skip(skip)
                        .Take(take)
                        .ToListAsync()

                return runEntities |> Seq.map RunEntityMapper.fromEntity |> Seq.toList
            }

        member _.AddAsync(run: ValidatedRun) : Task<Result<unit, DomainError>> = task {
            use transaction = context.Database.BeginTransaction()

            try
                // Convert to entity and add to context
                let entity = RunEntityMapper.toEntity run
                context.Runs.Add(entity) |> ignore

                // Save changes to database
                let! _ = context.SaveChangesAsync()

                // Save domain events
                let events = Run.getUncommittedEvents run

                for event in events do
                    do! eventRepository.SaveEventAsync event

                // Commit transaction
                transaction.Commit()
                return Ok()
            with ex ->
                transaction.Rollback()
                return Error(InvalidOperation $"Failed to save run: {ex.Message}")
        }

        member _.UpdateAsync(run: ValidatedRun) : Task<Result<unit, DomainError>> = task {
            use transaction = context.Database.BeginTransaction()

            try
                let runId = (Run.getId run).Value

                let! existingEntity = context.Runs.FirstOrDefaultAsync(fun r -> r.Id = runId)
                let existingEntityOption = Option.ofObj existingEntity

                match existingEntityOption with
                | None ->
                    transaction.Rollback()
                    return Error(NotFound "Run not found")
                | Some existingEntity ->
                    // Update entity with new values
                    let updatedEntity = RunEntityMapper.toEntity run
                    existingEntity.Status <- updatedEntity.Status
                    existingEntity.InputValues <- updatedEntity.InputValues
                    existingEntity.ExecutableRequest <- updatedEntity.ExecutableRequest
                    existingEntity.Response <- updatedEntity.Response
                    existingEntity.ErrorMessage <- updatedEntity.ErrorMessage
                    existingEntity.StartedAt <- updatedEntity.StartedAt
                    existingEntity.CompletedAt <- updatedEntity.CompletedAt

                    // Save changes to database
                    let! _ = context.SaveChangesAsync()

                    // Save domain events
                    let events = Run.getUncommittedEvents run

                    for event in events do
                        do! eventRepository.SaveEventAsync event

                    // Commit transaction
                    transaction.Commit()
                    return Ok()
            with ex ->
                transaction.Rollback()
                return Error(InvalidOperation $"Failed to update run: {ex.Message}")
        }

        member _.ExistsAsync(runId: RunId) : Task<bool> = task {
            let guidId = runId.Value
            return! context.Runs.AnyAsync(fun r -> r.Id = guidId)
        }

        member _.GetCountAsync() : Task<int> = task { return! context.Runs.CountAsync() }

        member _.GetCountByAppIdAsync(appId: AppId) : Task<int> = task {
            let appGuidId = appId.Value
            return! context.Runs.CountAsync(fun r -> r.AppId = appGuidId)
        }

        member _.GetCountByStatusAsync(status: RunStatus) : Task<int> = task {
            let statusString = status.ToString()
            return! context.Runs.CountAsync(fun r -> r.Status = statusString)
        }

        member _.GetCountByAppIdAndStatusAsync (appId: AppId) (status: RunStatus) : Task<int> = task {
            let appGuidId = appId.Value
            let statusString = status.ToString()
            return! context.Runs.CountAsync(fun r -> r.AppId = appGuidId && r.Status = statusString)
        }