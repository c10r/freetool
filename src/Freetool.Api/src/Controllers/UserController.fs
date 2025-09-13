namespace Freetool.Api.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Application.DTOs
open Freetool.Application.Commands
open Freetool.Application.Interfaces

[<ApiController>]
[<Route("user")>]
type UserController(userRepository: IUserRepository, commandHandler: ICommandHandler) =
    inherit AuthenticatedControllerBase()

    [<HttpGet("{id}")>]
    [<ProducesResponseType(typeof<UserData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.GetUserById(id: string) : Task<IActionResult> =
        task {
            let! result = commandHandler.HandleCommand userRepository (GetUserById id)

            return
                match result with
                | Ok(UserResult userDto) -> this.Ok(userDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpGet("email/{email}")>]
    [<ProducesResponseType(typeof<UserData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.GetUserByEmail(email: string) : Task<IActionResult> =
        task {
            let! result = commandHandler.HandleCommand userRepository (GetUserByEmail email)

            return
                match result with
                | Ok(UserResult userDto) -> this.Ok(userDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpGet>]
    [<ProducesResponseType(typeof<PagedResult<UserData>>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.GetUsers([<FromQuery>] skip: int, [<FromQuery>] take: int) : Task<IActionResult> =
        task {
            let skipValue = if skip < 0 then 0 else skip

            let takeValue =
                if take <= 0 then 10
                elif take > 100 then 100
                else take

            let! result = commandHandler.HandleCommand userRepository (GetAllUsers(skipValue, takeValue))

            return
                match result with
                | Ok(UsersResult pagedUsers) -> this.Ok(pagedUsers) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPut("{id}/name")>]
    [<ProducesResponseType(typeof<UserData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateUserName(id: string, [<FromBody>] updateDto: UpdateUserNameDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
            let! result = commandHandler.HandleCommand userRepository (UpdateUserName(userId, id, updateDto))

            return
                match result with
                | Ok(UserResult userDto) -> this.Ok(userDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPut("{id}/email")>]
    [<ProducesResponseType(typeof<UserData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateUserEmail(id: string, [<FromBody>] updateDto: UpdateUserEmailDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
            let! result = commandHandler.HandleCommand userRepository (UpdateUserEmail(userId, id, updateDto))

            return
                match result with
                | Ok(UserResult userDto) -> this.Ok userDto :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError error
        }

    [<HttpPut("{id}/profile-picture")>]
    [<ProducesResponseType(typeof<UserData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.SetProfilePicture(id: string, [<FromBody>] setDto: SetProfilePictureDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            let! result = commandHandler.HandleCommand userRepository (SetProfilePicture(userId, id, setDto))

            return
                match result with
                | Ok(UserResult userDto) -> this.Ok userDto :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError error
        }

    [<HttpDelete("{id}/profile-picture")>]
    [<ProducesResponseType(typeof<UserData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.RemoveProfilePicture(id: string) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            let! result = commandHandler.HandleCommand userRepository (RemoveProfilePicture(userId, id))

            return
                match result with
                | Ok(UserResult userDto) -> this.Ok userDto :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError error
        }

    [<HttpDelete("{id}")>]
    [<ProducesResponseType(StatusCodes.Status204NoContent)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.DeleteUser(id: string) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            let! result = commandHandler.HandleCommand userRepository (DeleteUser(userId, id))

            return
                match result with
                | Ok(UnitResult _) -> this.NoContent() :> IActionResult
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
