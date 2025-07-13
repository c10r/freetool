namespace Freetool.Application.Tests.Interfaces

open System.Threading.Tasks
open Xunit
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Application.Commands

module InterfaceTests =

    type TestUserRepository() =
        interface IUserRepository with
            member this.GetByIdAsync(userId: UserId) = Task.FromResult(None)

            member this.GetByEmailAsync(email: Email) = Task.FromResult(None)

            member this.GetAllAsync (skip: int) (take: int) = Task.FromResult([])

            member this.AddAsync(user: User) = Task.FromResult(Ok())

            member this.UpdateAsync(user: User) = Task.FromResult(Ok())

            member this.DeleteAsync(userId: UserId) = Task.FromResult(Ok())

            member this.ExistsAsync(userId: UserId) = Task.FromResult(false)

            member this.ExistsByEmailAsync(email: Email) = Task.FromResult(false)

    type TestCommandHandler() =
        interface ICommandHandler with
            member this.HandleCommand repository command = Task.FromResult(Ok(UnitResult()))

    [<Fact>]
    let ``IUserRepository can be implemented`` () =
        let repository = TestUserRepository() :> IUserRepository
        Assert.NotNull(repository)

    [<Fact>]
    let ``IUserRepository GetByIdAsync returns Task<User option>`` () = task {
        let repository = TestUserRepository() :> IUserRepository
        let userId = UserId.NewId()

        let! result = repository.GetByIdAsync(userId)

        Assert.Equal(None, result)
    }

    [<Fact>]
    let ``IUserRepository GetByEmailAsync returns Task<User option>`` () = task {
        let repository = TestUserRepository() :> IUserRepository
        let emailResult = Email.Create("test@example.com")

        match emailResult with
        | Ok email ->
            let! result = repository.GetByEmailAsync(email)
            Assert.Equal(None, result)
        | Error _ -> Assert.True(false, "Failed to create test email")
    }

    [<Fact>]
    let ``IUserRepository GetAllAsync returns Task<User list>`` () = task {
        let repository = TestUserRepository() :> IUserRepository

        let! result = repository.GetAllAsync 0 10

        Assert.Empty(result)
    }

    [<Fact>]
    let ``IUserRepository AddAsync returns Task<Result<unit, DomainError>>`` () = task {
        let repository = TestUserRepository() :> IUserRepository
        let emailResult = Email.Create("test@example.com")

        match emailResult with
        | Ok email ->
            let userResult = User.create "Test User" email None

            match userResult with
            | Ok user ->
                let! result = repository.AddAsync(user)

                match result with
                | Ok _ -> Assert.True(true)
                | Error _ -> Assert.True(false, "Expected Ok result")
            | Error _ -> Assert.True(false, "Failed to create test user")
        | Error _ -> Assert.True(false, "Failed to create test email")
    }

    [<Fact>]
    let ``IUserRepository UpdateAsync returns Task<Result<unit, DomainError>>`` () = task {
        let repository = TestUserRepository() :> IUserRepository
        let emailResult = Email.Create("test@example.com")

        match emailResult with
        | Ok email ->
            let userResult = User.create "Test User" email None

            match userResult with
            | Ok user ->
                let! result = repository.UpdateAsync(user)

                match result with
                | Ok _ -> Assert.True(true)
                | Error _ -> Assert.True(false, "Expected Ok result")
            | Error _ -> Assert.True(false, "Failed to create test user")
        | Error _ -> Assert.True(false, "Failed to create test email")
    }

    [<Fact>]
    let ``IUserRepository DeleteAsync returns Task<Result<unit, DomainError>>`` () = task {
        let repository = TestUserRepository() :> IUserRepository
        let userId = UserId.NewId()

        let! result = repository.DeleteAsync(userId)

        match result with
        | Ok _ -> Assert.True(true)
        | Error _ -> Assert.True(false, "Expected Ok result")
    }

    [<Fact>]
    let ``IUserRepository ExistsAsync returns Task<bool>`` () = task {
        let repository = TestUserRepository() :> IUserRepository
        let userId = UserId.NewId()

        let! result = repository.ExistsAsync(userId)

        Assert.False(result)
    }

    [<Fact>]
    let ``IUserRepository ExistsByEmailAsync returns Task<bool>`` () = task {
        let repository = TestUserRepository() :> IUserRepository
        let emailResult = Email.Create("test@example.com")

        match emailResult with
        | Ok email ->
            let! result = repository.ExistsByEmailAsync(email)
            Assert.False(result)
        | Error _ -> Assert.True(false, "Failed to create test email")
    }

    [<Fact>]
    let ``ICommandHandler can be implemented`` () =
        let handler = TestCommandHandler() :> ICommandHandler
        Assert.NotNull(handler)

    [<Fact>]
    let ``ICommandHandler HandleCommand returns Task<Result<UserCommandResult, DomainError>>`` () = task {
        let handler = TestCommandHandler() :> ICommandHandler
        let repository = TestUserRepository() :> IUserRepository
        let command = GetAllUsers(0, 10)

        let! result = handler.HandleCommand repository command

        match result with
        | Ok(UnitResult _) -> Assert.True(true)
        | _ -> Assert.True(false, "Expected Ok with UnitResult")
    }

    [<Fact>]
    let ``IUserRepository interface defines all required methods`` () =
        let repositoryType = typeof<IUserRepository>
        let methods = repositoryType.GetMethods()

        let expectedMethods = [
            "GetByIdAsync"
            "GetByEmailAsync"
            "GetAllAsync"
            "AddAsync"
            "UpdateAsync"
            "DeleteAsync"
            "ExistsAsync"
            "ExistsByEmailAsync"
        ]

        for expectedMethod in expectedMethods do
            let methodExists = methods |> Array.exists (fun m -> m.Name = expectedMethod)
            Assert.True(methodExists, $"Method {expectedMethod} should exist on IUserRepository")

    [<Fact>]
    let ``ICommandHandler interface defines required method`` () =
        let handlerType = typeof<ICommandHandler>
        let methods = handlerType.GetMethods()

        let handleCommandExists =
            methods |> Array.exists (fun m -> m.Name = "HandleCommand")

        Assert.True(handleCommandExists, "Method HandleCommand should exist on ICommandHandler")