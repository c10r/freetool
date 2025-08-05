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
            let guidId = groupId.Value
            let! groupData = context.Groups.FirstOrDefaultAsync(fun g -> g.Id.Value = guidId)

            match Option.ofObj groupData with
            | None -> return None
            | Some data ->
                // Get associated user-group relationships
                let! userGroupEntities = context.UserGroups.Where(fun ug -> ug.GroupId = guidId).ToListAsync()

                // Convert UserGroup entities to UserIds
                let userIds =
                    userGroupEntities
                    |> Seq.map (fun ug -> Freetool.Domain.ValueObjects.UserId(ug.UserId))
                    |> Seq.toList

                // Create GroupData with UserIds from relationships
                let groupDataWithUsers = { data with UserIds = userIds }

                return Some(Group.fromData groupDataWithUsers)
        }

        member _.GetByNameAsync(name: string) : Task<ValidatedGroup option> = task {
            let! groupData = context.Groups.FirstOrDefaultAsync(fun g -> g.Name = name)

            match Option.ofObj groupData with
            | None -> return None
            | Some data ->
                // Get associated user-group relationships
                let! userGroupEntities = context.UserGroups.Where(fun ug -> ug.GroupId = data.Id.Value).ToListAsync()

                // Convert UserGroup entities to UserIds
                let userIds =
                    userGroupEntities
                    |> Seq.map (fun ug -> Freetool.Domain.ValueObjects.UserId(ug.UserId))
                    |> Seq.toList

                // Create GroupData with UserIds from relationships
                let groupDataWithUsers = { data with UserIds = userIds }

                return Some(Group.fromData groupDataWithUsers)
        }

        member _.GetAllAsync (skip: int) (take: int) : Task<ValidatedGroup list> = task {
            let! groupDatas = context.Groups.OrderBy(fun g -> g.CreatedAt).Skip(skip).Take(take).ToListAsync()

            let groups = []
            let mutable groupList = groups

            for data in groupDatas do
                let! userGroupEntities = context.UserGroups.Where(fun ug -> ug.GroupId = data.Id.Value).ToListAsync()

                // Convert UserGroup entities to UserIds
                let userIds =
                    userGroupEntities
                    |> Seq.map (fun ug -> Freetool.Domain.ValueObjects.UserId(ug.UserId))
                    |> Seq.toList

                // Create GroupData with UserIds from relationships
                let groupDataWithUsers = { data with UserIds = userIds }

                let group = Group.fromData groupDataWithUsers
                groupList <- group :: groupList

            return List.rev groupList
        }

        member _.GetByUserIdAsync(userId: UserId) : Task<ValidatedGroup list> = task {
            let guidUserId = userId.Value

            let! userGroupEntities = context.UserGroups.Where(fun ug -> ug.UserId = guidUserId).ToListAsync()

            let groups = []
            let mutable groupList = groups

            for userGroup in userGroupEntities do
                let! groupData = context.Groups.FirstOrDefaultAsync(fun g -> g.Id.Value = userGroup.GroupId)

                match Option.ofObj groupData with
                | Some data ->
                    let! allUserGroupsForGroup =
                        context.UserGroups.Where(fun ug -> ug.GroupId = data.Id.Value).ToListAsync()

                    // Convert UserGroup entities to UserIds
                    let userIds =
                        allUserGroupsForGroup
                        |> Seq.map (fun ug -> Freetool.Domain.ValueObjects.UserId(ug.UserId))
                        |> Seq.toList

                    // Create GroupData with UserIds from relationships
                    let groupDataWithUsers = { data with UserIds = userIds }

                    let group = Group.fromData groupDataWithUsers

                    groupList <- group :: groupList
                | None -> ()

            return List.rev groupList
        }

        member _.AddAsync(group: ValidatedGroup) : Task<Result<unit, DomainError>> = task {
            use transaction = context.Database.BeginTransaction()

            try
                // 1. Save group to database
                context.Groups.Add group.State |> ignore
                let! _ = context.SaveChangesAsync()

                // 2. Save user-group relationships
                let userGroupEntities =
                    group.State.UserIds
                    |> List.map (fun userId -> {
                        Id = System.Guid.NewGuid()
                        UserId = userId.Value
                        GroupId = group.State.Id.Value
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
                transaction.Commit()
                return Ok()

            with
            | :? DbUpdateException as ex ->
                transaction.Rollback()
                return Error(Conflict $"Failed to add group: {ex.Message}")
            | ex ->
                transaction.Rollback()
                return Error(InvalidOperation $"Transaction failed: {ex.Message}")
        }

        member _.UpdateAsync(group: ValidatedGroup) : Task<Result<unit, DomainError>> = task {
            use transaction = context.Database.BeginTransaction()

            try
                let guidId = (Group.getId group).Value
                let! existingData = context.Groups.FirstOrDefaultAsync(fun g -> g.Id.Value = guidId)

                match Option.ofObj existingData with
                | None ->
                    transaction.Rollback()
                    return Error(NotFound "Group not found")
                | Some _ ->
                    // Update the entity directly
                    context.Groups.Update(group.State) |> ignore
                    let! _ = context.SaveChangesAsync()

                    // Update user-group relationships
                    // First, remove existing relationships
                    let! existingUserGroups = context.UserGroups.Where(fun ug -> ug.GroupId = guidId).ToListAsync()

                    context.UserGroups.RemoveRange(existingUserGroups)

                    // Add new relationships
                    let userGroupEntities =
                        group.State.UserIds
                        |> List.map (fun userId -> {
                            Id = System.Guid.NewGuid()
                            UserId = userId.Value
                            GroupId = group.State.Id.Value
                            CreatedAt = System.DateTime.UtcNow
                        })

                    for userGroupEntity in userGroupEntities do
                        context.UserGroups.Add userGroupEntity |> ignore

                    let! _ = context.SaveChangesAsync()

                    // Save events to audit log in SAME transaction
                    let events = Group.getUncommittedEvents group

                    for event in events do
                        do! eventRepository.SaveEventAsync event

                    transaction.Commit()
                    return Ok()
            with
            | :? DbUpdateException as ex ->
                transaction.Rollback()
                return Error(Conflict $"Failed to update group: {ex.Message}")
            | ex ->
                transaction.Rollback()
                return Error(InvalidOperation $"Update transaction failed: {ex.Message}")
        }

        member _.DeleteAsync (groupId: GroupId) (deleteEvent: IDomainEvent option) : Task<Result<unit, DomainError>> = task {
            use transaction = context.Database.BeginTransaction()

            try
                let guidId = groupId.Value

                let! groupData =
                    context.Groups
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(fun g -> g.Id.Value = guidId)

                match Option.ofObj groupData with
                | None ->
                    transaction.Rollback()
                    return Error(NotFound "Group not found")
                | Some data ->
                    // Remove all user-group relationships first
                    let! userGroupEntities = context.UserGroups.Where(fun ug -> ug.GroupId = guidId).ToListAsync()

                    context.UserGroups.RemoveRange(userGroupEntities)

                    // Soft delete: create updated record with IsDeleted flag
                    let updatedData = {
                        data with
                            IsDeleted = true
                            UpdatedAt = System.DateTime.UtcNow
                    }

                    context.Groups.Update(updatedData) |> ignore
                    let! _ = context.SaveChangesAsync()

                    // Save delete event if provided
                    match deleteEvent with
                    | Some event -> do! eventRepository.SaveEventAsync event
                    | None -> ()

                    transaction.Commit()
                    return Ok()
            with
            | :? DbUpdateException as ex ->
                transaction.Rollback()
                return Error(Conflict $"Failed to delete group: {ex.Message}")
            | ex ->
                transaction.Rollback()
                return Error(InvalidOperation $"Delete transaction failed: {ex.Message}")
        }

        member _.ExistsAsync(groupId: GroupId) : Task<bool> = task {
            let guidId = groupId.Value
            return! context.Groups.AnyAsync(fun g -> g.Id.Value = guidId)
        }

        member _.ExistsByNameAsync(name: string) : Task<bool> = task {
            return! context.Groups.AnyAsync(fun g -> g.Name = name)
        }

        member _.GetCountAsync() : Task<int> = task { return! context.Groups.CountAsync() }