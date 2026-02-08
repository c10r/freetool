module Freetool.Infrastructure.Tests.IdentityProvisioningServiceTests

open System
open System.Threading.Tasks
open Xunit
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging.Abstractions
open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Application.DTOs
open Freetool.Application.Interfaces
open Freetool.Api.Services

type TestUserRepository(existingUsers: ValidatedUser list) =
    let mutable usersByEmail =
        existingUsers |> List.map (fun user -> user.State.Email, user) |> Map.ofList

    interface IUserRepository with
        member _.GetByIdAsync(userId: UserId) =
            let found =
                usersByEmail
                |> Map.toList
                |> List.tryPick (fun (_, user) -> if user.State.Id = userId then Some user else None)

            Task.FromResult(found)

        member _.GetByEmailAsync(email: Email) =
            Task.FromResult(usersByEmail |> Map.tryFind email.Value)

        member _.GetAllAsync _ _ = Task.FromResult([])

        member _.AddAsync(user: ValidatedUser) =
            usersByEmail <- usersByEmail.Add(user.State.Email, user)
            Task.FromResult(Ok())

        member _.UpdateAsync(user: ValidatedUser) =
            usersByEmail <- usersByEmail.Add(user.State.Email, user)
            Task.FromResult(Ok())

        member _.DeleteAsync _ = Task.FromResult(Ok())

        member _.ExistsAsync(userId: UserId) =
            Task.FromResult(usersByEmail |> Map.exists (fun _ user -> user.State.Id = userId))

        member _.ExistsByEmailAsync(email: Email) =
            Task.FromResult(usersByEmail |> Map.containsKey email.Value)

        member _.GetCountAsync() = Task.FromResult(usersByEmail.Count)

type TestAuthorizationService() =
    let mutable createdTuples: RelationshipTuple list = []
    let mutable deletedTuples: RelationshipTuple list = []

    member _.CreatedTuples = createdTuples
    member _.DeletedTuples = deletedTuples

    interface IAuthorizationService with
        member _.CreateStoreAsync _ =
            Task.FromResult({ Id = "store"; Name = "test" })

        member _.WriteAuthorizationModelAsync() =
            Task.FromResult({ AuthorizationModelId = "model" })

        member _.InitializeOrganizationAsync _ _ = Task.FromResult(())

        member _.CreateRelationshipsAsync tuples =
            createdTuples <- createdTuples @ tuples
            Task.FromResult(())

        member _.UpdateRelationshipsAsync _ = Task.FromResult(())

        member _.DeleteRelationshipsAsync tuples =
            deletedTuples <- deletedTuples @ tuples
            Task.FromResult(())

        member _.CheckPermissionAsync _ _ _ = Task.FromResult(false)
        member _.StoreExistsAsync _ = Task.FromResult(true)
        member _.BatchCheckPermissionsAsync _ _ _ = Task.FromResult(Map.empty)

type TestIdentityGroupSpaceMappingRepository(mappings: IdentityGroupSpaceMappingDto list) =
    interface IIdentityGroupSpaceMappingRepository with
        member _.GetAllAsync() = Task.FromResult(mappings)

        member _.GetSpaceIdsByGroupKeysAsync(groupKeys: string list) =
            let normalizedKeys = groupKeys |> List.map (fun key -> key.Trim()) |> Set.ofList

            let spaceIds =
                mappings
                |> List.filter (fun mapping -> mapping.IsActive && normalizedKeys.Contains(mapping.GroupKey))
                |> List.choose (fun mapping ->
                    match Guid.TryParse(mapping.SpaceId) with
                    | true, guid -> Some(SpaceId.FromGuid guid)
                    | false, _ -> None)
                |> List.distinct

            Task.FromResult(spaceIds)

        member _.AddAsync _ _ _ =
            Task.FromResult(Error(InvalidOperation "Not used"))

        member _.UpdateIsActiveAsync _ _ _ =
            Task.FromResult(Error(InvalidOperation "Not used"))

        member _.DeleteAsync _ =
            Task.FromResult(Error(InvalidOperation "Not used"))

