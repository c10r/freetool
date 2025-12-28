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
[<Route("workspace")>]
type WorkspaceController
    (
        commandHandler: IMultiRepositoryCommandHandler<WorkspaceCommand, WorkspaceCommandResult>,
        authService: IAuthorizationService
    ) =
    inherit AuthenticatedControllerBase()

    /// Helper to check if a user is an organization admin
    member private _.IsOrganizationAdmin(userId: string, orgId: string) : Task<bool> =
        authService.CheckPermissionAsync userId "admin" orgId

    [<HttpPost>]
    [<ProducesResponseType(typeof<WorkspaceData>, StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.CreateWorkspace([<FromBody>] createDto: CreateWorkspaceDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
            let userIdStr = $"user:{userId.Value}"

            // Only organization admins can create workspaces
            // We use a default organization ID "acme" - in a real app this would come from config or user context
            let! isOrgAdmin = this.IsOrganizationAdmin(userIdStr, "organization:acme")

            if not isOrgAdmin then
                return
                    this.StatusCode(
                        403,
                        {| error = "Forbidden"
                           message = "Only organization administrators can create workspaces" |}
                    )
                    :> IActionResult
            else
                let unvalidatedWorkspace = WorkspaceMapper.fromCreateDto createDto

                match Workspace.validate unvalidatedWorkspace with
                | Error domainError -> return this.HandleDomainError(domainError)
                | Ok validatedWorkspace ->
                    let! result = commandHandler.HandleCommand(CreateWorkspace(userId, validatedWorkspace))

                    return
                        match result with
                        | Ok(WorkspaceResult workspaceDto) ->
                            this.CreatedAtAction(nameof this.GetWorkspaceById, {| id = workspaceDto.Id |}, workspaceDto)
                            :> IActionResult
                        | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                        | Error error -> this.HandleDomainError(error)
        }

    [<HttpGet("{id}")>]
    [<ProducesResponseType(typeof<WorkspaceData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.GetWorkspaceById(id: string) : Task<IActionResult> =
        task {
            let! result = commandHandler.HandleCommand(GetWorkspaceById id)

            return
                match result with
                | Ok(WorkspaceResult workspaceDto) -> this.Ok(workspaceDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpGet("group/{groupId}")>]
    [<ProducesResponseType(typeof<WorkspaceData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.GetWorkspaceByGroupId(groupId: string) : Task<IActionResult> =
        task {
            let! result = commandHandler.HandleCommand(GetWorkspaceByGroupId groupId)

            return
                match result with
                | Ok(WorkspaceResult workspaceDto) -> this.Ok(workspaceDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpGet>]
    [<ProducesResponseType(typeof<PagedResult<WorkspaceData>>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.GetWorkspaces([<FromQuery>] skip: int, [<FromQuery>] take: int) : Task<IActionResult> =
        task {
            let skipValue = if skip < 0 then 0 else skip

            let takeValue =
                if take <= 0 then 10
                elif take > 100 then 100
                else take

            let! result = commandHandler.HandleCommand(GetAllWorkspaces(skipValue, takeValue))

            return
                match result with
                | Ok(WorkspacesResult pagedWorkspaces) -> this.Ok(pagedWorkspaces) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpDelete("{id}")>]
    [<ProducesResponseType(StatusCodes.Status204NoContent)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.DeleteWorkspace(id: string) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
            let userIdStr = $"user:{userId.Value}"

            // Only organization admins can delete workspaces
            let! isOrgAdmin = this.IsOrganizationAdmin(userIdStr, "organization:acme")

            if not isOrgAdmin then
                return
                    this.StatusCode(
                        403,
                        {| error = "Forbidden"
                           message = "Only organization administrators can delete workspaces" |}
                    )
                    :> IActionResult
            else
                let! result = commandHandler.HandleCommand(DeleteWorkspace(userId, id))

                return
                    match result with
                    | Ok(WorkspaceCommandResult.UnitResult()) -> this.NoContent() :> IActionResult
                    | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                    | Error error -> this.HandleDomainError(error)
        }

    [<HttpPut("{id}/group")>]
    [<ProducesResponseType(typeof<WorkspaceData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateWorkspaceGroup(id: string, [<FromBody>] dto: UpdateWorkspaceGroupDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
            let userIdStr = $"user:{userId.Value}"

            // Only organization admins can change workspace group assignments
            let! isOrgAdmin = this.IsOrganizationAdmin(userIdStr, "organization:acme")

            if not isOrgAdmin then
                return
                    this.StatusCode(
                        403,
                        {| error = "Forbidden"
                           message = "Only organization administrators can change workspace group assignments" |}
                    )
                    :> IActionResult
            else
                let! result = commandHandler.HandleCommand(UpdateWorkspaceGroup(userId, id, dto))

                return
                    match result with
                    | Ok(WorkspaceResult workspaceDto) -> this.Ok(workspaceDto) :> IActionResult
                    | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                    | Error error -> this.HandleDomainError(error)
        }

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
