namespace Freetool.Api.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Freetool.Domain
open Freetool.Application.Commands
open Freetool.Application.DTOs
open Freetool.Application.Interfaces

[<ApiController>]
[<Route("trash")>]
type TrashController(commandHandler: IMultiRepositoryCommandHandler<TrashCommand, TrashCommandResult>) =
    inherit AuthenticatedControllerBase()

    [<HttpGet("space/{spaceId}")>]
    [<ProducesResponseType(typeof<PagedResult<TrashItemDto>>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.GetTrashBySpace
        (spaceId: string, [<FromQuery>] skip: int, [<FromQuery>] take: int)
        : Task<IActionResult> =
        task {
            let skipValue = if skip < 0 then 0 else skip

            let takeValue =
                if take <= 0 then 50
                elif take > 100 then 100
                else take

            let! result = commandHandler.HandleCommand(GetTrashBySpace(spaceId, skipValue, takeValue))

            return
                match result with
                | Ok(TrashListResult trashList) -> this.Ok(trashList) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPost("app/{appId}/restore")>]
    [<ProducesResponseType(typeof<RestoreResultDto>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status409Conflict)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.RestoreApp(appId: string) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
            let! result = commandHandler.HandleCommand(RestoreApp(userId, appId))

            return
                match result with
                | Ok(RestoreAppResult restoreResult) -> this.Ok(restoreResult) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPost("folder/{folderId}/restore")>]
    [<ProducesResponseType(typeof<RestoreResultDto>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status409Conflict)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.RestoreFolder(folderId: string) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
            let! result = commandHandler.HandleCommand(RestoreFolder(userId, folderId))

            return
                match result with
                | Ok(RestoreFolderResult restoreResult) -> this.Ok(restoreResult) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPost("resource/{resourceId}/restore")>]
    [<ProducesResponseType(typeof<RestoreResultDto>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status409Conflict)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.RestoreResource(resourceId: string) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
            let! result = commandHandler.HandleCommand(RestoreResource(userId, resourceId))

            return
                match result with
                | Ok(RestoreResourceResult restoreResult) -> this.Ok(restoreResult) :> IActionResult
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