type TestSpaceRepository(initialSpaces: ValidatedSpace list) =
    let mutable spacesById =
        initialSpaces |> List.map (fun space -> space.State.Id, space) |> Map.ofList

    member _.GetById(spaceId: SpaceId) = spacesById |> Map.tryFind spaceId

    interface ISpaceRepository with
        member _.GetByIdAsync(spaceId: SpaceId) =
            Task.FromResult(spacesById |> Map.tryFind spaceId)

        member _.GetByNameAsync(name: string) =
            let found =
                spacesById
                |> Map.toList
                |> List.tryPick (fun (_, space) -> if space.State.Name = name then Some space else None)

            Task.FromResult(found)

        member _.GetAllAsync _ _ =
            Task.FromResult(spacesById |> Map.toList |> List.map snd)

        member _.GetByUserIdAsync(userId: UserId) =
            let spaces =
                spacesById
                |> Map.toList
                |> List.map snd
                |> List.filter (fun space -> space.State.MemberIds |> List.contains userId)

            Task.FromResult(spaces)

        member _.GetByModeratorUserIdAsync(userId: UserId) =
            let spaces =
                spacesById
                |> Map.toList
                |> List.map snd
                |> List.filter (fun space -> space.State.ModeratorUserId = userId)

            Task.FromResult(spaces)

        member _.AddAsync(space: ValidatedSpace) =
            spacesById <- spacesById.Add(space.State.Id, space)
            Task.FromResult(Ok())

        member _.UpdateAsync(space: ValidatedSpace) =
            spacesById <- spacesById.Add(space.State.Id, space)
            Task.FromResult(Ok())

        member _.DeleteAsync _ = Task.FromResult(Ok())

        member _.ExistsAsync(spaceId: SpaceId) =
            Task.FromResult(spacesById |> Map.containsKey spaceId)

        member _.ExistsByNameAsync(name: string) =
            Task.FromResult(spacesById |> Map.exists (fun _ space -> space.State.Name = name))

        member _.GetCountAsync() = Task.FromResult(spacesById.Count)

let createUser (email: string) (name: string) =
    let validEmail =
        Email.Create(Some email)
        |> Result.defaultWith (fun _ -> failwith "Invalid test email")

    User.create name validEmail None

[<Fact>]
let ``EnsureUserAsync adds existing OU user as space member in repository and OpenFGA`` () : Task =
    task {
        let moderator = createUser "moderator@example.com" "Moderator"
        let user = createUser "dev2@example.com" "Dev Two"

        let engineeringSpace =
            Space.create moderator.State.Id "Engineering" moderator.State.Id None
            |> Result.defaultWith (fun error -> failwith $"Failed to create test space: {error}")

        let mapping: IdentityGroupSpaceMappingDto =
            { Id = Guid.NewGuid().ToString()
              GroupKey = "ou:/Engineering"
              SpaceId = engineeringSpace.State.Id.Value.ToString()
              SpaceName = Some "Engineering"
              IsActive = true
              CreatedAt = DateTime.UtcNow
              UpdatedAt = DateTime.UtcNow }

        let userRepository = TestUserRepository([ user ]) :> IUserRepository
        let authServiceImpl = TestAuthorizationService()
        let authService = authServiceImpl :> IAuthorizationService

        let mappingRepository =
            TestIdentityGroupSpaceMappingRepository([ mapping ]) :> IIdentityGroupSpaceMappingRepository

        let spaceRepositoryImpl = TestSpaceRepository([ engineeringSpace ])
        let spaceRepository = spaceRepositoryImpl :> ISpaceRepository

        let configuration =
            ConfigurationBuilder().AddInMemoryCollection(dict []).Build() :> IConfiguration

        let service =
            IdentityProvisioningService(
                userRepository,
                authService,
                mappingRepository,
                spaceRepository,
                configuration,
                NullLogger<IdentityProvisioningService>.Instance
            )
            :> IIdentityProvisioningService

        let! result =
            service.EnsureUserAsync(
                { Email = user.State.Email
                  Name = Some user.State.Name
                  ProfilePicUrl = user.State.ProfilePicUrl
                  GroupKeys = [ "ou:/Engineering" ]
                  Source = "test" }
            )

        match result with
        | Error error ->
            Assert.Fail($"Expected provisioning to succeed, but got: {IdentityProvisioningError.toMessage error}")
        | Ok provisionedUserId ->
            Assert.Equal(user.State.Id, provisionedUserId)

            let updatedSpace = spaceRepositoryImpl.GetById(engineeringSpace.State.Id)
            Assert.True(updatedSpace.IsSome, "Expected Engineering space to remain available")

            match updatedSpace with
            | None -> ()
            | Some space -> Assert.Contains(user.State.Id, space.State.MemberIds)

            let hasOpenFgaMemberTuple =
                authServiceImpl.CreatedTuples
                |> List.exists (fun tuple ->
                    tuple.Subject = User(user.State.Id.Value.ToString())
                    && tuple.Relation = SpaceMember
                    && tuple.Object = SpaceObject(engineeringSpace.State.Id.Value.ToString()))

            Assert.True(hasOpenFgaMemberTuple, "Expected OpenFGA member relationship to be created")
    }
