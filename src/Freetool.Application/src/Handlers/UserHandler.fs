namespace Freetool.Application.Handlers

open System
open System.Threading.Tasks
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Application.DTOs
open Freetool.Application.Commands
open Freetool.Application.Mappers

module UserHandler =

    let private mapUserToDto = UserMapper.toDto

    let handleCommand
        (userRepository: IUserRepository)
        (command: UserCommand)
        : Task<Result<UserCommandResult, DomainError>> =
        task {
            match command with
            | CreateUser validatedUser ->
                // Check if email already exists
                let email =
                    Email.Create(User.getEmail validatedUser)
                    |> function
                        | Ok e -> e
                        | Error _ -> failwith "ValidatedUser should have valid email"

                let! existsByEmail = userRepository.ExistsByEmailAsync email

                if existsByEmail then
                    return Error(Conflict "A user with this email already exists")
                else
                    match! userRepository.AddAsync validatedUser with
                    | Error error -> return Error error
                    | Ok() -> return Ok(UserResult(mapUserToDto validatedUser))

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

            | UpdateUserName(userId, dto) ->
                match Guid.TryParse userId with
                | false, _ -> return Error(ValidationError "Invalid user ID format")
                | true, guid ->
                    let userIdObj = UserId.FromGuid guid
                    let! userOption = userRepository.GetByIdAsync userIdObj

                    match userOption with
                    | None -> return Error(NotFound "User not found")
                    | Some user ->
                        let unvalidatedUser = UserMapper.fromUpdateNameDto dto user

                        match User.validate unvalidatedUser with
                        | Error error -> return Error error
                        | Ok validatedUser ->
                            match! userRepository.UpdateAsync validatedUser with
                            | Error error -> return Error error
                            | Ok() -> return Ok(UserResult(mapUserToDto validatedUser))

            | UpdateUserEmail(userId, dto) ->
                match Guid.TryParse userId with
                | false, _ -> return Error(ValidationError "Invalid user ID format")
                | true, guid ->
                    let userIdObj = UserId.FromGuid guid
                    let! userOption = userRepository.GetByIdAsync userIdObj

                    match userOption with
                    | None -> return Error(NotFound "User not found")
                    | Some user ->
                        let unvalidatedUser = UserMapper.fromUpdateEmailDto dto user

                        match User.validate unvalidatedUser with
                        | Error error -> return Error error
                        | Ok validatedUser ->
                            match! userRepository.UpdateAsync validatedUser with
                            | Error error -> return Error error
                            | Ok() -> return Ok(UserResult(mapUserToDto validatedUser))

            | SetProfilePicture(userId, dto) ->
                match Guid.TryParse userId with
                | false, _ -> return Error(ValidationError "Invalid user ID format")
                | true, guid ->
                    let userIdObj = UserId.FromGuid guid
                    let! userOption = userRepository.GetByIdAsync userIdObj

                    match userOption with
                    | None -> return Error(NotFound "User not found")
                    | Some user ->
                        let unvalidatedUser = UserMapper.fromSetProfilePictureDto dto user

                        match User.validate unvalidatedUser with
                        | Error error -> return Error error
                        | Ok validatedUser ->
                            match! userRepository.UpdateAsync validatedUser with
                            | Error error -> return Error error
                            | Ok() -> return Ok(UserResult(mapUserToDto validatedUser))

            | RemoveProfilePicture userId ->
                match Guid.TryParse userId with
                | false, _ -> return Error(ValidationError "Invalid user ID format")
                | true, guid ->
                    let userIdObj = UserId.FromGuid guid
                    let! userOption = userRepository.GetByIdAsync userIdObj

                    match userOption with
                    | None -> return Error(NotFound "User not found")
                    | Some user ->
                        let updatedUser = User.removeProfilePicture user

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
                    let! totalCount = userRepository.GetCountAsync()
                    let result = UserMapper.toPagedDto users totalCount skip take

                    return Ok(UsersResult result)
        }

type UserHandler() =
    interface ICommandHandler with
        member this.HandleCommand repository command =
            UserHandler.handleCommand repository command