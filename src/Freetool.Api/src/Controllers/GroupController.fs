namespace Freetool.Api.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Application.DTOs
open Freetool.Application.Commands
open Freetool.Application.Interfaces
open Freetool.Application.Mappers

[<ApiController>]
[<Route("group")>]
type GroupController
    (
        commandHandler: IMultiRepositoryCommandHandler<GroupCommand, GroupCommandResult>,
        authService: IAuthorizationService
    ) =
    inherit AuthenticatedControllerBase()

    [<HttpPost>]
    [<ProducesResponseType(typeof<GroupData>, StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.CreateGroup([<FromBody>] createDto: CreateGroupDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            // Only org admins can create groups/teams
            let! isOrgAdmin = this.CheckIsOrgAdminAsync()

            if not isOrgAdmin then
                return this.Forbidden("Only organization administrators can create groups")
            else
                let unvalidatedGroup = GroupMapper.fromCreateDto createDto

                match Group.validate unvalidatedGroup with
                | Error domainError -> return this.HandleDomainError(domainError)
                | Ok validatedGroup ->
                    let! result = commandHandler.HandleCommand(CreateGroup(userId, validatedGroup))

                    return
                        match result with
                        | Ok(GroupResult groupDto) ->
                            this.CreatedAtAction(nameof this.GetGroupById, {| id = groupDto.Id |}, groupDto)
                            :> IActionResult
                        | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                        | Error error -> this.HandleDomainError(error)
        }

    [<HttpGet("{id}")>]
    [<ProducesResponseType(typeof<GroupData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.GetGroupById(id: string) : Task<IActionResult> =
        task {
            let! result = commandHandler.HandleCommand(GetGroupById id)

            return
                match result with
                | Ok(GroupResult groupDto) -> this.Ok(groupDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpGet("name/{name}")>]
    [<ProducesResponseType(typeof<GroupData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.GetGroupByName(name: string) : Task<IActionResult> =
        task {
            let! result = commandHandler.HandleCommand(GetGroupByName name)

            return
                match result with
                | Ok(GroupResult groupDto) -> this.Ok(groupDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpGet>]
    [<ProducesResponseType(typeof<PagedResult<GroupData>>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.GetGroups([<FromQuery>] skip: int, [<FromQuery>] take: int) : Task<IActionResult> =
        task {
            let skipValue = if skip < 0 then 0 else skip

            let takeValue =
                if take <= 0 then 10
                elif take > 100 then 100
                else take

            let! result = commandHandler.HandleCommand(GetAllGroups(skipValue, takeValue))

            return
                match result with
                | Ok(GroupsResult pagedGroups) -> this.Ok(pagedGroups) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpGet("user/{userId}")>]
    [<ProducesResponseType(typeof<PagedResult<GroupData>>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.GetGroupsByUserId(userId: string) : Task<IActionResult> =
        task {
            let! isOrgAdmin = this.CheckIsOrgAdminAsync()

            let! result =
                if isOrgAdmin then
                    // Org admins can see all groups
                    commandHandler.HandleCommand(GetAllGroups(0, System.Int32.MaxValue))
                else
                    // Regular users see only groups they're members of
                    commandHandler.HandleCommand(GetGroupsByUserId userId)

            return
                match result with
                | Ok(GroupsResult pagedGroups) -> this.Ok(pagedGroups) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPut("{id}/name")>]
    [<ProducesResponseType(typeof<GroupData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateGroupName(id: string, [<FromBody>] updateDto: UpdateGroupNameDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            // Only org admins can rename groups/teams
            let! isOrgAdmin = this.CheckIsOrgAdminAsync()

            if not isOrgAdmin then
                return this.Forbidden("Only organization administrators can rename groups")
            else
                let! result = commandHandler.HandleCommand(UpdateGroupName(userId, id, updateDto))

                return
                    match result with
                    | Ok(GroupResult groupDto) -> this.Ok(groupDto) :> IActionResult
                    | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                    | Error error -> this.HandleDomainError(error)
        }

    [<HttpPost("{id}/users")>]
    [<ProducesResponseType(typeof<GroupData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.AddUserToGroup(id: string, [<FromBody>] addUserDto: AddUserToGroupDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            // Org admins or space moderators can add users to groups/spaces
            let! hasPermission = this.CheckIsOrgOrSpaceModeratorAsync(id)

            if not hasPermission then
                return
                    this.Forbidden("Only organization administrators or group administrators can add users to groups")
            else
                let! result = commandHandler.HandleCommand(AddUserToGroup(userId, id, addUserDto))

                return
                    match result with
                    | Ok(GroupResult groupDto) -> this.Ok(groupDto) :> IActionResult
                    | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                    | Error error -> this.HandleDomainError(error)
        }

    [<HttpDelete("{id}/users")>]
    [<ProducesResponseType(typeof<GroupData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.RemoveUserFromGroup
        (id: string, [<FromBody>] removeUserDto: RemoveUserFromGroupDto)
        : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            // Org admins or space moderators can remove users from groups/spaces
            let! hasPermission = this.CheckIsOrgOrSpaceModeratorAsync(id)

            if not hasPermission then
                return
                    this.Forbidden(
                        "Only organization administrators or group administrators can remove users from groups"
                    )
            else
                let! result = commandHandler.HandleCommand(RemoveUserFromGroup(userId, id, removeUserDto))

                return
                    match result with
                    | Ok(GroupResult groupDto) -> this.Ok(groupDto) :> IActionResult
                    | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                    | Error error -> this.HandleDomainError(error)
        }

    [<HttpDelete("{id}")>]
    [<ProducesResponseType(StatusCodes.Status204NoContent)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.DeleteGroup(id: string) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            // Only org admins can delete groups/teams
            let! isOrgAdmin = this.CheckIsOrgAdminAsync()

            if not isOrgAdmin then
                return this.Forbidden("Only organization administrators can delete groups")
            else
                let! result = commandHandler.HandleCommand(DeleteGroup(userId, id))

                return
                    match result with
                    | Ok(GroupCommandResult.UnitResult _) -> this.NoContent() :> IActionResult
                    | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                    | Error error -> this.HandleDomainError(error)
        }

    /// Checks if the current user is an organization administrator
    member private this.CheckIsOrgAdminAsync() : Task<bool> =
        task {
            let userId = this.CurrentUserId
            let userIdStr = userId.Value.ToString()

            return! authService.CheckPermissionAsync (User userIdStr) OrganizationAdmin (OrganizationObject "default")
        }

    /// Checks if the current user is either an org admin OR a space moderator for the specified group/space
    member private this.CheckIsOrgOrSpaceModeratorAsync(groupId: string) : Task<bool> =
        task {
            let userId = this.CurrentUserId

            let! isOrgAdmin =
                authService.CheckPermissionAsync
                    (User(userId.Value.ToString()))
                    OrganizationAdmin
                    (OrganizationObject "default")

            let! isModerator =
                authService.CheckPermissionAsync (User(userId.Value.ToString())) SpaceModerator (SpaceObject groupId)

            return isOrgAdmin || isModerator
        }

    /// Returns a 403 Forbidden response with the given message
    member private this.Forbidden(message: string) : IActionResult =
        this.StatusCode(
            403,
            {| error = "Forbidden"
               message = message |}
        )
        :> IActionResult

    member private this.HandleDomainError(error: DomainError) : IActionResult =
        match error with
        | ValidationError message ->
            this.BadRequest
                {| error = "Validation failed"
                   message = message |}
            :> IActionResult
        | NotFound message ->
            this.NotFound
                {| error = "Resource not found"
                   message = message |}
            :> IActionResult
        | Conflict message ->
            this.Conflict
                {| error = "Conflict"
                   message = message |}
            :> IActionResult
        | InvalidOperation message ->
            this.UnprocessableEntity
                {| error = "Invalid operation"
                   message = message |}
            :> IActionResult
