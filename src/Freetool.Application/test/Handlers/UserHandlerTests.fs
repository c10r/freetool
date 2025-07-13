namespace Freetool.Application.Tests.Handlers

open System
open Xunit
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Application.DTOs
open Freetool.Application.Commands
open Freetool.Application.Handlers

module UserHandlerTests =

    type MockUserRepository() =
        let mutable users = Map.empty<UserId, User>
        let mutable deletedUsers = Set.empty<UserId>

        member this.AddUser(user: User) = users <- users.Add(user.Id, user)

        member this.Clear() =
            users <- Map.empty
            deletedUsers <- Set.empty

        interface IUserRepository with
            member this.GetByIdAsync(userId: UserId) = task {
                if deletedUsers.Contains(userId) then
                    return None
                else
                    return users.TryFind(userId)
            }

            member this.GetByEmailAsync(email: Email) = task {
                let userOpt =
                    users.Values
                    |> Seq.tryFind (fun u -> u.Email = email && not (deletedUsers.Contains(u.Id)))

                return userOpt
            }

            member this.GetAllAsync (skip: int) (take: int) = task {
                let allUsers =
                    users.Values
                    |> Seq.filter (fun u -> not (deletedUsers.Contains(u.Id)))
                    |> Seq.toList

                return allUsers |> List.skip skip |> List.take (min take allUsers.Length)
            }

            member this.AddAsync(user: User) = task {
                users <- users.Add(user.Id, user)
                return Ok()
            }

            member this.UpdateAsync(user: User) = task {
                if users.ContainsKey(user.Id) && not (deletedUsers.Contains(user.Id)) then
                    users <- users.Add(user.Id, user)
                    return Ok()
                else
                    return Error(NotFound "User not found")
            }

            member this.DeleteAsync(userId: UserId) = task {
                if users.ContainsKey(userId) && not (deletedUsers.Contains(userId)) then
                    deletedUsers <- deletedUsers.Add(userId)
                    return Ok()
                else
                    return Error(NotFound "User not found")
            }

            member this.ExistsAsync(userId: UserId) = task {
                return users.ContainsKey(userId) && not (deletedUsers.Contains(userId))
            }

            member this.ExistsByEmailAsync(email: Email) = task {
                let exists =
                    users.Values
                    |> Seq.exists (fun u -> u.Email = email && not (deletedUsers.Contains(u.Id)))

                return exists
            }

    let createValidUser () =
        let email =
            Email.Create("test@example.com")
            |> Result.defaultWith (fun _ -> failwith "Invalid email in test setup")

        User.create "Test User" email None
        |> Result.defaultWith (fun _ -> failwith "Invalid user in test setup")

    let createValidUserDto name email profilePicUrl = {
        Name = name
        Email = email
        ProfilePicUrl = profilePicUrl
    }

    [<Fact>]
    let ``CreateUser command with valid data creates user successfully`` () = task {
        let repository = MockUserRepository() :> IUserRepository
        let dto = createValidUserDto "John Doe" "john.doe@example.com" null
        let command = CreateUser dto

        let! result = UserHandler.handleCommand repository command

        match result with
        | Ok(UserResult userDto) ->
            Assert.Equal("John Doe", userDto.Name)
            Assert.Equal("john.doe@example.com", userDto.Email)
            Assert.Null(userDto.ProfilePicUrl)
            Assert.NotNull(userDto.Id)
            Assert.True(userDto.CreatedAt <= DateTime.UtcNow)
            Assert.True(userDto.UpdatedAt <= DateTime.UtcNow)
        | _ -> Assert.True(false, "Expected Ok with UserResult")
    }

    [<Fact>]
    let ``CreateUser command with invalid email returns ValidationError`` () = task {
        let repository = MockUserRepository() :> IUserRepository
        let dto = createValidUserDto "John Doe" "invalid-email" null
        let command = CreateUser dto

        let! result = UserHandler.handleCommand repository command

        match result with
        | Error(ValidationError msg) -> Assert.Equal("Invalid email format", msg)
        | _ -> Assert.True(false, "Expected Error with ValidationError")
    }

    [<Fact>]
    let ``CreateUser command with existing email returns Conflict`` () = task {
        let repository = MockUserRepository()
        let existingUser = createValidUser ()
        repository.AddUser(existingUser)

        let dto = createValidUserDto "John Doe" "test@example.com" null
        let command = CreateUser dto

        let! result = UserHandler.handleCommand (repository :> IUserRepository) command

        match result with
        | Error(Conflict msg) -> Assert.Equal("A user with this email already exists", msg)
        | _ -> Assert.True(false, "Expected Error with Conflict")
    }

    [<Fact>]
    let ``GetUserById command with valid ID returns user`` () = task {
        let repository = MockUserRepository()
        let user = createValidUser ()
        repository.AddUser(user)

        let command = GetUserById(user.Id.Value.ToString())

        let! result = UserHandler.handleCommand (repository :> IUserRepository) command

        match result with
        | Ok(UserResult userDto) ->
            Assert.Equal(user.Id.Value.ToString(), userDto.Id)
            Assert.Equal(user.Name, userDto.Name)
            Assert.Equal(user.Email.Value, userDto.Email)
        | _ -> Assert.True(false, "Expected Ok with UserResult")
    }

    [<Fact>]
    let ``GetUserById command with invalid ID format returns ValidationError`` () = task {
        let repository = MockUserRepository() :> IUserRepository
        let command = GetUserById "invalid-guid"

        let! result = UserHandler.handleCommand repository command

        match result with
        | Error(ValidationError msg) -> Assert.Equal("Invalid user ID format", msg)
        | _ -> Assert.True(false, "Expected Error with ValidationError")
    }

    [<Fact>]
    let ``GetUserById command with non-existent ID returns NotFound`` () = task {
        let repository = MockUserRepository() :> IUserRepository
        let nonExistentId = Guid.NewGuid().ToString()
        let command = GetUserById nonExistentId

        let! result = UserHandler.handleCommand repository command

        match result with
        | Error(NotFound msg) -> Assert.Equal("User not found", msg)
        | _ -> Assert.True(false, "Expected Error with NotFound")
    }

    [<Fact>]
    let ``DeleteUser command with valid ID deletes user successfully`` () = task {
        let repository = MockUserRepository()
        let user = createValidUser ()
        repository.AddUser(user)

        let command = DeleteUser(user.Id.Value.ToString())

        let! result = UserHandler.handleCommand (repository :> IUserRepository) command

        match result with
        | Ok(UnitResult _) -> Assert.True(true)
        | _ -> Assert.True(false, "Expected Ok with UnitResult")
    }

    [<Fact>]
    let ``UpdateUserName command with valid data updates user successfully`` () = task {
        let repository = MockUserRepository()
        let user = createValidUser ()
        repository.AddUser(user)

        let command = UpdateUserName(user.Id.Value.ToString(), "Updated Name")

        let! result = UserHandler.handleCommand (repository :> IUserRepository) command

        match result with
        | Ok(UserResult userDto) ->
            Assert.Equal("Updated Name", userDto.Name)
            Assert.Equal(user.Email.Value, userDto.Email)
        | _ -> Assert.True(false, "Expected Ok with UserResult")
    }

    [<Fact>]
    let ``UpdateUserName command with invalid name returns ValidationError`` () = task {
        let repository = MockUserRepository()
        let user = createValidUser ()
        repository.AddUser(user)

        let command = UpdateUserName(user.Id.Value.ToString(), "")

        let! result = UserHandler.handleCommand (repository :> IUserRepository) command

        match result with
        | Error(ValidationError msg) -> Assert.Equal("User name cannot be empty", msg)
        | _ -> Assert.True(false, "Expected Error with ValidationError")
    }

    [<Fact>]
    let ``UpdateUserEmail command with valid email updates user successfully`` () = task {
        let repository = MockUserRepository()
        let user = createValidUser ()
        repository.AddUser(user)

        let command = UpdateUserEmail(user.Id.Value.ToString(), "newemail@example.com")

        let! result = UserHandler.handleCommand (repository :> IUserRepository) command

        match result with
        | Ok(UserResult userDto) ->
            Assert.Equal("newemail@example.com", userDto.Email)
            Assert.Equal(user.Name, userDto.Name)
        | _ -> Assert.True(false, "Expected Ok with UserResult")
    }

    [<Fact>]
    let ``SetProfilePicture command sets profile picture successfully`` () = task {
        let repository = MockUserRepository()
        let user = createValidUser ()
        repository.AddUser(user)

        let command =
            SetProfilePicture(user.Id.Value.ToString(), "https://example.com/newpic.jpg")

        let! result = UserHandler.handleCommand (repository :> IUserRepository) command

        match result with
        | Ok(UserResult userDto) -> Assert.Equal("https://example.com/newpic.jpg", userDto.ProfilePicUrl)
        | _ -> Assert.True(false, "Expected Ok with UserResult")
    }

    [<Fact>]
    let ``RemoveProfilePicture command removes profile picture successfully`` () = task {
        let repository = MockUserRepository()
        let user = createValidUser ()
        repository.AddUser(user)

        let command = RemoveProfilePicture(user.Id.Value.ToString())

        let! result = UserHandler.handleCommand (repository :> IUserRepository) command

        match result with
        | Ok(UserResult userDto) -> Assert.Null(userDto.ProfilePicUrl)
        | _ -> Assert.True(false, "Expected Ok with UserResult")
    }

    [<Fact>]
    let ``GetAllUsers command with valid parameters returns paged results`` () = task {
        let repository = MockUserRepository()
        let user1 = createValidUser ()
        let user2 = createValidUser ()
        repository.AddUser(user1)
        repository.AddUser(user2)

        let command = GetAllUsers(0, 10)

        let! result = UserHandler.handleCommand (repository :> IUserRepository) command

        match result with
        | Ok(UsersResult pagedDto) ->
            Assert.Equal(2, pagedDto.Users.Length)
            Assert.Equal(0, pagedDto.Skip)
            Assert.Equal(10, pagedDto.Take)
        | _ -> Assert.True(false, "Expected Ok with UsersResult")
    }

    [<Fact>]
    let ``GetAllUsers command with negative skip returns ValidationError`` () = task {
        let repository = MockUserRepository() :> IUserRepository
        let command = GetAllUsers(-1, 10)

        let! result = UserHandler.handleCommand repository command

        match result with
        | Error(ValidationError msg) -> Assert.Equal("Skip cannot be negative", msg)
        | _ -> Assert.True(false, "Expected Error with ValidationError")
    }

    [<Fact>]
    let ``GetAllUsers command with invalid take returns ValidationError`` () = task {
        let repository = MockUserRepository() :> IUserRepository
        let command = GetAllUsers(0, 0)

        let! result = UserHandler.handleCommand repository command

        match result with
        | Error(ValidationError msg) -> Assert.Equal("Take must be between 1 and 100", msg)
        | _ -> Assert.True(false, "Expected Error with ValidationError")
    }

    [<Fact>]
    let ``GetAllUsers command with take over 100 returns ValidationError`` () = task {
        let repository = MockUserRepository() :> IUserRepository
        let command = GetAllUsers(0, 101)

        let! result = UserHandler.handleCommand repository command

        match result with
        | Error(ValidationError msg) -> Assert.Equal("Take must be between 1 and 100", msg)
        | _ -> Assert.True(false, "Expected Error with ValidationError")
    }