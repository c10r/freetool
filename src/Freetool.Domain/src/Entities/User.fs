namespace Freetool.Domain.Entities

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open System.Text.Json.Serialization
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events

[<Table("Users")>]
[<Index([| "Email" |], IsUnique = true, Name = "IX_Users_Email")>]
// CLIMutable for EntityFramework
[<CLIMutable>]
type UserData = {
    [<Key>]
    Id: UserId

    [<Required>]
    [<MaxLength(100)>]
    Name: string

    [<Required>]
    [<MaxLength(254)>]
    Email: string

    [<MaxLength(2_000)>]
    ProfilePicUrl: string option

    [<Required>]
    [<JsonIgnore>]
    CreatedAt: DateTime

    [<Required>]
    [<JsonIgnore>]
    UpdatedAt: DateTime

    [<JsonIgnore>]
    IsDeleted: bool
}

type User = EventSourcingAggregate<UserData>

module UserAggregateHelpers =
    let getEntityId (user: User) : UserId = user.State.Id

    let implementsIEntity (user: User) =
        { new IEntity<UserId> with
            member _.Id = user.State.Id
        }

// Type aliases for clarity
type UnvalidatedUser = User // From DTOs - potentially unsafe
type ValidatedUser = User // Validated domain model and database data

module User =
    let create (name: string) (email: Email) (profilePicUrl: string option) : ValidatedUser =
        let userData = {
            Id = UserId.NewId()
            Name = name.Trim()
            Email = email.Value
            ProfilePicUrl = profilePicUrl
            CreatedAt = DateTime.UtcNow
            UpdatedAt = DateTime.UtcNow
            IsDeleted = false
        }

        let userCreatedEvent =
            let profilePicUrlOption =
                profilePicUrl
                |> Option.bind (fun url -> Url.Create(Some url) |> Result.toOption)

            UserEvents.userCreated userData.Id userData.Id userData.Name email profilePicUrlOption

        {
            State = userData
            UncommittedEvents = [ userCreatedEvent :> IDomainEvent ]
        }

    let fromData (userData: UserData) : ValidatedUser = {
        State = userData
        UncommittedEvents = []
    }

    let validate (user: UnvalidatedUser) : Result<ValidatedUser, DomainError> =
        let userData = user.State

        match userData.Name with
        | "" -> Error(ValidationError "User name cannot be empty")
        | name when name.Length > 100 -> Error(ValidationError "User name cannot exceed 100 characters")
        | name ->
            // Validate email format
            match Email.Create(Some userData.Email) with
            | Error err -> Error err
            | Ok validEmail ->
                // Validate profile pic URL if present
                match userData.ProfilePicUrl with
                | None ->
                    Ok {
                        State = {
                            userData with
                                Name = name.Trim()
                                Email = validEmail.Value
                        }
                        UncommittedEvents = user.UncommittedEvents
                    }
                | Some urlString ->
                    match Url.Create(Some urlString) with
                    | Error err -> Error err
                    | Ok validUrl ->
                        Ok {
                            State = {
                                userData with
                                    Name = name.Trim()
                                    Email = validEmail.Value
                                    ProfilePicUrl = Some validUrl.Value
                            }
                            UncommittedEvents = user.UncommittedEvents
                        }

    let markForDeletion (actorUserId: UserId) (user: ValidatedUser) : ValidatedUser =
        let userDeletedEvent = UserEvents.userDeleted actorUserId user.State.Id

        {
            user with
                UncommittedEvents = user.UncommittedEvents @ [ userDeletedEvent :> IDomainEvent ]
        }

    let getUncommittedEvents (user: ValidatedUser) : IDomainEvent list = user.UncommittedEvents

    let markEventsAsCommitted (user: ValidatedUser) : ValidatedUser = { user with UncommittedEvents = [] }

    let getId (user: User) : UserId = user.State.Id

    let getName (user: User) : string = user.State.Name

    let getEmail (user: User) : string = user.State.Email

    let getProfilePicUrl (user: User) : string option = user.State.ProfilePicUrl

    let getCreatedAt (user: User) : DateTime = user.State.CreatedAt

    let getUpdatedAt (user: User) : DateTime = user.State.UpdatedAt

    let updateName
        (actorUserId: UserId)
        (newName: string option)
        (user: ValidatedUser)
        : Result<ValidatedUser, DomainError> =
        match newName with
        | None -> Error(ValidationError "User name cannot be null")
        | Some nameValue when String.IsNullOrWhiteSpace nameValue -> Error(ValidationError "User name cannot be empty")
        | Some nameValue when nameValue.Trim().Length > 100 ->
            Error(ValidationError "User name cannot exceed 100 characters")
        | Some nameValue ->
            let oldName = user.State.Name

            let updatedUserData = {
                user.State with
                    Name = nameValue.Trim()
                    UpdatedAt = DateTime.UtcNow
            }

            let nameChangedEvent =
                UserEvents.userUpdated actorUserId user.State.Id [ UserChange.NameChanged(oldName, nameValue.Trim()) ]

            Ok {
                State = updatedUserData
                UncommittedEvents = user.UncommittedEvents @ [ nameChangedEvent :> IDomainEvent ]
            }

    let updateEmail
        (actorUserId: UserId)
        (newEmail: string)
        (user: ValidatedUser)
        : Result<ValidatedUser, DomainError> =
        match Email.Create(Some newEmail) with
        | Error err -> Error err
        | Ok newEmailObj ->
            match Email.Create(Some user.State.Email) with
            | Error _ -> Error(ValidationError "Current user email is invalid")
            | Ok oldEmailObj ->
                let updatedUserData = {
                    user.State with
                        Email = newEmailObj.Value
                        UpdatedAt = DateTime.UtcNow
                }

                let emailChangedEvent =
                    UserEvents.userUpdated actorUserId user.State.Id [ EmailChanged(oldEmailObj, newEmailObj) ]

                Ok {
                    State = updatedUserData
                    UncommittedEvents = user.UncommittedEvents @ [ emailChangedEvent :> IDomainEvent ]
                }

    let updateProfilePic
        (actorUserId: UserId)
        (newProfilePicUrl: string option)
        (user: ValidatedUser)
        : Result<ValidatedUser, DomainError> =
        let oldProfilePicUrl =
            user.State.ProfilePicUrl
            |> Option.bind (fun url -> Url.Create(Some url) |> Result.toOption)

        let newProfilePicUrlObj =
            newProfilePicUrl
            |> Option.bind (fun url -> Url.Create(Some url) |> Result.toOption)

        match newProfilePicUrl with
        | None ->
            let updatedUserData = {
                user.State with
                    ProfilePicUrl = None
                    UpdatedAt = DateTime.UtcNow
            }

            let profilePicChangedEvent =
                UserEvents.userUpdated actorUserId user.State.Id [ ProfilePicChanged(oldProfilePicUrl, None) ]

            Ok {
                State = updatedUserData
                UncommittedEvents = user.UncommittedEvents @ [ profilePicChangedEvent :> IDomainEvent ]
            }
        | Some urlString ->
            match Url.Create(Some urlString) with
            | Error err -> Error err
            | Ok validUrl ->

                let updatedUserData = {
                    user.State with
                        ProfilePicUrl = Some(validUrl.Value)
                        UpdatedAt = DateTime.UtcNow
                }

                let profilePicChangedEvent =
                    UserEvents.userUpdated actorUserId user.State.Id [
                        ProfilePicChanged(oldProfilePicUrl, newProfilePicUrlObj)
                    ]

                Ok {
                    State = updatedUserData
                    UncommittedEvents = user.UncommittedEvents @ [ profilePicChangedEvent :> IDomainEvent ]
                }

    let removeProfilePicture (actorUserId: UserId) (user: ValidatedUser) : ValidatedUser =
        let oldProfilePicUrl =
            user.State.ProfilePicUrl
            |> Option.bind (fun url -> Url.Create(Some url) |> Result.toOption)

        let updatedUserData = {
            user.State with
                ProfilePicUrl = None
                UpdatedAt = DateTime.UtcNow
        }

        let profilePicChangedEvent =
            UserEvents.userUpdated actorUserId user.State.Id [ ProfilePicChanged(oldProfilePicUrl, None) ]

        {
            State = updatedUserData
            UncommittedEvents = user.UncommittedEvents @ [ profilePicChangedEvent :> IDomainEvent ]
        }