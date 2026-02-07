namespace Freetool.Api.Services

open System
open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Application.Interfaces
open Freetool.Api

type IdentityProvisioningContext =
    { Email: string
      Name: string option
      ProfilePicUrl: string option
      GroupKeys: string list
      Source: string }

type IdentityProvisioningError =
    | InvalidEmailFormat of string
    | CreateUserFailed of string
    | ActivateUserFailed of string
    | SaveActivatedUserFailed of string
    | ProvisioningFailed of string

module IdentityProvisioningError =
    let toMessage (error: IdentityProvisioningError) =
        match error with
        | InvalidEmailFormat message -> message
        | CreateUserFailed message -> message
        | ActivateUserFailed message -> message
        | SaveActivatedUserFailed message -> message
        | ProvisioningFailed message -> message

type IIdentityProvisioningService =
    abstract member EnsureUserAsync: IdentityProvisioningContext -> Task<Result<UserId, IdentityProvisioningError>>

type IdentityProvisioningService
    (
        userRepository: IUserRepository,
        authService: IAuthorizationService,
        mappingRepository: IIdentityGroupSpaceMappingRepository,
        configuration: IConfiguration,
        logger: ILogger<IdentityProvisioningService>
    ) =

    let domainErrorToMessage (err: DomainError) =
        match err with
        | ValidationError msg -> $"Validation error: {msg}"
        | NotFound msg -> $"Not found: {msg}"
        | Conflict msg -> $"Conflict: {msg}"
        | InvalidOperation msg -> $"Invalid operation: {msg}"

    let ensureOrgAdminIfConfigured (email: string) (userId: UserId) =
        task {
            let orgAdminEmail = configuration[ConfigurationKeys.OpenFGA.OrgAdminEmail]

            if
                not (String.IsNullOrEmpty(orgAdminEmail))
                && email.Equals(orgAdminEmail, StringComparison.OrdinalIgnoreCase)
            then
                try
                    do! authService.InitializeOrganizationAsync "default" (userId.Value.ToString())
                with ex ->
                    logger.LogWarning("Failed to ensure org admin for {Email}: {Error}", email, ex.Message)
        }

    let ensureMappedSpaceMemberships (userId: UserId) (groupKeys: string list) =
        task {
            let! spaceIds = mappingRepository.GetSpaceIdsByGroupKeysAsync groupKeys

            for spaceId in spaceIds do
                try
                    do!
                        authService.CreateRelationshipsAsync(
                            [ { Subject = User(userId.Value.ToString())
                                Relation = SpaceMember
                                Object = SpaceObject(spaceId.Value.ToString()) } ]
                        )
                with ex ->
                    logger.LogWarning(
                        "Failed to ensure mapped membership for user {UserId} in space {SpaceId}: {Error}",
                        userId.Value,
                        spaceId.Value,
                        ex.Message
                    )
        }

    interface IIdentityProvisioningService with
        member _.EnsureUserAsync
            (context: IdentityProvisioningContext)
            : Task<Result<UserId, IdentityProvisioningError>> =
            task {
                match Email.Create(Some context.Email) with
                | Error err -> return Error(InvalidEmailFormat(domainErrorToMessage err))
                | Ok validEmail ->
                    let! existingUser = userRepository.GetByEmailAsync validEmail

                    match existingUser with
                    | None ->
                        let userName = context.Name |> Option.defaultValue context.Email
                        let newUser = User.create userName validEmail context.ProfilePicUrl

                        match! userRepository.AddAsync newUser with
                        | Error err -> return Error(CreateUserFailed(domainErrorToMessage err))
                        | Ok() ->
                            do! ensureOrgAdminIfConfigured context.Email newUser.State.Id
                            do! ensureMappedSpaceMemberships newUser.State.Id context.GroupKeys
                            return Ok newUser.State.Id

                    | Some user when User.isInvitedPlaceholder user ->
                        let userName = context.Name |> Option.defaultValue context.Email

                        match User.activate userName context.ProfilePicUrl user with
                        | Error err -> return Error(ActivateUserFailed(domainErrorToMessage err))
                        | Ok activatedUser ->
                            match! userRepository.UpdateAsync activatedUser with
                            | Error err -> return Error(SaveActivatedUserFailed(domainErrorToMessage err))
                            | Ok() ->
                                do! ensureOrgAdminIfConfigured context.Email activatedUser.State.Id
                                do! ensureMappedSpaceMemberships activatedUser.State.Id context.GroupKeys
                                return Ok activatedUser.State.Id

                    | Some user ->
                        do! ensureOrgAdminIfConfigured context.Email user.State.Id
                        do! ensureMappedSpaceMemberships user.State.Id context.GroupKeys
                        return Ok user.State.Id
            }
