namespace Freetool.Application.Handlers

open System
open System.Threading.Tasks
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
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
                    Email.Create(Some(User.getEmail validatedUser))
                    |> function
                        | Ok e -> e
                        | Error _ -> failwith "ValidatedUser should have valid email"

                let! existsByEmail = userRepository.ExistsByEmailAsync email

                if existsByEmail then
                    return Error(Conflict "A user with this email already exists")
                else
                    // Create event-aware user from the validated user
                    let eventAwareUser =
                        User.create (User.getName validatedUser) email (User.getProfilePicUrl validatedUser)

                    // Save user and events atomically
                    match! userRepository.AddAsync eventAwareUser with
                    | Error error -> return Error error
                    | Ok() -> return Ok(UserCommandResult.UserResult(mapUserToDto eventAwareUser))

            | DeleteUser userId ->
                match Guid.TryParse userId with
                | false, _ -> return Error(ValidationError "Invalid user ID format")
                | true, guid ->
                    let userIdObj = UserId.FromGuid guid
                    let! userOption = userRepository.GetByIdAsync userIdObj

                    match userOption with
                    | None -> return Error(NotFound "User not found")
                    | Some user ->
                        // Mark user for deletion to create the delete event
                        let userWithDeleteEvent = User.markForDeletion user
                        let deleteEvent = User.getUncommittedEvents userWithDeleteEvent |> List.tryHead

                        // Delete user and save event atomically
                        match! userRepository.DeleteAsync userIdObj deleteEvent with
                        | Error error -> return Error error
                        | Ok() -> return Ok(UserCommandResult.UnitResult())

            | UpdateUserName(userId, dto) ->
                match Guid.TryParse userId with
                | false, _ -> return Error(ValidationError "Invalid user ID format")
                | true, guid ->
                    let userIdObj = UserId.FromGuid guid
                    let! userOption = userRepository.GetByIdAsync userIdObj

                    match userOption with
                    | None -> return Error(NotFound "User not found")
                    | Some user ->
                        // Update name using domain method (automatically creates event)
                        match User.updateName (Some dto.Name) user with
                        | Error error -> return Error error
                        | Ok updatedUser ->
                            // Save user and events atomically
                            match! userRepository.UpdateAsync updatedUser with
                            | Error error -> return Error error
                            | Ok() -> return Ok(UserCommandResult.UserResult(mapUserToDto updatedUser))

            | UpdateUserEmail(userId, dto) ->
                match Guid.TryParse userId with
                | false, _ -> return Error(ValidationError "Invalid user ID format")
                | true, guid ->
                    let userIdObj = UserId.FromGuid guid
                    let! userOption = userRepository.GetByIdAsync userIdObj

                    match userOption with
                    | None -> return Error(NotFound "User not found")
                    | Some user ->
                        // Update email using domain method (automatically creates event)
                        match User.updateEmail dto.Email user with
                        | Error error -> return Error error
                        | Ok updatedUser ->
                            // Save user and events atomically
                            match! userRepository.UpdateAsync updatedUser with
                            | Error error -> return Error error
                            | Ok() -> return Ok(UserCommandResult.UserResult(mapUserToDto updatedUser))

            | SetProfilePicture(userId, dto) ->
                match Guid.TryParse userId with
                | false, _ -> return Error(ValidationError "Invalid user ID format")
                | true, guid ->
                    let userIdObj = UserId.FromGuid guid
                    let! userOption = userRepository.GetByIdAsync userIdObj

                    match userOption with
                    | None -> return Error(NotFound "User not found")
                    | Some user ->
                        // Update profile picture using domain method (automatically creates event)
                        match User.updateProfilePic (Some dto.ProfilePicUrl) user with
                        | Error error -> return Error error
                        | Ok updatedUser ->
                            // Save user and events atomically
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
                        // Remove profile picture using domain method (automatically creates event)
                        let updatedUser = User.removeProfilePicture user

                        // Save user and events atomically
                        match! userRepository.UpdateAsync updatedUser with
                        | Error error -> return Error error
                        | Ok() -> return Ok(UserCommandResult.UserResult(mapUserToDto updatedUser))

            | GetUserById userId ->
                match Guid.TryParse userId with
                | false, _ -> return Error(ValidationError "Invalid user ID format")
                | true, guid ->
                    let userIdObj = UserId.FromGuid guid
                    let! userOption = userRepository.GetByIdAsync userIdObj

                    match userOption with
                    | None -> return Error(NotFound "User not found")
                    | Some user -> return Ok(UserCommandResult.UserResult(mapUserToDto user))

            | GetUserByEmail email ->
                match Email.Create(Some email) with
                | Error error -> return Error error
                | Ok emailObj ->
                    let! userOption = userRepository.GetByEmailAsync emailObj

                    match userOption with
                    | None -> return Error(NotFound "User not found")
                    | Some user -> return Ok(UserCommandResult.UserResult(mapUserToDto user))

            | GetAllUsers(skip, take) ->
                if skip < 0 then
                    return Error(ValidationError "Skip cannot be negative")
                elif take <= 0 || take > 100 then
                    return Error(ValidationError "Take must be between 1 and 100")
                else
                    let! users = userRepository.GetAllAsync skip take
                    let! totalCount = userRepository.GetCountAsync()
                    let result = UserMapper.toPagedDto users totalCount skip take

                    return Ok(UserCommandResult.UsersResult result)
        }

type UserHandler() =
    interface ICommandHandler with
        member this.HandleCommand
            (repository: IUserRepository)
            (command: UserCommand)
            : Task<Result<UserCommandResult, DomainError>> =
            UserHandler.handleCommand repository command