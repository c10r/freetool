namespace Freetool.Domain.Entities

open System
open Freetool.Domain
open Freetool.Domain.ValueObjects

// Validation state markers (phantom types)
type Unvalidated = Unvalidated
type Validated = Validated

// Core user data that's shared across all states
type UserData = {
    Id: UserId
    Name: string
    Email: string
    ProfilePicUrl: string option
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

// User type parameterized by validation state
type User<'State> =
    | User of UserData

    interface IEntity<UserId> with
        member this.Id =
            let (User userData) = this
            userData.Id

// Type aliases for clarity
type UnvalidatedUser = User<Unvalidated> // From DTOs - potentially unsafe
type ValidatedUser = User<Validated> // Validated domain model and database data

module User =
    // Validate unvalidated user -> validated user
    let validate (User userData: UnvalidatedUser) : Result<ValidatedUser, DomainError> =
        if String.IsNullOrWhiteSpace(userData.Name) then
            Error(ValidationError "User name cannot be empty")
        elif userData.Name.Length > 100 then
            Error(ValidationError "User name cannot exceed 100 characters")
        else
            // Validate email format
            match Email.Create(userData.Email) with
            | Error err -> Error err
            | Ok validEmail ->
                // Validate profile pic URL if present
                match userData.ProfilePicUrl with
                | None ->
                    Ok(
                        User {
                            userData with
                                Name = userData.Name.Trim()
                        }
                    )
                | Some urlString ->
                    match Url.Create(urlString) with
                    | Error err -> Error err
                    | Ok validUrl ->
                        Ok(
                            User {
                                userData with
                                    Name = userData.Name.Trim()
                                    ProfilePicUrl = Some(validUrl.Value)
                            }
                        )

    // Business logic operations on validated users
    let updateName (newName: string) (User userData: ValidatedUser) : Result<ValidatedUser, DomainError> =
        if String.IsNullOrWhiteSpace newName then
            Error(ValidationError "User name cannot be empty")
        elif newName.Length > 100 then
            Error(ValidationError "User name cannot exceed 100 characters")
        else
            Ok(
                User {
                    userData with
                        Name = newName.Trim()
                        UpdatedAt = DateTime.UtcNow
                }
            )

    let updateEmail (newEmail: string) (User userData: ValidatedUser) : Result<ValidatedUser, DomainError> =
        match Email.Create(newEmail) with
        | Error err -> Error err
        | Ok validEmail ->
            Ok(
                User {
                    userData with
                        Email = validEmail.Value
                        UpdatedAt = DateTime.UtcNow
                }
            )

    let updateProfilePic
        (newProfilePicUrl: string option)
        (User userData: ValidatedUser)
        : Result<ValidatedUser, DomainError> =
        match newProfilePicUrl with
        | None ->
            Ok(
                User {
                    userData with
                        ProfilePicUrl = None
                        UpdatedAt = DateTime.UtcNow
                }
            )
        | Some urlString ->
            match Url.Create(urlString) with
            | Error err -> Error err
            | Ok validUrl ->
                Ok(
                    User {
                        userData with
                            ProfilePicUrl = Some(validUrl.Value)
                            UpdatedAt = DateTime.UtcNow
                    }
                )

    let removeProfilePicture (User userData: ValidatedUser) : ValidatedUser =
        User {
            userData with
                ProfilePicUrl = None
                UpdatedAt = DateTime.UtcNow
        }

    // Create a new validated user (for testing purposes)
    let create (name: string) (email: Email) (profilePicUrl: string option) : Result<ValidatedUser, DomainError> =
        let unvalidatedUser =
            User {
                Id = UserId.NewId()
                Name = name
                Email = email.Value
                ProfilePicUrl = profilePicUrl
                CreatedAt = DateTime.UtcNow
                UpdatedAt = DateTime.UtcNow
            }

        validate unvalidatedUser

    // Utility functions for accessing data from any state
    let getId (User userData: User<'State>) : UserId = userData.Id

    let getName (User userData: User<'State>) : string = userData.Name

    let getEmail (User userData: User<'State>) : string = userData.Email

    let getProfilePicUrl (User userData: User<'State>) : string option = userData.ProfilePicUrl

    let getCreatedAt (User userData: User<'State>) : DateTime = userData.CreatedAt

    let getUpdatedAt (User userData: User<'State>) : DateTime = userData.UpdatedAt