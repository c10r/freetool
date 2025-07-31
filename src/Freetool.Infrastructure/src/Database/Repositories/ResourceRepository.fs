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

type ResourceRepository(context: FreetoolDbContext) =

    interface IResourceRepository with

        member _.GetByIdAsync(resourceId: ResourceId) : Task<ValidatedResource option> = task {
            let guidId = resourceId.Value
            let! resourceEntity = context.Resources.FirstOrDefaultAsync(fun r -> r.Id = guidId)

            return resourceEntity |> Option.ofObj |> Option.map ResourceEntityMapper.fromEntity
        }

        member _.GetAllAsync (skip: int) (take: int) : Task<ValidatedResource list> = task {
            let! resourceEntities =
                context.Resources
                    .OrderBy(fun r -> r.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync()

            return resourceEntities |> Seq.map ResourceEntityMapper.fromEntity |> Seq.toList
        }

        member _.AddAsync(resource: ValidatedResource) : Task<Result<unit, DomainError>> = task {
            try
                let resourceEntity = ResourceEntityMapper.toEntity resource
                context.Resources.Add resourceEntity |> ignore
                let! _ = context.SaveChangesAsync()
                return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to add resource: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.UpdateAsync(resource: ValidatedResource) : Task<Result<unit, DomainError>> = task {
            try
                let guidId = (Resource.getId resource).Value
                let! existingEntity = context.Resources.FirstOrDefaultAsync(fun r -> r.Id = guidId)

                match Option.ofObj existingEntity with
                | None -> return Error(NotFound "Resource not found")
                | Some entity ->
                    let resourceData = resource.State
                    entity.Name <- resourceData.Name.Value
                    entity.Description <- resourceData.Description.Value
                    entity.BaseUrl <- resourceData.BaseUrl.Value

                    entity.UrlParameters <-
                        ResourceEntityMapper.serializeKeyValuePairs (
                            resourceData.UrlParameters |> List.map (fun kvp -> (kvp.Key, kvp.Value))
                        )

                    entity.Headers <-
                        ResourceEntityMapper.serializeKeyValuePairs (
                            resourceData.Headers |> List.map (fun kvp -> (kvp.Key, kvp.Value))
                        )

                    entity.Body <-
                        ResourceEntityMapper.serializeKeyValuePairs (
                            resourceData.Body |> List.map (fun kvp -> (kvp.Key, kvp.Value))
                        )

                    entity.UpdatedAt <- resourceData.UpdatedAt

                    let! _ = context.SaveChangesAsync()
                    return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to update resource: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.DeleteAsync(resourceId: ResourceId) : Task<Result<unit, DomainError>> = task {
            try
                let guidId = resourceId.Value

                let! resourceEntity =
                    context.Resources
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(fun r -> r.Id = guidId)

                match Option.ofObj resourceEntity with
                | None -> return Error(NotFound "Resource not found")
                | Some entity ->
                    // Soft delete: set IsDeleted flag instead of removing entity
                    entity.IsDeleted <- true
                    entity.UpdatedAt <- System.DateTime.UtcNow
                    let! _ = context.SaveChangesAsync()
                    return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to delete resource: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.ExistsAsync(resourceId: ResourceId) : Task<bool> = task {
            let guidId = resourceId.Value
            return! context.Resources.AnyAsync(fun r -> r.Id = guidId)
        }

        member _.ExistsByNameAsync(resourceName: ResourceName) : Task<bool> = task {
            let nameStr = resourceName.Value
            return! context.Resources.AnyAsync(fun r -> r.Name = nameStr)
        }

        member _.GetCountAsync() : Task<int> = task { return! context.Resources.CountAsync() }