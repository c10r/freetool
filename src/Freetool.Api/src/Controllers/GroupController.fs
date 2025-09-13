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
type GroupController(commandHandler: IMultiRepositoryCommandHandler<GroupCommand, GroupCommandResult>) =
    inherit AuthenticatedControllerBase()

    [<HttpPost>]
    [<ProducesResponseType(typeof<GroupData>, StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.CreateGroup([<FromBody>] createDto: CreateGroupDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
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
            let! result = commandHandler.HandleCommand(GetGroupsByUserId userId)

            return
                match result with
                | Ok(GroupsResult pagedGroups) -> this.Ok(pagedGroups) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPut("{id}/name")>]
    [<ProducesResponseType(typeof<GroupData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateGroupName(id: string, [<FromBody>] updateDto: UpdateGroupNameDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
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
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.AddUserToGroup(id: string, [<FromBody>] addUserDto: AddUserToGroupDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
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
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.RemoveUserFromGroup
        (id: string, [<FromBody>] removeUserDto: RemoveUserFromGroupDto)
        : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
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
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.DeleteGroup(id: string) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
            let! result = commandHandler.HandleCommand(DeleteGroup(userId, id))

            return
                match result with
                | Ok(GroupCommandResult.UnitResult _) -> this.NoContent() :> IActionResult
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
