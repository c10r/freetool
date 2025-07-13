namespace Freetool.Infrastructure.Tests.Database.Mapping

open System
open Xunit
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Infrastructure.Database
open Freetool.Infrastructure.Database.Repositories

module UserEntityMapperTests =

    let createValidUser () =
        let email =
            Email.Create("test@example.com")
            |> Result.defaultWith (fun _ -> failwith "Invalid email in test setup")

        User.create "Test User" email (Some "https://example.com/pic.jpg")
        |> Result.defaultWith (fun _ -> failwith "Invalid user in test setup")

    let createUserEntity () =
        UserEntity(
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@example.com",
            ProfilePicUrl = Some "https://example.com/pic.jpg",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        )

    [<Fact>]
    let ``toEntityUser converts domain User to UserEntity correctly`` () =
        let domainUser = createValidUser ()

        let entity = UserEntityMapper.toEntityUser domainUser

        Assert.Equal(domainUser.Id.Value, entity.Id)
        Assert.Equal(domainUser.Name, entity.Name)
        Assert.Equal(domainUser.Email.Value, entity.Email)
        Assert.Equal(domainUser.ProfilePicUrl, entity.ProfilePicUrl)
        Assert.Equal(domainUser.CreatedAt, entity.CreatedAt)
        Assert.Equal(domainUser.UpdatedAt, entity.UpdatedAt)

    [<Fact>]
    let ``toEntityUser handles None ProfilePicUrl correctly`` () =
        let email =
            Email.Create("test@example.com")
            |> Result.defaultWith (fun _ -> failwith "Invalid email")

        let domainUser =
            User.create "Test User" email None
            |> Result.defaultWith (fun _ -> failwith "Invalid user")

        let entity = UserEntityMapper.toEntityUser domainUser

        Assert.Equal(None, entity.ProfilePicUrl)

    [<Fact>]
    let ``toDomainUser converts UserEntity to domain User correctly`` () =
        let entity = createUserEntity ()

        let domainUser = UserEntityMapper.toDomainUser entity

        Assert.Equal(UserId.FromGuid(entity.Id), domainUser.Id)
        Assert.Equal(entity.Name, domainUser.Name)
        Assert.Equal(entity.Email, domainUser.Email.Value)
        Assert.Equal(entity.ProfilePicUrl, domainUser.ProfilePicUrl)
        Assert.Equal(entity.CreatedAt, domainUser.CreatedAt)
        Assert.Equal(entity.UpdatedAt, domainUser.UpdatedAt)

    [<Fact>]
    let ``toDomainUser handles None ProfilePicUrl correctly`` () =
        let entity =
            UserEntity(
                Id = Guid.NewGuid(),
                Name = "Test User",
                Email = "test@example.com",
                ProfilePicUrl = None,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            )

        let domainUser = UserEntityMapper.toDomainUser entity

        Assert.Equal(None, domainUser.ProfilePicUrl)

    [<Fact>]
    let ``toDomainUser throws when entity has invalid email`` () =
        let entity =
            UserEntity(
                Id = Guid.NewGuid(),
                Name = "Test User",
                Email = "invalid-email",
                ProfilePicUrl = None,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            )

        Assert.Throws<System.Exception>(fun () -> UserEntityMapper.toDomainUser entity |> ignore)

    [<Fact>]
    let ``round trip conversion preserves data`` () =
        let originalUser = createValidUser ()

        let entity = UserEntityMapper.toEntityUser originalUser
        let convertedUser = UserEntityMapper.toDomainUser entity

        Assert.Equal(originalUser.Id, convertedUser.Id)
        Assert.Equal(originalUser.Name, convertedUser.Name)
        Assert.Equal(originalUser.Email, convertedUser.Email)
        Assert.Equal(originalUser.ProfilePicUrl, convertedUser.ProfilePicUrl)
        Assert.Equal(originalUser.CreatedAt, convertedUser.CreatedAt)
        Assert.Equal(originalUser.UpdatedAt, convertedUser.UpdatedAt)

    [<Fact>]
    let ``UserEntity can be created with default constructor`` () =
        let entity = UserEntity()

        Assert.Equal(Guid.Empty, entity.Id)
        Assert.Equal("", entity.Name)
        Assert.Equal("", entity.Email)
        Assert.Equal(None, entity.ProfilePicUrl)
        Assert.True(entity.CreatedAt <= DateTime.UtcNow)
        Assert.True(entity.UpdatedAt <= DateTime.UtcNow)

    [<Fact>]
    let ``UserEntity properties can be set and retrieved`` () =
        let entity = UserEntity()
        let id = Guid.NewGuid()
        let createdAt = DateTime.UtcNow.AddDays(-1.0)
        let updatedAt = DateTime.UtcNow

        entity.Id <- id
        entity.Name <- "John Doe"
        entity.Email <- "john@example.com"
        entity.ProfilePicUrl <- Some "https://example.com/pic.jpg"
        entity.CreatedAt <- createdAt
        entity.UpdatedAt <- updatedAt

        Assert.Equal(id, entity.Id)
        Assert.Equal("John Doe", entity.Name)
        Assert.Equal("john@example.com", entity.Email)
        Assert.Equal(Some "https://example.com/pic.jpg", entity.ProfilePicUrl)
        Assert.Equal(createdAt, entity.CreatedAt)
        Assert.Equal(updatedAt, entity.UpdatedAt)

    [<Fact>]
    let ``UserEntity handles long names correctly`` () =
        let entity = UserEntity()
        let longName = String.replicate 100 "a"

        entity.Name <- longName

        Assert.Equal(longName, entity.Name)

    [<Fact>]
    let ``UserEntity handles long emails correctly`` () =
        let entity = UserEntity()
        let longEmail = String.replicate 250 "a" + "@example.com"

        entity.Email <- longEmail

        Assert.Equal(longEmail, entity.Email)

    [<Fact>]
    let ``UserEntity handles long profile picture URLs correctly`` () =
        let entity = UserEntity()
        let longUrl = "https://example.com/" + String.replicate 1900 "a" + ".jpg"

        entity.ProfilePicUrl <- Some longUrl

        Assert.Equal(Some longUrl, entity.ProfilePicUrl)