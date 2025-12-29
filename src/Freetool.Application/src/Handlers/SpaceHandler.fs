namespace Freetool.Application.Handlers

open System
open System.Threading.Tasks
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Application.Commands
open Freetool.Application.DTOs
open Freetool.Application.Mappers

module SpaceHandler =

    let handleCommand
        (spaceRepository: ISpaceRepository)
        (userRepository: IUserRepository)
        (command: SpaceCommand)
        : Task<Result<SpaceCommandResult, DomainError>> =
        task {
            match command with
            | CreateSpace(actorUserId, dto) ->
                // Parse and validate the CreateSpaceDto
                match SpaceMapper.fromCreateDto dto with
                | Error errorMsg -> return Error(ValidationError errorMsg)
                | Ok(moderatorUserId, name, memberIds) ->
                    // Check if space name already exists
                    let! existsByName = spaceRepository.ExistsByNameAsync(name)

                    if existsByName then
                        return Error(Conflict "A space with this name already exists")
                    else
                        // Validate moderator exists
                        let! moderatorExists = userRepository.ExistsAsync moderatorUserId

                        if not moderatorExists then
                            return Error(NotFound "Moderator user not found")
                        else
                            // Validate all member IDs exist
                            let! validationResults =
                                memberIds
                                |> List.map (fun userId ->
                                    task {
                                        let! exists = userRepository.ExistsAsync userId
                                        return (userId, exists)
                                    })
                                |> Task.WhenAll

                            let invalidMemberIds =
                                validationResults
                                |> Array.filter (fun (_, exists) -> not exists)
                                |> Array.map fst
                                |> Array.toList

                            match invalidMemberIds with
                            | [] ->
                                // Create event-aware space using domain method
                                match Space.create actorUserId name moderatorUserId (Some memberIds) with
                                | Error error -> return Error error
                                | Ok eventAwareSpace ->
                                    // Save space and events atomically
                                    match! spaceRepository.AddAsync eventAwareSpace with
                                    | Error error -> return Error error
                                    | Ok() -> return Ok(SpaceResult(eventAwareSpace.State))
                            | invalidIds ->
                                let invalidIdStrings = invalidIds |> List.map (fun id -> id.Value.ToString())

                                let message =
                                    sprintf
                                        "The following member user IDs do not exist or are deleted: %s"
                                        (String.concat ", " invalidIdStrings)

                                return Error(ValidationError message)

            | DeleteSpace(actorUserId, spaceId) ->
                match Guid.TryParse spaceId with
                | false, _ -> return Error(ValidationError "Invalid space ID format")
                | true, guid ->
                    let spaceIdObj = SpaceId.FromGuid guid
                    let! spaceOption = spaceRepository.GetByIdAsync spaceIdObj

                    match spaceOption with
                    | None -> return Error(NotFound "Space not found")
                    | Some space ->
                        // Mark space for deletion to create the delete event
                        let spaceWithDeleteEvent = Space.markForDeletion actorUserId space

                        // Delete space and save event atomically
                        match! spaceRepository.DeleteAsync spaceWithDeleteEvent with
                        | Error error -> return Error error
                        | Ok() -> return Ok(SpaceCommandResult.UnitResult())

            | UpdateSpaceName(actorUserId, spaceId, dto) ->
                match Guid.TryParse spaceId with
                | false, _ -> return Error(ValidationError "Invalid space ID format")
                | true, guid ->
                    let spaceIdObj = SpaceId.FromGuid guid
                    let! spaceOption = spaceRepository.GetByIdAsync spaceIdObj

                    match spaceOption with
                    | None -> return Error(NotFound "Space not found")
                    | Some space ->
                        // Check if new name already exists (but allow same name for idempotency)
                        if Space.getName space <> dto.Name then
                            let! existsByName = spaceRepository.ExistsByNameAsync dto.Name

                            if existsByName then
                                return Error(Conflict "A space with this name already exists")
                            else
                                // Update name using domain method (automatically creates event)
                                match Space.updateName actorUserId dto.Name space with
                                | Error error -> return Error error
                                | Ok updatedSpace ->
                                    // Save space and events atomically
                                    match! spaceRepository.UpdateAsync updatedSpace with
                                    | Error error -> return Error error
                                    | Ok() -> return Ok(SpaceResult(updatedSpace.State))
                        else
                            return Ok(SpaceResult(space.State))

            | ChangeModerator(actorUserId, spaceId, dto) ->
                match Guid.TryParse spaceId, Guid.TryParse dto.NewModeratorUserId with
                | (false, _), _ -> return Error(ValidationError "Invalid space ID format")
                | _, (false, _) -> return Error(ValidationError "Invalid new moderator user ID format")
                | (true, spaceGuid), (true, moderatorGuid) ->
                    let spaceIdObj = SpaceId.FromGuid spaceGuid
                    let newModeratorUserId = UserId.FromGuid moderatorGuid

                    let! spaceOption = spaceRepository.GetByIdAsync spaceIdObj

                    match spaceOption with
                    | None -> return Error(NotFound "Space not found")
                    | Some space ->
                        // Check if new moderator exists
                        let! moderatorExists = userRepository.ExistsAsync newModeratorUserId

                        if not moderatorExists then
                            return Error(NotFound "New moderator user not found")
                        else
                            // Change moderator using domain method (automatically creates event)
                            match Space.changeModerator actorUserId newModeratorUserId space with
                            | Error error -> return Error error
                            | Ok updatedSpace ->
                                // Save space and events atomically
                                match! spaceRepository.UpdateAsync updatedSpace with
                                | Error error -> return Error error
                                | Ok() -> return Ok(SpaceResult(updatedSpace.State))

            | AddMember(actorUserId, spaceId, dto) ->
                match Guid.TryParse spaceId, Guid.TryParse dto.UserId with
                | (false, _), _ -> return Error(ValidationError "Invalid space ID format")
                | _, (false, _) -> return Error(ValidationError "Invalid user ID format")
                | (true, spaceGuid), (true, userGuid) ->
                    let spaceIdObj = SpaceId.FromGuid spaceGuid
                    let userIdObj = UserId.FromGuid userGuid

                    // Check if space exists
                    let! spaceOption = spaceRepository.GetByIdAsync spaceIdObj

                    match spaceOption with
                    | None -> return Error(NotFound "Space not found")
                    | Some space ->
                        // Check if user exists
                        let! userExists = userRepository.ExistsAsync userIdObj

                        if not userExists then
                            return Error(NotFound "User not found")
                        else
                            // Add member using domain method (automatically creates event)
                            match Space.addMember actorUserId userIdObj space with
                            | Error error -> return Error error
                            | Ok updatedSpace ->
                                // Save space and events atomically
                                match! spaceRepository.UpdateAsync updatedSpace with
                                | Error error -> return Error error
                                | Ok() -> return Ok(SpaceResult(updatedSpace.State))

            | RemoveMember(actorUserId, spaceId, dto) ->
                match Guid.TryParse spaceId, Guid.TryParse dto.UserId with
                | (false, _), _ -> return Error(ValidationError "Invalid space ID format")
                | _, (false, _) -> return Error(ValidationError "Invalid user ID format")
                | (true, spaceGuid), (true, userGuid) ->
                    let spaceIdObj = SpaceId.FromGuid spaceGuid
                    let userIdObj = UserId.FromGuid userGuid

                    let! spaceOption = spaceRepository.GetByIdAsync spaceIdObj

                    match spaceOption with
                    | None -> return Error(NotFound "Space not found")
                    | Some space ->
                        // Remove member using domain method (automatically creates event)
                        match Space.removeMember actorUserId userIdObj space with
                        | Error error -> return Error error
                        | Ok updatedSpace ->
                            // Save space and events atomically
                            match! spaceRepository.UpdateAsync updatedSpace with
                            | Error error -> return Error error
                            | Ok() -> return Ok(SpaceResult(updatedSpace.State))

            | GetSpaceById spaceId ->
                match Guid.TryParse spaceId with
                | false, _ -> return Error(ValidationError "Invalid space ID format")
                | true, guid ->
                    let spaceIdObj = SpaceId.FromGuid guid
                    let! spaceOption = spaceRepository.GetByIdAsync spaceIdObj

                    match spaceOption with
                    | None -> return Error(NotFound "Space not found")
                    | Some space -> return Ok(SpaceResult(space.State))

            | GetSpaceByName name ->
                let! spaceOption = spaceRepository.GetByNameAsync name

                match spaceOption with
                | None -> return Error(NotFound "Space not found")
                | Some space -> return Ok(SpaceResult(space.State))

            | GetSpacesByUserId userId ->
                match Guid.TryParse userId with
                | false, _ -> return Error(ValidationError "Invalid user ID format")
                | true, guid ->
                    let userIdObj = UserId.FromGuid guid

                    // Check if user exists
                    let! userExists = userRepository.ExistsAsync userIdObj

                    if not userExists then
                        return Error(NotFound "User not found")
                    else
                        // Get spaces where user is a member
                        let! memberSpaces = spaceRepository.GetByUserIdAsync userIdObj
                        // Get spaces where user is a moderator
                        let! moderatorSpaces = spaceRepository.GetByModeratorUserIdAsync userIdObj

                        // Combine and deduplicate
                        let allSpaces =
                            (memberSpaces @ moderatorSpaces)
                            |> List.distinctBy (fun space -> space.State.Id)

                        let numSpaces = List.length allSpaces

                        let result =
                            { Items = allSpaces |> List.map (fun space -> space.State)
                              TotalCount = numSpaces
                              Skip = 0
                              Take = numSpaces }

                        return Ok(SpacesResult result)

            | GetAllSpaces(skip, take) ->
                if skip < 0 then
                    return Error(ValidationError "Skip cannot be negative")
                elif take <= 0 || take > 100 then
                    return Error(ValidationError "Take must be between 1 and 100")
                else
                    let! spaces = spaceRepository.GetAllAsync skip take
                    let! totalCount = spaceRepository.GetCountAsync()

                    let result =
                        { Items = spaces |> List.map (fun space -> space.State)
                          TotalCount = totalCount
                          Skip = skip
                          Take = take }

                    return Ok(SpacesResult result)
        }

/// SpaceHandler class that implements IMultiRepositoryCommandHandler
type SpaceHandler(spaceRepository: ISpaceRepository, userRepository: IUserRepository) =
    interface IMultiRepositoryCommandHandler<SpaceCommand, SpaceCommandResult> with
        member this.HandleCommand command =
            SpaceHandler.handleCommand spaceRepository userRepository command
