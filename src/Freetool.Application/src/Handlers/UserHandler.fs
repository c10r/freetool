namespace Freetool.Application.Handlers

open System
open System.Threading.Tasks
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Application.DTOs
open Freetool.Application.Commands

module UserHandler =

    let private mapUserToDto (user: User) : UserDto = {
        Id = user.Id.Value.ToString()
        Name = user.Name
        Email = user.Email.Value
        ProfilePicUrl = Option.toObj user.ProfilePicUrl
        CreatedAt = user.CreatedAt
        UpdatedAt = user.UpdatedAt
    }

    let handleCommand
        (userRepository: IUserRepository)
        (command: UserCommand)
        : Task<Result<UserCommandResult, DomainError>> =
        task {
            match command with
            | CreateUser dto ->
                match Email.Create dto.Email with
                | Error error -> return Error error
                | Ok email ->
                    let! existsByEmail = userRepository.ExistsByEmailAsync email

                    if existsByEmail then
                        return Error(Conflict "A user with this email already exists")
                    else
                        match User.create dto.Name email (Option.ofObj dto.ProfilePicUrl) with
                        | Error error -> return Error error
                        | Ok user ->
                            match! userRepository.AddAsync user with
                            | Error error -> return Error error
                            | Ok() -> return Ok(UserResult(mapUserToDto user))

            | DeleteUser userId ->
                match Guid.TryParse userId with
                | false, _ -> return Error(ValidationError "Invalid user ID format")
                | true, guid ->
                    let userIdObj = UserId.FromGuid guid
                    let! exists = userRepository.ExistsAsync userIdObj

                    if not exists then
                        return Error(NotFound "User not found")
                    else
                        match! userRepository.DeleteAsync userIdObj with
                        | Error error -> return Error error
                        | Ok() -> return Ok(UnitResult())

            | UpdateUserName(userId, name) ->
                match Guid.TryParse userId with
                | false, _ -> return Error(ValidationError "Invalid user ID format")
                | true, guid ->
                    let userIdObj = UserId.FromGuid guid
                    let! userOption = userRepository.GetByIdAsync userIdObj

                    match userOption with
                    | None -> return Error(NotFound "User not found")
                    | Some user ->
                        match User.updateName name user with
                        | Error error -> return Error error
                        | Ok updatedUser ->
                            match! userRepository.UpdateAsync updatedUser with
                            | Error error -> return Error error
                            | Ok() -> return Ok(UserResult(mapUserToDto updatedUser))

            | UpdateUserEmail(userId, email) ->
                match Guid.TryParse userId with
                | false, _ -> return Error(ValidationError "Invalid user ID format")
                | true, guid ->
                    match Email.Create email with
                    | Error error -> return Error error
                    | Ok emailObj ->
                        let userIdObj = UserId.FromGuid guid
                        let! userOption = userRepository.GetByIdAsync userIdObj

                        match userOption with
                        | None -> return Error(NotFound "User not found")
                        | Some user ->
                            let updatedUser = User.updateEmail emailObj user

                            match! userRepository.UpdateAsync updatedUser with
                            | Error error -> return Error error
                            | Ok() -> return Ok(UserResult(mapUserToDto updatedUser))

            | SetProfilePicture(userId, profilePicUrl) ->
                match Guid.TryParse userId with
                | false, _ -> return Error(ValidationError "Invalid user ID format")
                | true, guid ->
                    let userIdObj = UserId.FromGuid guid
                    let! userOption = userRepository.GetByIdAsync userIdObj

                    match userOption with
                    | None -> return Error(NotFound "User not found")
                    | Some user ->
                        let updatedUser = User.updateProfilePic (Option.ofObj profilePicUrl) user

                        match! userRepository.UpdateAsync updatedUser with
                        | Error error -> return Error error
                        | Ok() -> return Ok(UserResult(mapUserToDto updatedUser))

            | RemoveProfilePicture userId ->
                match Guid.TryParse userId with
                | false, _ -> return Error(ValidationError "Invalid user ID format")
                | true, guid ->
                    let userIdObj = UserId.FromGuid guid
                    let! userOption = userRepository.GetByIdAsync userIdObj

                    match userOption with
                    | None -> return Error(NotFound "User not found")
                    | Some user ->
                        let updatedUser = User.updateProfilePic None user

                        match! userRepository.UpdateAsync updatedUser with
                        | Error error -> return Error error
                        | Ok() -> return Ok(UserResult(mapUserToDto updatedUser))

            | GetUserById userId ->
                match Guid.TryParse userId with
                | false, _ -> return Error(ValidationError "Invalid user ID format")
                | true, guid ->
                    let userIdObj = UserId.FromGuid guid
                    let! userOption = userRepository.GetByIdAsync userIdObj

                    match userOption with
                    | None -> return Error(NotFound "User not found")
                    | Some user -> return Ok(UserResult(mapUserToDto user))

            | GetUserByEmail email ->
                match Email.Create email with
                | Error error -> return Error error
                | Ok emailObj ->
                    let! userOption = userRepository.GetByEmailAsync emailObj

                    match userOption with
                    | None -> return Error(NotFound "User not found")
                    | Some user -> return Ok(UserResult(mapUserToDto user))

            | GetAllUsers(skip, take) ->
                if skip < 0 then
                    return Error(ValidationError "Skip cannot be negative")
                elif take <= 0 || take > 100 then
                    return Error(ValidationError "Take must be between 1 and 100")
                else
                    let! users = userRepository.GetAllAsync skip take
                    let userDtos = users |> List.map mapUserToDto

                    let result = {
                        Users = userDtos
                        TotalCount = userDtos.Length
                        Skip = skip
                        Take = take
                    }

                    return Ok(UsersResult result)
        }

type UserHandler() =
    interface ICommandHandler with
        member this.HandleCommand repository command =
            UserHandler.handleCommand repository command