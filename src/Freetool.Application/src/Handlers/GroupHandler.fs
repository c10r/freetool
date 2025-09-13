namespace Freetool.Application.Handlers

open System
open System.Threading.Tasks
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Application.Commands
open Freetool.Application.DTOs

module GroupHandler =

    let handleCommand
        (groupRepository: IGroupRepository)
        (userRepository: IUserRepository)
        (command: GroupCommand)
        : Task<Result<GroupCommandResult, DomainError>> =
        task {
            match command with
            | CreateGroup(actorUserId, validatedGroup) ->
                // Check if group name already exists
                let! existsByName = groupRepository.ExistsByNameAsync(Group.getName validatedGroup)

                if existsByName then
                    return Error(Conflict "A group with this name already exists")
                else
                    // Validate UserIds if any are provided
                    let userIds = Group.getUserIds validatedGroup

                    // Validate UserIds if any are provided
                    let! validationResults =
                        userIds
                        |> List.map (fun userId ->
                            task {
                                let! exists = userRepository.ExistsAsync userId
                                return (userId, exists)
                            })
                        |> Task.WhenAll

                    let invalidUserIds =
                        validationResults
                        |> Array.filter (fun (_, exists) -> not exists)
                        |> Array.map fst
                        |> Array.toList

                    match invalidUserIds with
                    | [] ->
                        let validatedUserIds = userIds |> List.distinct
                        // Create event-aware group from the validated group with validated user IDs
                        match Group.create actorUserId (Group.getName validatedGroup) (Some validatedUserIds) with
                        | Error error -> return Error error
                        | Ok eventAwareGroup ->
                            // Save group and events atomically
                            match! groupRepository.AddAsync eventAwareGroup with
                            | Error error -> return Error error
                            | Ok() -> return Ok(GroupResult(eventAwareGroup.State))
                    | invalidIds ->
                        let invalidIdStrings = invalidIds |> List.map (fun id -> id.Value.ToString())

                        let message =
                            sprintf
                                "The following user IDs do not exist or are deleted: %s"
                                (String.concat ", " invalidIdStrings)

                        return Error(ValidationError message)

            | DeleteGroup(actorUserId, groupId) ->
                match Guid.TryParse groupId with
                | false, _ -> return Error(ValidationError "Invalid group ID format")
                | true, guid ->
                    let groupIdObj = GroupId.FromGuid guid
                    let! groupOption = groupRepository.GetByIdAsync groupIdObj

                    match groupOption with
                    | None -> return Error(NotFound "Group not found")
                    | Some group ->
                        // Mark group for deletion to create the delete event
                        let groupWithDeleteEvent = Group.markForDeletion actorUserId group

                        // Delete group and save event atomically
                        match! groupRepository.DeleteAsync groupWithDeleteEvent with
                        | Error error -> return Error error
                        | Ok() -> return Ok(UnitResult())

            | UpdateGroupName(actorUserId, groupId, dto) ->
                match Guid.TryParse groupId with
                | false, _ -> return Error(ValidationError "Invalid group ID format")
                | true, guid ->
                    let groupIdObj = GroupId.FromGuid guid
                    let! groupOption = groupRepository.GetByIdAsync groupIdObj

                    match groupOption with
                    | None -> return Error(NotFound "Group not found")
                    | Some group ->
                        // Check if new name already exists (but allow same name for idempotency)
                        if Group.getName group <> dto.Name then
                            let! existsByName = groupRepository.ExistsByNameAsync dto.Name

                            if existsByName then
                                return Error(Conflict "A group with this name already exists")
                            else
                                // Update name using domain method (automatically creates event)
                                match Group.updateName actorUserId dto.Name group with
                                | Error error -> return Error error
                                | Ok updatedGroup ->
                                    // Save group and events atomically
                                    match! groupRepository.UpdateAsync updatedGroup with
                                    | Error error -> return Error error
                                    | Ok() -> return Ok(GroupResult(updatedGroup.State))
                        else
                            return Ok(GroupResult(group.State))

            | AddUserToGroup(actorUserId, groupId, dto) ->
                match Guid.TryParse groupId, Guid.TryParse dto.UserId with
                | (false, _), _ -> return Error(ValidationError "Invalid group ID format")
                | _, (false, _) -> return Error(ValidationError "Invalid user ID format")
                | (true, groupGuid), (true, userGuid) ->
                    let groupIdObj = GroupId.FromGuid groupGuid
                    let userIdObj = UserId.FromGuid userGuid

                    // Check if group exists
                    let! groupOption = groupRepository.GetByIdAsync groupIdObj

                    match groupOption with
                    | None -> return Error(NotFound "Group not found")
                    | Some group ->
                        // Check if user exists
                        let! userExists = userRepository.ExistsAsync userIdObj

                        if not userExists then
                            return Error(NotFound "User not found")
                        else
                            // Add user using domain method (automatically creates event)
                            match Group.addUser actorUserId userIdObj group with
                            | Error error -> return Error error
                            | Ok updatedGroup ->
                                // Save group and events atomically
                                match! groupRepository.UpdateAsync updatedGroup with
                                | Error error -> return Error error
                                | Ok() -> return Ok(GroupResult(updatedGroup.State))

            | RemoveUserFromGroup(actorUserId, groupId, dto) ->
                match Guid.TryParse groupId, Guid.TryParse dto.UserId with
                | (false, _), _ -> return Error(ValidationError "Invalid group ID format")
                | _, (false, _) -> return Error(ValidationError "Invalid user ID format")
                | (true, groupGuid), (true, userGuid) ->
                    let groupIdObj = GroupId.FromGuid groupGuid
                    let userIdObj = UserId.FromGuid userGuid

                    let! groupOption = groupRepository.GetByIdAsync groupIdObj

                    match groupOption with
                    | None -> return Error(NotFound "Group not found")
                    | Some group ->
                        // Remove user using domain method (automatically creates event)
                        match Group.removeUser actorUserId userIdObj group with
                        | Error error -> return Error error
                        | Ok updatedGroup ->
                            // Save group and events atomically
                            match! groupRepository.UpdateAsync updatedGroup with
                            | Error error -> return Error error
                            | Ok() -> return Ok(GroupResult(updatedGroup.State))

            | GetGroupById groupId ->
                match Guid.TryParse groupId with
                | false, _ -> return Error(ValidationError "Invalid group ID format")
                | true, guid ->
                    let groupIdObj = GroupId.FromGuid guid
                    let! groupOption = groupRepository.GetByIdAsync groupIdObj

                    match groupOption with
                    | None -> return Error(NotFound "Group not found")
                    | Some group -> return Ok(GroupResult(group.State))

            | GetGroupByName name ->
                let! groupOption = groupRepository.GetByNameAsync name

                match groupOption with
                | None -> return Error(NotFound "Group not found")
                | Some group -> return Ok(GroupResult(group.State))

            | GetGroupsByUserId userId ->
                match Guid.TryParse userId with
                | false, _ -> return Error(ValidationError "Invalid user ID format")
                | true, guid ->
                    let userIdObj = UserId.FromGuid guid

                    // Check if user exists
                    let! userExists = userRepository.ExistsAsync userIdObj

                    if not userExists then
                        return Error(NotFound "User not found")
                    else
                        let! groups = groupRepository.GetByUserIdAsync userIdObj
                        let numGroups = List.length groups

                        let result =
                            { Items = groups |> List.map (fun group -> group.State)
                              TotalCount = numGroups
                              Skip = 0
                              Take = numGroups }

                        return Ok(GroupsResult result)

            | GetAllGroups(skip, take) ->
                if skip < 0 then
                    return Error(ValidationError "Skip cannot be negative")
                elif take <= 0 || take > 100 then
                    return Error(ValidationError "Take must be between 1 and 100")
                else
                    let! groups = groupRepository.GetAllAsync skip take
                    let! totalCount = groupRepository.GetCountAsync()

                    let result =
                        { Items = groups |> List.map (fun group -> group.State)
                          TotalCount = totalCount
                          Skip = skip
                          Take = take }

                    return Ok(GroupsResult result)
        }

type GroupHandler(groupRepository: IGroupRepository, userRepository: IUserRepository) =
    interface IMultiRepositoryCommandHandler<GroupCommand, GroupCommandResult> with
        member this.HandleCommand command =
            GroupHandler.handleCommand groupRepository userRepository command
