namespace Freetool.Infrastructure.Database.Repositories

open System.Linq
open System.Threading.Tasks
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Infrastructure.Database

type GroupRepository(context: FreetoolDbContext, eventRepository: IEventRepository) =

    interface IGroupRepository with

        member _.GetByIdAsync(groupId: GroupId) : Task<ValidatedGroup option> = task {
            let! groupData = context.Groups.FirstOrDefaultAsync(fun g -> g.Id = groupId)

            match Option.ofObj groupData with
            | None -> return None
            | Some data ->
                // Get associated user-group relationships
                let! userGroupEntities = context.UserGroups.Where(fun ug -> ug.GroupId = groupId).ToListAsync()

                // Convert UserGroup entities to UserIds
                let userIds = userGroupEntities |> Seq.map (fun ug -> ug.UserId) |> Seq.toList

                // Create GroupData with UserIds populated from relationships
                data._userIds <- userIds
                return Some(Group.fromData data)
        }

        member _.GetByNameAsync(name: string) : Task<ValidatedGroup option> = task {
            let! groupData = context.Groups.FirstOrDefaultAsync(fun g -> g.Name = name)

            match Option.ofObj groupData with
            | None -> return None
            | Some data ->
                // Get associated user-group relationships
                let! userGroupEntities = context.UserGroups.Where(fun ug -> ug.GroupId = data.Id).ToListAsync()

                // Convert UserGroup entities to UserIds
                let userIds = userGroupEntities |> Seq.map (fun ug -> ug.UserId) |> Seq.toList

                // Create GroupData with UserIds populated from relationships
                data._userIds <- userIds
                return Some(Group.fromData data)
        }

        member _.GetAllAsync (skip: int) (take: int) : Task<ValidatedGroup list> = task {
            let! groupDatas = context.Groups.OrderBy(fun g -> g.CreatedAt).Skip(skip).Take(take).ToListAsync()

            let groups = []
            let mutable groupList = groups

            for data in groupDatas do
                let groupId = data.Id

                // Use direct value object comparison - EF value converters should handle this
                let! userGroupEntities = context.UserGroups.Where(fun ug -> ug.GroupId = groupId).ToListAsync()

                // Convert UserGroup entities to UserIds
                let userIds = userGroupEntities |> Seq.map (fun ug -> ug.UserId) |> Seq.toList

                // Create GroupData with UserIds populated from relationships
                data._userIds <- userIds
                let group = Group.fromData data
                groupList <- group :: groupList

            return List.rev groupList
        }

        member _.GetByUserIdAsync(userId: UserId) : Task<ValidatedGroup list> = task {
            let! userGroupEntities = context.UserGroups.Where(fun ug -> ug.UserId = userId).ToListAsync()

            let groups = []
            let mutable groupList = groups

            for userGroup in userGroupEntities do
                let groupId = userGroup.GroupId
                let! groupData = context.Groups.FirstOrDefaultAsync(fun g -> g.Id = groupId)

                match Option.ofObj groupData with
                | Some data ->
                    let dataGroupId = data.Id

                    let! allUserGroupsForGroup =
                        context.UserGroups.Where(fun ug -> ug.GroupId = dataGroupId).ToListAsync()

                    // Convert UserGroup entities to UserIds
                    let userIds = allUserGroupsForGroup |> Seq.map (fun ug -> ug.UserId) |> Seq.toList

                    // Set the UserIds for this group
                    data._userIds <- userIds
                    let group = Group.fromData data

                    groupList <- group :: groupList
                | None -> ()

            return List.rev groupList
        }

        member _.AddAsync(group: ValidatedGroup) : Task<Result<unit, DomainError>> = task {
            try
                // 1. Save group to database
                context.Groups.Add group.State |> ignore

                // 2. Save user-group relationships
                let userGroupEntities =
                    group.State.UserIds
                    |> List.map (fun userId -> {
                        Id = System.Guid.NewGuid()
                        UserId = userId
                        GroupId = group.State.Id
                        CreatedAt = System.DateTime.UtcNow
                    })

                for userGroupEntity in userGroupEntities do
                    context.UserGroups.Add userGroupEntity |> ignore

                let! _ = context.SaveChangesAsync()

                // 3. Save events to audit log in SAME transaction
                let events = Group.getUncommittedEvents group

                for event in events do
                    do! eventRepository.SaveEventAsync event

                // 4. Commit everything atomically
                let! _ = context.SaveChangesAsync()
                return Ok()

            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to add group: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Transaction failed: {ex.Message}")
        }

        member _.UpdateAsync(group: ValidatedGroup) : Task<Result<unit, DomainError>> = task {
            try
                let groupId = Group.getId group
                let! existingData = context.Groups.FirstOrDefaultAsync(fun g -> g.Id = groupId)

                match Option.ofObj existingData with
                | None -> return Error(NotFound "Group not found")
                | Some _ ->
                    // Update the entity directly
                    context.Groups.Update(group.State) |> ignore
                    let! _ = context.SaveChangesAsync()

                    // Update user-group relationships
                    // First, remove existing relationships
                    let! existingUserGroups = context.UserGroups.Where(fun ug -> ug.GroupId = groupId).ToListAsync()

                    context.UserGroups.RemoveRange(existingUserGroups)

                    // Add new relationships
                    let userGroupEntities =
                        group.State.UserIds
                        |> List.map (fun userId -> {
                            Id = System.Guid.NewGuid()
                            UserId = userId
                            GroupId = group.State.Id
                            CreatedAt = System.DateTime.UtcNow
                        })

                    for userGroupEntity in userGroupEntities do
                        context.UserGroups.Add userGroupEntity |> ignore

                    // Save events to audit log
                    let events = Group.getUncommittedEvents group

                    for event in events do
                        do! eventRepository.SaveEventAsync event

                    let! _ = context.SaveChangesAsync()
                    return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to update group: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Update transaction failed: {ex.Message}")
        }

        member _.DeleteAsync(groupWithDeleteEvent: ValidatedGroup) : Task<Result<unit, DomainError>> = task {
            try
                let groupId = Group.getId groupWithDeleteEvent

                // Remove all user-group relationships first
                let! userGroupEntities = context.UserGroups.Where(fun ug -> ug.GroupId = groupId).ToListAsync()

                context.UserGroups.RemoveRange(userGroupEntities)

                // Soft delete: create updated record with IsDeleted flag
                let updatedData = {
                    groupWithDeleteEvent.State with
                        IsDeleted = true
                        UpdatedAt = System.DateTime.UtcNow
                }

                context.Groups.Update(updatedData) |> ignore

                // Save all uncommitted events
                let events = Group.getUncommittedEvents groupWithDeleteEvent

                for event in events do
                    do! eventRepository.SaveEventAsync event

                let! _ = context.SaveChangesAsync()
                return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to delete group: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Delete transaction failed: {ex.Message}")
        }

        member _.ExistsAsync(groupId: GroupId) : Task<bool> = task {
            return! context.Groups.AnyAsync(fun g -> g.Id = groupId)
        }

        member _.ExistsByNameAsync(name: string) : Task<bool> = task {
            return! context.Groups.AnyAsync(fun g -> g.Name = name)
        }

        member _.GetCountAsync() : Task<int> = task { return! context.Groups.CountAsync() }