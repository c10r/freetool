namespace Freetool.Domain.Entities

open System
open Freetool.Domain
open Freetool.Domain.ValueObjects

type User = {
    Id: UserId
    Name: string
    Email: Email
    ProfilePicUrl: Url option
    CreatedAt: DateTime
    UpdatedAt: DateTime
} with

    interface IEntity<UserId> with
        member this.Id = this.Id

module User =
    let create (name: string) (email: Email) (profilePicUrl: Url option) : Result<User, DomainError> =
        if String.IsNullOrWhiteSpace name then
            Error(ValidationError "User name cannot be empty")
        elif name.Length > 100 then
            Error(ValidationError "User name cannot exceed 100 characters")
        else
            let now = DateTime.UtcNow

            Ok {
                Id = UserId.NewId()
                Name = name.Trim()
                Email = email
                ProfilePicUrl = profilePicUrl
                CreatedAt = now
                UpdatedAt = now
            }

    let updateName (newName: string) (user: User) : Result<User, DomainError> =
        if String.IsNullOrWhiteSpace newName then
            Error(ValidationError "User name cannot be empty")
        elif newName.Length > 100 then
            Error(ValidationError "User name cannot exceed 100 characters")
        else
            Ok {
                user with
                    Name = newName.Trim()
                    UpdatedAt = DateTime.UtcNow
            }

    let updateEmail (newEmail: Email) (user: User) : User = {
        user with
            Email = newEmail
            UpdatedAt = DateTime.UtcNow
    }

    let updateProfilePic (newProfilePicUrl: Url option) (user: User) : User = {
        user with
            ProfilePicUrl = newProfilePicUrl
            UpdatedAt = DateTime.UtcNow
    }