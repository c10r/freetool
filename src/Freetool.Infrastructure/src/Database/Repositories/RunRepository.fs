namespace Freetool.Infrastructure.Database.Repositories

open System.Linq
open System.Threading.Tasks
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Infrastructure.Database

type RunRepository(context: FreetoolDbContext, eventRepository: IEventRepository) =

    interface IRunRepository with

        member _.GetByIdAsync(runId: RunId) : Task<ValidatedRun option> = task {
            let guidId = runId.Value
            let! runData = context.Runs.FirstOrDefaultAsync(fun r -> r.Id.Value = guidId)

            return runData |> Option.ofObj |> Option.map (fun data -> Run.fromData data)
        }

        member _.GetByAppIdAsync (appId: AppId) (skip: int) (take: int) : Task<ValidatedRun list> = task {
            let appGuidId = appId.Value

            let! runDatas =
                context.Runs
                    .Where(fun r -> r.AppId.Value = appGuidId)
                    .OrderByDescending(fun r -> r.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync()

            return runDatas |> Seq.map (fun data -> Run.fromData data) |> Seq.toList
        }

        member _.GetByStatusAsync (status: RunStatus) (skip: int) (take: int) : Task<ValidatedRun list> = task {
            let! runDatas =
                context.Runs
                    .Where(fun r -> r.Status = status)
                    .OrderByDescending(fun r -> r.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync()

            return runDatas |> Seq.map (fun data -> Run.fromData data) |> Seq.toList
        }

        member _.GetByAppIdAndStatusAsync
            (appId: AppId)
            (status: RunStatus)
            (skip: int)
            (take: int)
            : Task<ValidatedRun list> =
            task {
                let appGuidId = appId.Value

                let! runDatas =
                    context.Runs
                        .Where(fun r -> r.AppId.Value = appGuidId && r.Status = status)
                        .OrderByDescending(fun r -> r.CreatedAt)
                        .Skip(skip)
                        .Take(take)
                        .ToListAsync()

                return runDatas |> Seq.map (fun data -> Run.fromData data) |> Seq.toList
            }

        member _.AddAsync(run: ValidatedRun) : Task<Result<unit, DomainError>> = task {
            use transaction = context.Database.BeginTransaction()

            try
                // Add domain entity directly to context
                context.Runs.Add(run.State) |> ignore

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

                let! existingData = context.Runs.FirstOrDefaultAsync(fun r -> r.Id.Value = runId)
                let existingDataOption = Option.ofObj existingData

                match existingDataOption with
                | None ->
                    transaction.Rollback()
                    return Error(NotFound "Run not found")
                | Some _ ->
                    // Update entity directly
                    context.Runs.Update(run.State) |> ignore

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
            return! context.Runs.AnyAsync(fun r -> r.Id.Value = guidId)
        }

        member _.GetCountAsync() : Task<int> = task { return! context.Runs.CountAsync() }

        member _.GetCountByAppIdAsync(appId: AppId) : Task<int> = task {
            let appGuidId = appId.Value
            return! context.Runs.CountAsync(fun r -> r.AppId.Value = appGuidId)
        }

        member _.GetCountByStatusAsync(status: RunStatus) : Task<int> = task {
            return! context.Runs.CountAsync(fun r -> r.Status = status)
        }

        member _.GetCountByAppIdAndStatusAsync (appId: AppId) (status: RunStatus) : Task<int> = task {
            let appGuidId = appId.Value
            return! context.Runs.CountAsync(fun r -> r.AppId.Value = appGuidId && r.Status = status)
        }