namespace Freetool.Api.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Freetool.Domain
open Freetool.Application.DTOs
open Freetool.Application.Commands
open Freetool.Application.Interfaces

[<ApiController>]
[<Route("user")>]
type UserController(userRepository: IUserRepository, commandHandler: ICommandHandler) =
    inherit ControllerBase()

    [<HttpPost>]
    member this.CreateUser([<FromBody>] createDto: CreateUserDto) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand userRepository (CreateUser createDto)

        return
            match result with
            | Ok(UserResult userDto) ->
                this.CreatedAtAction(nameof this.GetUserById, {| id = userDto.Id |}, userDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpGet("{id}")>]
    member this.GetUserById(id: string) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand userRepository (GetUserById id)

        return
            match result with
            | Ok(UserResult userDto) -> this.Ok(userDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpGet("email/{email}")>]
    member this.GetUserByEmail(email: string) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand userRepository (GetUserByEmail email)

        return
            match result with
            | Ok(UserResult userDto) -> this.Ok(userDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpGet>]
    member this.GetUsers([<FromQuery>] skip: int, [<FromQuery>] take: int) : Task<IActionResult> = task {
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
    member this.UpdateUserName(id: string, [<FromBody>] updateDto: UpdateUserNameDto) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand userRepository (UpdateUserName(id, updateDto.Name))

        return
            match result with
            | Ok(UserResult userDto) -> this.Ok(userDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpPut("{id}/email")>]
    member this.UpdateUserEmail(id: string, [<FromBody>] updateDto: UpdateUserEmailDto) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand userRepository (UpdateUserEmail(id, updateDto.Email))

        return
            match result with
            | Ok(UserResult userDto) -> this.Ok(userDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpPut("{id}/profile-picture")>]
    member this.SetProfilePicture(id: string, [<FromBody>] setDto: SetProfilePictureDto) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand userRepository (SetProfilePicture(id, setDto.ProfilePicUrl))

        return
            match result with
            | Ok(UserResult userDto) -> this.Ok(userDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpDelete("{id}/profile-picture")>]
    member this.RemoveProfilePicture(id: string) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand userRepository (RemoveProfilePicture id)

        return
            match result with
            | Ok(UserResult userDto) -> this.Ok(userDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpDelete("{id}")>]
    member this.DeleteUser(id: string) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand userRepository (DeleteUser id)

        return
            match result with
            | Ok(UnitResult _) -> this.NoContent() :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    member private this.HandleDomainError(error: DomainError) : IActionResult =
        match error with
        | ValidationError message ->
            this.BadRequest {|
                error = "Validation failed"
                message = message
            |}
            :> IActionResult
        | NotFound message ->
            this.NotFound {|
                error = "Resource not found"
                message = message
            |}
            :> IActionResult
        | Conflict message ->
            this.Conflict {|
                error = "Conflict"
                message = message
            |}
            :> IActionResult
        | InvalidOperation message ->
            this.UnprocessableEntity {|
                error = "Invalid operation"
                message = message
            |}
            :> IActionResult