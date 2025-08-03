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

type GroupRepository(context: FreetoolDbContext, eventRepository: IEventRepository) =

    interface IGroupRepository with

        member _.GetByIdAsync(groupId: GroupId) : Task<ValidatedGroup option> = task {
            let guidId = groupId.Value
            let! groupEntity = context.Groups.FirstOrDefaultAsync(fun g -> g.Id = guidId)

            match Option.ofObj groupEntity with
            | None -> return None
            | Some entity ->
                // Get associated user-group relationships
                let! userGroupEntities = context.UserGroups.Where(fun ug -> ug.GroupId = guidId).ToListAsync()

                return Some(GroupEntityMapper.fromEntity entity (userGroupEntities |> Seq.toList))
        }

        member _.GetByNameAsync(name: string) : Task<ValidatedGroup option> = task {
            let! groupEntity = context.Groups.FirstOrDefaultAsync(fun g -> g.Name = name)

            match Option.ofObj groupEntity with
            | None -> return None
            | Some entity ->
                // Get associated user-group relationships
                let! userGroupEntities = context.UserGroups.Where(fun ug -> ug.GroupId = entity.Id).ToListAsync()

                return Some(GroupEntityMapper.fromEntity entity (userGroupEntities |> Seq.toList))
        }

        member _.GetAllAsync (skip: int) (take: int) : Task<ValidatedGroup list> = task {
            let! groupEntities = context.Groups.OrderBy(fun g -> g.CreatedAt).Skip(skip).Take(take).ToListAsync()

            let groups = []
            let mutable groupList = groups

            for entity in groupEntities do
                let! userGroupEntities = context.UserGroups.Where(fun ug -> ug.GroupId = entity.Id).ToListAsync()

                let group = GroupEntityMapper.fromEntity entity (userGroupEntities |> Seq.toList)
                groupList <- group :: groupList

            return List.rev groupList
        }

        member _.GetByUserIdAsync(userId: UserId) : Task<ValidatedGroup list> = task {
            let guidUserId = userId.Value

            let! userGroupEntities = context.UserGroups.Where(fun ug -> ug.UserId = guidUserId).ToListAsync()

            let groups = []
            let mutable groupList = groups

            for userGroup in userGroupEntities do
                let! groupEntity = context.Groups.FirstOrDefaultAsync(fun g -> g.Id = userGroup.GroupId)

                match Option.ofObj groupEntity with
                | Some entity ->
                    let! allUserGroupsForGroup =
                        context.UserGroups.Where(fun ug -> ug.GroupId = entity.Id).ToListAsync()

                    let group =
                        GroupEntityMapper.fromEntity entity (allUserGroupsForGroup |> Seq.toList)

                    groupList <- group :: groupList
                | None -> ()

            return List.rev groupList
        }

        member _.AddAsync(group: ValidatedGroup) : Task<Result<unit, DomainError>> = task {
            use transaction = context.Database.BeginTransaction()

            try
                // 1. Save group to database
                let groupEntity = GroupEntityMapper.toEntity group
                context.Groups.Add groupEntity |> ignore
                let! _ = context.SaveChangesAsync()

                // 2. Save user-group relationships
                let userGroupEntities = GroupEntityMapper.toUserGroupEntities group

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
                let! existingEntity = context.Groups.FirstOrDefaultAsync(fun g -> g.Id = guidId)

                match Option.ofObj existingEntity with
                | None ->
                    transaction.Rollback()
                    return Error(NotFound "Group not found")
                | Some entity ->
                    // Update entity properties
                    let groupData = group.State
                    entity.Name <- groupData.Name
                    entity.UpdatedAt <- groupData.UpdatedAt

                    let! _ = context.SaveChangesAsync()

                    // Update user-group relationships
                    // First, remove existing relationships
                    let! existingUserGroups = context.UserGroups.Where(fun ug -> ug.GroupId = guidId).ToListAsync()

                    context.UserGroups.RemoveRange(existingUserGroups)

                    // Add new relationships
                    let userGroupEntities = GroupEntityMapper.toUserGroupEntities group

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
                let! groupEntity = context.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(fun g -> g.Id = guidId)

                match Option.ofObj groupEntity with
                | None ->
                    transaction.Rollback()
                    return Error(NotFound "Group not found")
                | Some entity ->
                    // Remove all user-group relationships first
                    let! userGroupEntities = context.UserGroups.Where(fun ug -> ug.GroupId = guidId).ToListAsync()

                    context.UserGroups.RemoveRange(userGroupEntities)

                    // Soft delete: set IsDeleted flag instead of removing entity
                    entity.IsDeleted <- true
                    entity.UpdatedAt <- System.DateTime.UtcNow
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
            return! context.Groups.AnyAsync(fun g -> g.Id = guidId)
        }

        member _.ExistsByNameAsync(name: string) : Task<bool> = task {
            return! context.Groups.AnyAsync(fun g -> g.Name = name)
        }

        member _.GetCountAsync() : Task<int> = task { return! context.Groups.CountAsync() }