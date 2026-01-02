namespace Freetool.Infrastructure.Database.Repositories

open System.Linq
open System.Threading.Tasks
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Infrastructure.Database

type SpaceRepository(context: FreetoolDbContext, eventRepository: IEventRepository) =

    interface ISpaceRepository with

        member _.GetByIdAsync(spaceId: SpaceId) : Task<ValidatedSpace option> =
            task {
                let! spaceData = context.Spaces.FirstOrDefaultAsync(fun s -> s.Id = spaceId)

                match Option.ofObj spaceData with
                | None -> return None
                | Some data ->
                    // Get associated space-member relationships
                    let! spaceMemberEntities = context.SpaceMembers.Where(fun sm -> sm.SpaceId = spaceId).ToListAsync()

                    // Convert SpaceMember entities to UserIds
                    let memberIds = spaceMemberEntities |> Seq.map (fun sm -> sm.UserId) |> Seq.toList

                    // Create SpaceData with MemberIds populated from relationships
                    data.MemberIds <- memberIds
                    return Some(Space.fromData data)
            }

        member _.GetByNameAsync(name: string) : Task<ValidatedSpace option> =
            task {
                let! spaceData = context.Spaces.FirstOrDefaultAsync(fun s -> s.Name = name)

                match Option.ofObj spaceData with
                | None -> return None
                | Some data ->
                    // Get associated space-member relationships
                    let! spaceMemberEntities = context.SpaceMembers.Where(fun sm -> sm.SpaceId = data.Id).ToListAsync()

                    // Convert SpaceMember entities to UserIds
                    let memberIds = spaceMemberEntities |> Seq.map (fun sm -> sm.UserId) |> Seq.toList

                    // Create SpaceData with MemberIds populated from relationships
                    data.MemberIds <- memberIds
                    return Some(Space.fromData data)
            }

        member _.GetAllAsync (skip: int) (take: int) : Task<ValidatedSpace list> =
            task {
                let! spaceDatas = context.Spaces.OrderBy(fun s -> s.CreatedAt).Skip(skip).Take(take).ToListAsync()

                if spaceDatas.Count = 0 then
                    return []
                else
                    // Batch load ALL SpaceMembers for these spaces in ONE query
                    let spaceIds = spaceDatas |> Seq.map (fun s -> s.Id) |> Seq.toList

                    let! allSpaceMembers =
                        context.SpaceMembers.Where(fun sm -> spaceIds.Contains(sm.SpaceId)).ToListAsync()

                    // Group by SpaceId for O(1) lookup
                    let membersBySpaceId =
                        allSpaceMembers
                        |> Seq.groupBy (fun sm -> sm.SpaceId)
                        |> Seq.map (fun (spaceId, members) ->
                            (spaceId, members |> Seq.map (fun sm -> sm.UserId) |> Seq.toList))
                        |> Map.ofSeq

                    return
                        spaceDatas
                        |> Seq.map (fun data ->
                            data.MemberIds <- membersBySpaceId |> Map.tryFind data.Id |> Option.defaultValue []

                            Space.fromData data)
                        |> Seq.toList
            }

        member _.GetByUserIdAsync(userId: UserId) : Task<ValidatedSpace list> =
            task {
                // Find all space IDs where the user is a member
                let! userSpaceMemberEntities = context.SpaceMembers.Where(fun sm -> sm.UserId = userId).ToListAsync()

                if userSpaceMemberEntities.Count = 0 then
                    return []
                else
                    let spaceIds =
                        userSpaceMemberEntities |> Seq.map (fun sm -> sm.SpaceId) |> Seq.toList

                    // Batch load Spaces and SpaceMembers (run in parallel)
                    let spacesTask =
                        context.Spaces.Where(fun s -> spaceIds.Contains(s.Id)).ToListAsync()

                    let allMembersTask =
                        context.SpaceMembers.Where(fun sm -> spaceIds.Contains(sm.SpaceId)).ToListAsync()

                    let! spaceDatas = spacesTask
                    let! allSpaceMembers = allMembersTask

                    // Group by SpaceId for O(1) lookup
                    let membersBySpaceId =
                        allSpaceMembers
                        |> Seq.groupBy (fun sm -> sm.SpaceId)
                        |> Seq.map (fun (spaceId, members) ->
                            (spaceId, members |> Seq.map (fun sm -> sm.UserId) |> Seq.toList))
                        |> Map.ofSeq

                    return
                        spaceDatas
                        |> Seq.map (fun data ->
                            data.MemberIds <- membersBySpaceId |> Map.tryFind data.Id |> Option.defaultValue []

                            Space.fromData data)
                        |> Seq.toList
            }

        member _.GetByModeratorUserIdAsync(moderatorUserId: UserId) : Task<ValidatedSpace list> =
            task {
                let! spaceDatas =
                    context.Spaces
                        .Where(fun s -> s.ModeratorUserId = moderatorUserId)
                        .OrderBy(fun s -> s.CreatedAt)
                        .ToListAsync()

                if spaceDatas.Count = 0 then
                    return []
                else
                    // Batch load ALL SpaceMembers for these spaces in ONE query
                    let spaceIds = spaceDatas |> Seq.map (fun s -> s.Id) |> Seq.toList

                    let! allSpaceMembers =
                        context.SpaceMembers.Where(fun sm -> spaceIds.Contains(sm.SpaceId)).ToListAsync()

                    // Group by SpaceId for O(1) lookup
                    let membersBySpaceId =
                        allSpaceMembers
                        |> Seq.groupBy (fun sm -> sm.SpaceId)
                        |> Seq.map (fun (spaceId, members) ->
                            (spaceId, members |> Seq.map (fun sm -> sm.UserId) |> Seq.toList))
                        |> Map.ofSeq

                    return
                        spaceDatas
                        |> Seq.map (fun data ->
                            data.MemberIds <- membersBySpaceId |> Map.tryFind data.Id |> Option.defaultValue []

                            Space.fromData data)
                        |> Seq.toList
            }

        member _.AddAsync(space: ValidatedSpace) : Task<Result<unit, DomainError>> =
            task {
                try
                    // 1. Save space to database
                    context.Spaces.Add space.State |> ignore

                    // 2. Save space-member relationships
                    let spaceMemberEntities =
                        space.State.MemberIds
                        |> List.map (fun userId ->
                            { Id = System.Guid.NewGuid()
                              UserId = userId
                              SpaceId = space.State.Id
                              CreatedAt = System.DateTime.UtcNow })

                    for spaceMemberEntity in spaceMemberEntities do
                        context.SpaceMembers.Add spaceMemberEntity |> ignore

                    let! _ = context.SaveChangesAsync()

                    // 3. Save events to audit log in SAME transaction
                    let events = Space.getUncommittedEvents space

                    for event in events do
                        do! eventRepository.SaveEventAsync event

                    // 4. Commit everything atomically
                    let! _ = context.SaveChangesAsync()
                    return Ok()

                with
                | :? DbUpdateException as ex -> return Error(Conflict $"Failed to add space: {ex.Message}")
                | ex -> return Error(InvalidOperation $"Transaction failed: {ex.Message}")
            }

        member _.UpdateAsync(space: ValidatedSpace) : Task<Result<unit, DomainError>> =
            task {
                try
                    let spaceId = Space.getId space
                    let! existingData = context.Spaces.FirstOrDefaultAsync(fun s -> s.Id = spaceId)

                    match Option.ofObj existingData with
                    | None -> return Error(NotFound "Space not found")
                    | Some existingEntity ->
                        // Update the already-tracked entity to avoid tracking conflicts
                        context.Entry(existingEntity).CurrentValues.SetValues(space.State)
                        let! _ = context.SaveChangesAsync()

                        // Update space-member relationships
                        // First, remove existing relationships
                        let! existingSpaceMembers =
                            context.SpaceMembers.Where(fun sm -> sm.SpaceId = spaceId).ToListAsync()

                        context.SpaceMembers.RemoveRange(existingSpaceMembers)

                        // Add new relationships
                        let spaceMemberEntities =
                            space.State.MemberIds
                            |> List.map (fun userId ->
                                { Id = System.Guid.NewGuid()
                                  UserId = userId
                                  SpaceId = space.State.Id
                                  CreatedAt = System.DateTime.UtcNow })

                        for spaceMemberEntity in spaceMemberEntities do
                            context.SpaceMembers.Add spaceMemberEntity |> ignore

                        // Save events to audit log
                        let events = Space.getUncommittedEvents space

                        for event in events do
                            do! eventRepository.SaveEventAsync event

                        let! _ = context.SaveChangesAsync()
                        return Ok()
                with
                | :? DbUpdateException as ex -> return Error(Conflict $"Failed to update space: {ex.Message}")
                | ex -> return Error(InvalidOperation $"Update transaction failed: {ex.Message}")
            }

        member _.DeleteAsync(spaceWithDeleteEvent: ValidatedSpace) : Task<Result<unit, DomainError>> =
            task {
                try
                    let spaceId = Space.getId spaceWithDeleteEvent

                    // Remove all space-member relationships first
                    let! spaceMemberEntities = context.SpaceMembers.Where(fun sm -> sm.SpaceId = spaceId).ToListAsync()

                    context.SpaceMembers.RemoveRange(spaceMemberEntities)

                    // Soft delete: create updated record with IsDeleted flag
                    let updatedData =
                        { spaceWithDeleteEvent.State with
                            IsDeleted = true
                            UpdatedAt = System.DateTime.UtcNow }

                    context.Spaces.Update(updatedData) |> ignore

                    // Save all uncommitted events
                    let events = Space.getUncommittedEvents spaceWithDeleteEvent

                    for event in events do
                        do! eventRepository.SaveEventAsync event

                    let! _ = context.SaveChangesAsync()
                    return Ok()
                with
                | :? DbUpdateException as ex -> return Error(Conflict $"Failed to delete space: {ex.Message}")
                | ex -> return Error(InvalidOperation $"Delete transaction failed: {ex.Message}")
            }

        member _.ExistsAsync(spaceId: SpaceId) : Task<bool> =
            task { return! context.Spaces.AnyAsync(fun s -> s.Id = spaceId) }

        member _.ExistsByNameAsync(name: string) : Task<bool> =
            task { return! context.Spaces.AnyAsync(fun s -> s.Name = name) }

        member _.GetCountAsync() : Task<int> =
            task { return! context.Spaces.CountAsync() }
