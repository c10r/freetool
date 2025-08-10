namespace Freetool.Infrastructure.Database.Repositories

open System.Linq
open System.Threading.Tasks
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Infrastructure.Database

type ResourceRepository(context: FreetoolDbContext, eventRepository: IEventRepository) =

    interface IResourceRepository with

        member _.GetByIdAsync(resourceId: ResourceId) : Task<ValidatedResource option> = task {
            let guidId = resourceId.Value
            let! resourceData = context.Resources.FirstOrDefaultAsync(fun r -> r.Id.Value = guidId)

            return resourceData |> Option.ofObj |> Option.map (fun data -> Resource.fromData data)
        }

        member _.GetAllAsync (skip: int) (take: int) : Task<ValidatedResource list> = task {
            let! resourceDatas =
                context.Resources
                    .OrderBy(fun r -> r.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync()

            return resourceDatas |> Seq.map (fun data -> Resource.fromData data) |> Seq.toList
        }

        member _.AddAsync(resource: ValidatedResource) : Task<Result<unit, DomainError>> = task {
            try
                // 1. Save resource to database
                context.Resources.Add resource.State |> ignore

                // 2. Save event to database
                let events = Resource.getUncommittedEvents resource

                for event in events do
                    do! eventRepository.SaveEventAsync event

                // 3. Save to database
                let! _ = context.SaveChangesAsync()
                return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to add resource: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.UpdateAsync(resource: ValidatedResource) : Task<Result<unit, DomainError>> = task {
            try
                let guidId = (Resource.getId resource).Value
                let! existingData = context.Resources.FirstOrDefaultAsync(fun r -> r.Id.Value = guidId)

                match Option.ofObj existingData with
                | None -> return Error(NotFound "Resource not found")
                | Some existingEntity ->
                    // Update the already-tracked entity to avoid tracking conflicts
                    context.Entry(existingEntity).CurrentValues.SetValues(resource.State)

                    // Save event to database
                    let events = Resource.getUncommittedEvents resource

                    for event in events do
                        do! eventRepository.SaveEventAsync event

                    // Commit transaction
                    let! _ = context.SaveChangesAsync()
                    return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to update resource: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.DeleteAsync(resourceWithDeleteEvent: ValidatedResource) : Task<Result<unit, DomainError>> = task {
            try
                // Soft delete: create updated record with IsDeleted flag
                let updatedData = {
                    resourceWithDeleteEvent.State with
                        IsDeleted = true
                        UpdatedAt = System.DateTime.UtcNow
                }

                context.Resources.Update(updatedData) |> ignore

                // Save all uncommitted events
                let events = Resource.getUncommittedEvents resourceWithDeleteEvent

                for event in events do
                    do! eventRepository.SaveEventAsync event

                // Commit transaction
                let! _ = context.SaveChangesAsync()
                return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to delete resource: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.ExistsAsync(resourceId: ResourceId) : Task<bool> = task {
            let guidId = resourceId.Value
            return! context.Resources.AnyAsync(fun r -> r.Id.Value = guidId)
        }

        member _.ExistsByNameAsync(resourceName: ResourceName) : Task<bool> = task {
            let nameStr = resourceName.Value
            return! context.Resources.AnyAsync(fun r -> r.Name = resourceName)
        }

        member _.GetCountAsync() : Task<int> = task { return! context.Resources.CountAsync() }