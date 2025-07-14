namespace Freetool.Infrastructure.Tests.Database.Repositories

open System
open Xunit
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Infrastructure.Database
open Freetool.Infrastructure.Database.Repositories

module UserRepositoryTests =

    let createInMemoryContext () =
        let options =
            DbContextOptionsBuilder<FreetoolDbContext>()
                .UseInMemoryDatabase(databaseName = Guid.NewGuid().ToString())
                .Options

        new FreetoolDbContext(options)

    let createValidUser () =
        let email =
            Email.Create("test@example.com")
            |> Result.defaultWith (fun _ -> failwith "Invalid email in test setup")

        let url =
            Url.Create("https://google.com")
            |> Result.defaultWith (fun _ -> failwith "Invalid url in test setup")

        User.create "Test User" email (Some url)
        |> Result.defaultWith (fun _ -> failwith "Invalid user in test setup")

    let createValidUserWithEmail emailStr =
        let email =
            Email.Create(emailStr)
            |> Result.defaultWith (fun _ -> failwith "Invalid email in test setup")

        User.create "Test User" email None
        |> Result.defaultWith (fun _ -> failwith "Invalid user in test setup")

    [<Fact>]
    let ``AddAsync successfully adds user to database`` () = task {
        use context = createInMemoryContext ()
        let repository = UserRepository(context) :> IUserRepository
        let user = createValidUser ()

        let! result = repository.AddAsync(user)

        match result with
        | Ok _ ->
            let! savedUser = repository.GetByIdAsync(user.Id)

            match savedUser with
            | Some saved ->
                Assert.Equal(user.Id, saved.Id)
                Assert.Equal(user.Name, saved.Name)
                Assert.Equal(user.Email, saved.Email)
            | None -> Assert.True(false, "User was not saved")
        | Error _ -> Assert.True(false, "Expected Ok result")
    }

    [<Fact>]
    let ``GetByIdAsync returns user when exists`` () = task {
        use context = createInMemoryContext ()
        let repository = UserRepository(context) :> IUserRepository
        let user = createValidUser ()

        let! _ = repository.AddAsync(user)
        let! result = repository.GetByIdAsync(user.Id)

        match result with
        | Some foundUser ->
            Assert.Equal(user.Id, foundUser.Id)
            Assert.Equal(user.Name, foundUser.Name)
            Assert.Equal(user.Email, foundUser.Email)
            Assert.Equal(user.ProfilePicUrl, foundUser.ProfilePicUrl)
        | None -> Assert.True(false, "Expected to find user")
    }

    [<Fact>]
    let ``GetByIdAsync returns None when user does not exist`` () = task {
        use context = createInMemoryContext ()
        let repository = UserRepository(context) :> IUserRepository
        let nonExistentId = UserId.NewId()

        let! result = repository.GetByIdAsync(nonExistentId)

        Assert.Equal(None, result)
    }

    [<Fact>]
    let ``GetByEmailAsync returns user when exists`` () = task {
        use context = createInMemoryContext ()
        let repository = UserRepository(context) :> IUserRepository
        let user = createValidUser ()

        let! _ = repository.AddAsync(user)
        let! result = repository.GetByEmailAsync(user.Email)

        match result with
        | Some foundUser ->
            Assert.Equal(user.Id, foundUser.Id)
            Assert.Equal(user.Email, foundUser.Email)
        | None -> Assert.True(false, "Expected to find user")
    }

    [<Fact>]
    let ``GetByEmailAsync returns None when user does not exist`` () = task {
        use context = createInMemoryContext ()
        let repository = UserRepository(context) :> IUserRepository

        let nonExistentEmail =
            Email.Create("nonexistent@example.com")
            |> Result.defaultWith (fun _ -> failwith "Invalid email")

        let! result = repository.GetByEmailAsync(nonExistentEmail)

        Assert.Equal(None, result)
    }

    [<Fact>]
    let ``GetAllAsync returns all users with pagination`` () = task {
        use context = createInMemoryContext ()
        let repository = UserRepository(context) :> IUserRepository
        let user1 = createValidUserWithEmail "user1@example.com"
        let user2 = createValidUserWithEmail "user2@example.com"
        let user3 = createValidUserWithEmail "user3@example.com"

        let! _ = repository.AddAsync(user1)
        let! _ = repository.AddAsync(user2)
        let! _ = repository.AddAsync(user3)

        let! allUsers = repository.GetAllAsync 0 10
        let! firstTwo = repository.GetAllAsync 0 2
        let! lastOne = repository.GetAllAsync 2 1

        Assert.Equal(3, allUsers.Length)
        Assert.Equal(2, firstTwo.Length)
        Assert.Equal(1, lastOne.Length)
    }

    [<Fact>]
    let ``GetAllAsync returns empty list when no users exist`` () = task {
        use context = createInMemoryContext ()
        let repository = UserRepository(context) :> IUserRepository

        let! result = repository.GetAllAsync 0 10

        Assert.Empty(result)
    }

    [<Fact>]
    let ``UpdateAsync successfully updates existing user`` () = task {
        use context = createInMemoryContext ()
        let repository = UserRepository(context) :> IUserRepository
        let user = createValidUser ()

        let! _ = repository.AddAsync(user)

        let updatedUser =
            User.updateName "Updated Name" user
            |> Result.defaultWith (fun _ -> failwith "Invalid update")

        let! updateResult = repository.UpdateAsync(updatedUser)

        match updateResult with
        | Ok _ ->
            let! retrievedUser = repository.GetByIdAsync(user.Id)

            match retrievedUser with
            | Some retrieved ->
                Assert.Equal("Updated Name", retrieved.Name)
                Assert.Equal(user.Email, retrieved.Email)
            | None -> Assert.True(false, "User not found after update")
        | Error _ -> Assert.True(false, "Expected Ok result")
    }

    [<Fact>]
    let ``UpdateAsync returns NotFound when user does not exist`` () = task {
        use context = createInMemoryContext ()
        let repository = UserRepository(context) :> IUserRepository
        let user = createValidUser ()

        let! result = repository.UpdateAsync(user)

        match result with
        | Error(NotFound _) -> Assert.True(true)
        | _ -> Assert.True(false, "Expected NotFound error")
    }

    [<Fact>]
    let ``DeleteAsync successfully deletes existing user`` () = task {
        use context = createInMemoryContext ()
        let repository = UserRepository(context) :> IUserRepository
        let user = createValidUser ()

        let! _ = repository.AddAsync(user)
        let! deleteResult = repository.DeleteAsync(user.Id)

        match deleteResult with
        | Ok _ ->
            let! retrievedUser = repository.GetByIdAsync(user.Id)
            Assert.Equal(None, retrievedUser)
        | Error _ -> Assert.True(false, "Expected Ok result")
    }

    [<Fact>]
    let ``DeleteAsync returns NotFound when user does not exist`` () = task {
        use context = createInMemoryContext ()
        let repository = UserRepository(context) :> IUserRepository
        let nonExistentId = UserId.NewId()

        let! result = repository.DeleteAsync(nonExistentId)

        match result with
        | Error(NotFound _) -> Assert.True(true)
        | _ -> Assert.True(false, "Expected NotFound error")
    }

    [<Fact>]
    let ``ExistsAsync returns true when user exists`` () = task {
        use context = createInMemoryContext ()
        let repository = UserRepository(context) :> IUserRepository
        let user = createValidUser ()

        let! _ = repository.AddAsync(user)
        let! exists = repository.ExistsAsync(user.Id)

        Assert.True(exists)
    }

    [<Fact>]
    let ``ExistsAsync returns false when user does not exist`` () = task {
        use context = createInMemoryContext ()
        let repository = UserRepository(context) :> IUserRepository
        let nonExistentId = UserId.NewId()

        let! exists = repository.ExistsAsync(nonExistentId)

        Assert.False(exists)
    }

    [<Fact>]
    let ``ExistsByEmailAsync returns true when user with email exists`` () = task {
        use context = createInMemoryContext ()
        let repository = UserRepository(context) :> IUserRepository
        let user = createValidUser ()

        let! _ = repository.AddAsync(user)
        let! exists = repository.ExistsByEmailAsync(user.Email)

        Assert.True(exists)
    }

    [<Fact>]
    let ``ExistsByEmailAsync returns false when user with email does not exist`` () = task {
        use context = createInMemoryContext ()
        let repository = UserRepository(context) :> IUserRepository

        let nonExistentEmail =
            Email.Create("nonexistent@example.com")
            |> Result.defaultWith (fun _ -> failwith "Invalid email")

        let! exists = repository.ExistsByEmailAsync(nonExistentEmail)

        Assert.False(exists)
    }

    [<Fact>]
    let ``Repository handles users with no profile picture`` () = task {
        use context = createInMemoryContext ()
        let repository = UserRepository(context) :> IUserRepository

        let email =
            Email.Create("nopic@example.com")
            |> Result.defaultWith (fun _ -> failwith "Invalid email")

        let user =
            User.create "No Pic User" email None
            |> Result.defaultWith (fun _ -> failwith "Invalid user")

        let! _ = repository.AddAsync(user)
        let! retrievedUser = repository.GetByIdAsync(user.Id)

        match retrievedUser with
        | Some retrieved -> Assert.Equal(None, retrieved.ProfilePicUrl)
        | None -> Assert.True(false, "User not found")
    }

    [<Fact>]
    let ``Repository preserves user timestamps`` () = task {
        use context = createInMemoryContext ()
        let repository = UserRepository(context) :> IUserRepository
        let user = createValidUser ()

        let! _ = repository.AddAsync(user)
        let! retrievedUser = repository.GetByIdAsync(user.Id)

        match retrievedUser with
        | Some retrieved ->
            Assert.Equal(user.CreatedAt, retrieved.CreatedAt)
            Assert.Equal(user.UpdatedAt, retrieved.UpdatedAt)
        | None -> Assert.True(false, "User not found")
    }

    [<Fact>]
    let ``Multiple operations work correctly`` () = task {
        use context = createInMemoryContext ()
        let repository = UserRepository(context) :> IUserRepository
        let user1 = createValidUserWithEmail "user1@example.com"
        let user2 = createValidUserWithEmail "user2@example.com"

        // Add both users
        let! _ = repository.AddAsync(user1)
        let! _ = repository.AddAsync(user2)

        // Verify both exist
        let! exists1 = repository.ExistsAsync(user1.Id)
        let! exists2 = repository.ExistsAsync(user2.Id)
        Assert.True(exists1)
        Assert.True(exists2)

        // Update user1
        let updatedUser1 =
            User.updateName "Updated User 1" user1
            |> Result.defaultWith (fun _ -> failwith "Invalid update")

        let! _ = repository.UpdateAsync(updatedUser1)

        // Delete user2
        let! _ = repository.DeleteAsync(user2.Id)

        // Verify final state
        let! retrievedUser1 = repository.GetByIdAsync(user1.Id)
        let! retrievedUser2 = repository.GetByIdAsync(user2.Id)
        let! allUsers = repository.GetAllAsync 0 10

        match retrievedUser1 with
        | Some user -> Assert.Equal("Updated User 1", user.Name)
        | None -> Assert.True(false, "User1 should still exist")

        Assert.Equal(None, retrievedUser2)
        Assert.Equal(1, allUsers.Length)
    }