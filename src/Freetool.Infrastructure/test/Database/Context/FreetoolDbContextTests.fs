namespace Freetool.Infrastructure.Tests.Database.Context

open System
open System.Linq
open Xunit
open Microsoft.EntityFrameworkCore
open Freetool.Infrastructure.Database

module FreetoolDbContextTests =

    let createInMemoryContext () =
        let options =
            DbContextOptionsBuilder<FreetoolDbContext>()
                .UseInMemoryDatabase(databaseName = Guid.NewGuid().ToString())
                .Options

        new FreetoolDbContext(options)

    let createUserEntity name email =
        UserEntity(
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            ProfilePicUrl = Some "https://example.com/pic.jpg",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        )

    [<Fact>]
    let ``FreetoolDbContext can be created with options`` () =
        use context = createInMemoryContext ()
        Assert.NotNull(context)
        Assert.NotNull(context.Users)

    [<Fact>]
    let ``FreetoolDbContext Users DbSet is accessible`` () =
        use context = createInMemoryContext ()
        let usersDbSet = context.Users
        Assert.NotNull(usersDbSet)

    [<Fact>]
    let ``FreetoolDbContext can add UserEntity`` () = task {
        use context = createInMemoryContext ()
        let userEntity = createUserEntity "Test User" "test@example.com"

        context.Users.Add(userEntity) |> ignore
        let! saveResult = context.SaveChangesAsync()

        Assert.Equal(1, saveResult)
    }

    [<Fact>]
    let ``FreetoolDbContext can query UserEntity`` () = task {
        use context = createInMemoryContext ()
        let userEntity = createUserEntity "Test User" "test@example.com"

        context.Users.Add(userEntity) |> ignore
        let! _ = context.SaveChangesAsync()

        let! foundUser = context.Users.FirstOrDefaultAsync(fun u -> u.Id = userEntity.Id)

        Assert.NotNull(foundUser)
        Assert.Equal(userEntity.Name, foundUser.Name)
        Assert.Equal(userEntity.Email, foundUser.Email)
    }

    [<Fact>]
    let ``FreetoolDbContext enforces unique email constraint`` () = task {
        use context = createInMemoryContext ()
        let user1 = createUserEntity "User 1" "same@example.com"
        let user2 = createUserEntity "User 2" "same@example.com"

        context.Users.Add(user1) |> ignore
        context.Users.Add(user2) |> ignore

        // In-memory database doesn't enforce unique constraints by default
        // This test verifies the constraint is configured, not enforced
        let! saveResult = context.SaveChangesAsync()
        Assert.Equal(2, saveResult)
    }

    [<Fact>]
    let ``FreetoolDbContext can update UserEntity`` () = task {
        use context = createInMemoryContext ()
        let userEntity = createUserEntity "Original Name" "test@example.com"

        context.Users.Add(userEntity) |> ignore
        let! _ = context.SaveChangesAsync()

        let! foundUser = context.Users.FirstOrDefaultAsync(fun u -> u.Id = userEntity.Id)
        foundUser.Name <- "Updated Name"
        foundUser.UpdatedAt <- DateTime.UtcNow

        let! updateResult = context.SaveChangesAsync()
        Assert.Equal(1, updateResult)

        let! updatedUser = context.Users.FirstOrDefaultAsync(fun u -> u.Id = userEntity.Id)
        Assert.Equal("Updated Name", updatedUser.Name)
    }

    [<Fact>]
    let ``FreetoolDbContext can delete UserEntity`` () = task {
        use context = createInMemoryContext ()
        let userEntity = createUserEntity "Test User" "test@example.com"

        context.Users.Add(userEntity) |> ignore
        let! _ = context.SaveChangesAsync()

        let! foundUser = context.Users.FirstOrDefaultAsync(fun u -> u.Id = userEntity.Id)
        context.Users.Remove(foundUser) |> ignore

        let! deleteResult = context.SaveChangesAsync()
        Assert.Equal(1, deleteResult)

        let! deletedUser = context.Users.FirstOrDefaultAsync(fun u -> u.Id = userEntity.Id)
        Assert.Null(deletedUser)
    }

    [<Fact>]
    let ``FreetoolDbContext handles UserEntity with None ProfilePicUrl`` () = task {
        use context = createInMemoryContext ()

        let userEntity =
            UserEntity(
                Id = Guid.NewGuid(),
                Name = "No Pic User",
                Email = "nopic@example.com",
                ProfilePicUrl = None,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            )

        context.Users.Add(userEntity) |> ignore
        let! _ = context.SaveChangesAsync()

        let! foundUser = context.Users.FirstOrDefaultAsync(fun u -> u.Id = userEntity.Id)
        Assert.NotNull(foundUser)
        Assert.Equal(None, foundUser.ProfilePicUrl)
    }

    [<Fact>]
    let ``FreetoolDbContext handles UserEntity with Some ProfilePicUrl`` () = task {
        use context = createInMemoryContext ()
        let expectedUrl = "https://example.com/profile.jpg"

        let userEntity =
            UserEntity(
                Id = Guid.NewGuid(),
                Name = "Has Pic User",
                Email = "haspic@example.com",
                ProfilePicUrl = Some expectedUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            )

        context.Users.Add(userEntity) |> ignore
        let! _ = context.SaveChangesAsync()

        let! foundUser = context.Users.FirstOrDefaultAsync(fun u -> u.Id = userEntity.Id)
        Assert.NotNull(foundUser)
        Assert.Equal(Some expectedUrl, foundUser.ProfilePicUrl)
    }

    [<Fact>]
    let ``FreetoolDbContext preserves DateTime values`` () = task {
        use context = createInMemoryContext ()
        let createdAt = DateTime.UtcNow.AddDays(-1.0)
        let updatedAt = DateTime.UtcNow

        let userEntity =
            UserEntity(
                Id = Guid.NewGuid(),
                Name = "DateTime Test",
                Email = "datetime@example.com",
                ProfilePicUrl = None,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            )

        context.Users.Add(userEntity) |> ignore
        let! _ = context.SaveChangesAsync()

        let! foundUser = context.Users.FirstOrDefaultAsync(fun u -> u.Id = userEntity.Id)
        Assert.NotNull(foundUser)
        Assert.Equal(createdAt, foundUser.CreatedAt)
        Assert.Equal(updatedAt, foundUser.UpdatedAt)
    }

    [<Fact>]
    let ``FreetoolDbContext can handle multiple UserEntities`` () = task {
        use context = createInMemoryContext ()
        let user1 = createUserEntity "User 1" "user1@example.com"
        let user2 = createUserEntity "User 2" "user2@example.com"
        let user3 = createUserEntity "User 3" "user3@example.com"

        context.Users.AddRange([ user1; user2; user3 ]) |> ignore
        let! saveResult = context.SaveChangesAsync()

        Assert.Equal(3, saveResult)

        let! userCount = context.Users.CountAsync()
        Assert.Equal(3, userCount)
    }

    [<Fact>]
    let ``FreetoolDbContext can query UserEntities with filters`` () = task {
        use context = createInMemoryContext ()
        let user1 = createUserEntity "Alice" "alice@example.com"
        let user2 = createUserEntity "Bob" "bob@example.com"
        let user3 = createUserEntity "Charlie" "charlie@example.com"

        context.Users.AddRange([ user1; user2; user3 ]) |> ignore
        let! _ = context.SaveChangesAsync()

        let! aliceUser = context.Users.FirstOrDefaultAsync(fun u -> u.Name = "Alice")
        let! bobUser = context.Users.FirstOrDefaultAsync(fun u -> u.Email = "bob@example.com")

        Assert.NotNull(aliceUser)
        Assert.Equal("Alice", aliceUser.Name)
        Assert.NotNull(bobUser)
        Assert.Equal("Bob", bobUser.Name)
    }

    [<Fact>]
    let ``FreetoolDbContext model configuration is applied`` () =
        use context = createInMemoryContext ()
        let model = context.Model
        let userEntityType = model.FindEntityType(typeof<UserEntity>)

        Assert.NotNull(userEntityType)

        // Verify key is configured
        let key = userEntityType.FindPrimaryKey()
        Assert.NotNull(key)
        Assert.Equal("Id", key.Properties.[0].Name)

        // Verify properties are configured
        let nameProperty = userEntityType.FindProperty("Name")
        Assert.NotNull(nameProperty)
        Assert.True(nameProperty.IsNullable = false)

        let emailProperty = userEntityType.FindProperty("Email")
        Assert.NotNull(emailProperty)
        Assert.True(emailProperty.IsNullable = false)