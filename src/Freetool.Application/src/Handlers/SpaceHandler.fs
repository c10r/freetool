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

    /// List of all 10 permission relations for space members
    let allSpacePermissions =
        [ ResourceCreate
          ResourceEdit
          ResourceDelete
          AppCreate
          AppEdit
          AppDelete
          AppRun
          FolderCreate
          FolderEdit
          FolderDelete ]

    /// Converts a Map of permission check results to SpacePermissionsDto
    let permissionsMapToDto (permissionsMap: Map<AuthRelation, bool>) : SpacePermissionsDto =
        { CreateResource = permissionsMap |> Map.tryFind ResourceCreate |> Option.defaultValue false
          EditResource = permissionsMap |> Map.tryFind ResourceEdit |> Option.defaultValue false
          DeleteResource = permissionsMap |> Map.tryFind ResourceDelete |> Option.defaultValue false
          CreateApp = permissionsMap |> Map.tryFind AppCreate |> Option.defaultValue false
          EditApp = permissionsMap |> Map.tryFind AppEdit |> Option.defaultValue false
          DeleteApp = permissionsMap |> Map.tryFind AppDelete |> Option.defaultValue false
          RunApp = permissionsMap |> Map.tryFind AppRun |> Option.defaultValue false
          CreateFolder = permissionsMap |> Map.tryFind FolderCreate |> Option.defaultValue false
          EditFolder = permissionsMap |> Map.tryFind FolderEdit |> Option.defaultValue false
          DeleteFolder = permissionsMap |> Map.tryFind FolderDelete |> Option.defaultValue false }

    /// Creates a SpacePermissionsDto with all permissions set to true (for moderators)
    let allPermissionsDto: SpacePermissionsDto =
        { CreateResource = true
          EditResource = true
          DeleteResource = true
          CreateApp = true
          EditApp = true
          DeleteApp = true
          RunApp = true
          CreateFolder = true
          EditFolder = true
          DeleteFolder = true }

    let handleCommand
        (spaceRepository: ISpaceRepository)
        (userRepository: IUserRepository)
        (authService: IAuthorizationService)
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

            | GetSpacesByUserId(userId, skip, take) ->
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

                        let totalCount = List.length allSpaces

                        // Apply pagination
                        let paginatedSpaces = allSpaces |> List.skip skip |> List.truncate take

                        let result =
                            { Items = paginatedSpaces |> List.map (fun space -> space.State)
                              TotalCount = totalCount
                              Skip = skip
                              Take = take }

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

            | GetSpaceMembersWithPermissions(spaceId, skip, take) ->
                match Guid.TryParse spaceId with
                | false, _ -> return Error(ValidationError "Invalid space ID format")
                | true, guid ->
                    let spaceIdObj = SpaceId.FromGuid guid
                    let! spaceOption = spaceRepository.GetByIdAsync spaceIdObj

                    match spaceOption with
                    | None -> return Error(NotFound "Space not found")
                    | Some space ->
                        // Collect all user IDs (moderator + members)
                        let moderatorId = space.State.ModeratorUserId
                        let memberIds = space.State.MemberIds
                        let allUserIds = moderatorId :: memberIds |> List.distinct
                        let totalCount = List.length allUserIds

                        // Apply pagination to user IDs
                        let paginatedUserIds = allUserIds |> List.skip skip |> List.truncate take

                        // Fetch user details for paginated users
                        let! userResults =
                            paginatedUserIds
                            |> List.map (fun userId -> userRepository.GetByIdAsync userId)
                            |> Task.WhenAll

                        // Create a lookup map for users
                        let userLookup =
                            userResults
                            |> Array.choose id
                            |> Array.map (fun u -> (u.State.Id, u))
                            |> Map.ofArray

                        // Build member permissions DTOs
                        let! memberPermissions =
                            paginatedUserIds
                            |> List.map (fun userId ->
                                task {
                                    let userOption = userLookup |> Map.tryFind userId
                                    let isModerator = userId = moderatorId
                                    let userIdStr = userId.Value.ToString()

                                    // For moderators, all permissions are true
                                    // For non-moderators, batch check permissions
                                    let! permissions =
                                        if isModerator then
                                            Task.FromResult allPermissionsDto
                                        else
                                            task {
                                                let! permMap =
                                                    authService.BatchCheckPermissionsAsync
                                                        (User userIdStr)
                                                        allSpacePermissions
                                                        (SpaceObject spaceId)

                                                return permissionsMapToDto permMap
                                            }

                                    // Check if user is an organization admin
                                    let! isOrgAdmin =
                                        authService.CheckPermissionAsync
                                            (User userIdStr)
                                            OrganizationAdmin
                                            (OrganizationObject "default")

                                    let userName =
                                        userOption
                                        |> Option.map (fun u -> u.State.Name)
                                        |> Option.defaultValue "Unknown User"

                                    let userEmail =
                                        userOption |> Option.map (fun u -> u.State.Email) |> Option.defaultValue ""

                                    let profilePicUrl = userOption |> Option.bind (fun u -> u.State.ProfilePicUrl)

                                    let memberDto: SpaceMemberPermissionsDto =
                                        { UserId = userIdStr
                                          UserName = userName
                                          UserEmail = userEmail
                                          ProfilePicUrl = profilePicUrl
                                          IsModerator = isModerator
                                          IsOrgAdmin = isOrgAdmin
                                          Permissions = permissions }

                                    return memberDto
                                })
                            |> Task.WhenAll

                        let pagedMembers: PagedResult<SpaceMemberPermissionsDto> =
                            { Items = memberPermissions |> Array.toList
                              TotalCount = totalCount
                              Skip = skip
                              Take = take }

                        let response: SpaceMembersPermissionsResponseDto =
                            { SpaceId = spaceId
                              SpaceName = space.State.Name
                              Members = pagedMembers }

                        return Ok(SpaceMembersPermissionsResult response)

            | UpdateUserPermissions(actorUserId, spaceId, dto) ->
                match Guid.TryParse spaceId, Guid.TryParse dto.UserId with
                | (false, _), _ -> return Error(ValidationError "Invalid space ID format")
                | _, (false, _) -> return Error(ValidationError "Invalid user ID format")
                | (true, spaceGuid), (true, userGuid) ->
                    let spaceIdObj = SpaceId.FromGuid spaceGuid
                    let targetUserId = UserId.FromGuid userGuid

                    let! spaceOption = spaceRepository.GetByIdAsync spaceIdObj

                    match spaceOption with
                    | None -> return Error(NotFound "Space not found")
                    | Some space ->
                        // Cannot modify moderator permissions
                        if targetUserId = space.State.ModeratorUserId then
                            return
                                Error(
                                    ValidationError
                                        "Cannot modify moderator permissions - moderators have all permissions by default"
                                )
                        else if
                            // Verify target user is a member of the space
                            not (List.contains targetUserId space.State.MemberIds)
                        then
                            return Error(NotFound "User is not a member of this space")
                        else
                            let targetUserIdStr = dto.UserId

                            // Get current permissions for the user
                            let! currentPermissionsMap =
                                authService.BatchCheckPermissionsAsync
                                    (User targetUserIdStr)
                                    allSpacePermissions
                                    (SpaceObject spaceId)

                            // Build the desired permissions map from DTO
                            let desiredPermissions: Map<AuthRelation, bool> =
                                Map.ofList
                                    [ (ResourceCreate, dto.Permissions.CreateResource)
                                      (ResourceEdit, dto.Permissions.EditResource)
                                      (ResourceDelete, dto.Permissions.DeleteResource)
                                      (AppCreate, dto.Permissions.CreateApp)
                                      (AppEdit, dto.Permissions.EditApp)
                                      (AppDelete, dto.Permissions.DeleteApp)
                                      (AppRun, dto.Permissions.RunApp)
                                      (FolderCreate, dto.Permissions.CreateFolder)
                                      (FolderEdit, dto.Permissions.EditFolder)
                                      (FolderDelete, dto.Permissions.DeleteFolder) ]

                            // Compute diff: what to add and what to remove
                            let tuplesToAdd =
                                desiredPermissions
                                |> Map.toList
                                |> List.filter (fun (relation, desired) ->
                                    let current =
                                        currentPermissionsMap |> Map.tryFind relation |> Option.defaultValue false

                                    desired && not current)
                                |> List.map (fun (relation, _) ->
                                    { Subject = User targetUserIdStr
                                      Relation = relation
                                      Object = SpaceObject spaceId })

                            let tuplesToRemove =
                                desiredPermissions
                                |> Map.toList
                                |> List.filter (fun (relation, desired) ->
                                    let current =
                                        currentPermissionsMap |> Map.tryFind relation |> Option.defaultValue false

                                    not desired && current)
                                |> List.map (fun (relation, _) ->
                                    { Subject = User targetUserIdStr
                                      Relation = relation
                                      Object = SpaceObject spaceId })

                            // Update relationships atomically if there are changes
                            if not (List.isEmpty tuplesToAdd) || not (List.isEmpty tuplesToRemove) then
                                do!
                                    authService.UpdateRelationshipsAsync
                                        { TuplesToAdd = tuplesToAdd
                                          TuplesToRemove = tuplesToRemove }

                            return Ok(SpaceCommandResult.UnitResult())
        }

/// SpaceHandler class that implements ICommandHandler
type SpaceHandler
    (spaceRepository: ISpaceRepository, userRepository: IUserRepository, authService: IAuthorizationService) =
    interface ICommandHandler<SpaceCommand, SpaceCommandResult> with
        member this.HandleCommand command =
            SpaceHandler.handleCommand spaceRepository userRepository authService command
