namespace Freetool.Api.Tests.Controllers

open System
open Xunit
open Microsoft.AspNetCore.Mvc
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Application.DTOs
open Freetool.Application.Commands
open Freetool.Api.Controllers

module UserControllerTests =

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

    type MockCommandHandler(repository: IUserRepository) =
        interface ICommandHandler with
            member this.HandleCommand _ command = task {
                match command with
                | CreateUser dto ->
                    match Email.Create(dto.Email) with
                    | Ok email ->
                        let! existsWithEmail = repository.ExistsByEmailAsync(email)

                        if existsWithEmail then
                            return Error(Conflict "A user with this email already exists")
                        else
                            let urlOption =
                                if System.String.IsNullOrWhiteSpace(dto.ProfilePicUrl) then
                                    None
                                else
                                    Url.Create dto.ProfilePicUrl
                                    |> Result.map Some
                                    |> Result.defaultWith (fun _ -> failwith "Invalid url in test setup")

                            match User.create (dto.Name) (email) urlOption with
                            | Ok user ->
                                let! addResult = repository.AddAsync(user)

                                match addResult with
                                | Ok _ ->
                                    let url =
                                        user.ProfilePicUrl
                                        |> Option.map (fun url -> url.Value)
                                        |> Option.defaultValue null

                                    let userDto = {
                                        Id = user.Id.Value.ToString()
                                        Name = user.Name
                                        Email = user.Email.Value
                                        ProfilePicUrl = url
                                        CreatedAt = user.CreatedAt
                                        UpdatedAt = user.UpdatedAt
                                    }

                                    return Ok(UserResult userDto)
                                | Error err -> return Error err
                            | Error err -> return Error err
                    | Error err -> return Error err

                | GetUserById idStr ->
                    match Guid.TryParse(idStr) with
                    | true, guid ->
                        let userId = UserId.FromGuid(guid)
                        let! userOpt = repository.GetByIdAsync(userId)

                        match userOpt with
                        | Some user ->
                            let url =
                                user.ProfilePicUrl
                                |> Option.map (fun url -> url.Value)
                                |> Option.defaultValue null

                            let userDto = {
                                Id = user.Id.Value.ToString()
                                Name = user.Name
                                Email = user.Email.Value
                                ProfilePicUrl = url
                                CreatedAt = user.CreatedAt
                                UpdatedAt = user.UpdatedAt
                            }

                            return Ok(UserResult userDto)
                        | None -> return Error(NotFound "User not found")
                    | false, _ -> return Error(ValidationError "Invalid user ID format")

                | GetUserByEmail emailStr ->
                    match Email.Create(emailStr) with
                    | Ok email ->
                        let! userOpt = repository.GetByEmailAsync(email)

                        match userOpt with
                        | Some user ->
                            let url =
                                user.ProfilePicUrl
                                |> Option.map (fun url -> url.Value)
                                |> Option.defaultValue null

                            let userDto = {
                                Id = user.Id.Value.ToString()
                                Name = user.Name
                                Email = user.Email.Value
                                ProfilePicUrl = url
                                CreatedAt = user.CreatedAt
                                UpdatedAt = user.UpdatedAt
                            }

                            return Ok(UserResult userDto)
                        | None -> return Error(NotFound "User not found")
                    | Error err -> return Error err

                | GetAllUsers(skip, take) ->
                    if skip < 0 then
                        return Error(ValidationError "Skip cannot be negative")
                    elif take < 1 || take > 100 then
                        return Error(ValidationError "Take must be between 1 and 100")
                    else
                        let! users = repository.GetAllAsync skip take

                        let userDtos =
                            users
                            |> List.map (fun user ->
                                let url =
                                    user.ProfilePicUrl
                                    |> Option.map (fun url -> url.Value)
                                    |> Option.defaultValue null

                                {
                                    Id = user.Id.Value.ToString()
                                    Name = user.Name
                                    Email = user.Email.Value
                                    ProfilePicUrl = url
                                    CreatedAt = user.CreatedAt
                                    UpdatedAt = user.UpdatedAt
                                })

                        let pagedResult = {
                            Users = userDtos
                            TotalCount = userDtos.Length
                            Skip = skip
                            Take = take
                        }

                        return Ok(UsersResult pagedResult)

                | UpdateUserName(idStr, newName) ->
                    if String.IsNullOrWhiteSpace(newName) then
                        return Error(ValidationError "User name cannot be empty")
                    else
                        match Guid.TryParse(idStr) with
                        | true, guid ->
                            let userId = UserId.FromGuid(guid)
                            let! userOpt = repository.GetByIdAsync(userId)

                            match userOpt with
                            | Some user ->
                                match User.updateName newName user with
                                | Ok updatedUser ->
                                    let! updateResult = repository.UpdateAsync(updatedUser)

                                    match updateResult with
                                    | Ok _ ->
                                        let url =
                                            updatedUser.ProfilePicUrl
                                            |> Option.map (fun url -> url.Value)
                                            |> Option.defaultValue null

                                        let userDto = {
                                            Id = updatedUser.Id.Value.ToString()
                                            Name = updatedUser.Name
                                            Email = updatedUser.Email.Value
                                            ProfilePicUrl = url
                                            CreatedAt = updatedUser.CreatedAt
                                            UpdatedAt = updatedUser.UpdatedAt
                                        }

                                        return Ok(UserResult userDto)
                                    | Error err -> return Error err
                                | Error err -> return Error err
                            | None -> return Error(NotFound "User not found")
                        | false, _ -> return Error(ValidationError "Invalid user ID format")

                | UpdateUserEmail(idStr, newEmailStr) ->
                    match Email.Create(newEmailStr) with
                    | Ok newEmail ->
                        match Guid.TryParse(idStr) with
                        | true, guid ->
                            let userId = UserId.FromGuid(guid)
                            let! userOpt = repository.GetByIdAsync(userId)

                            match userOpt with
                            | Some user ->
                                let! existsWithEmail = repository.ExistsByEmailAsync(newEmail)

                                if existsWithEmail && user.Email <> newEmail then
                                    return Error(Conflict "A user with this email already exists")
                                else
                                    let updatedUser = User.updateEmail newEmail user
                                    let! updateResult = repository.UpdateAsync(updatedUser)

                                    match updateResult with
                                    | Ok _ ->
                                        let url =
                                            updatedUser.ProfilePicUrl
                                            |> Option.map (fun url -> url.Value)
                                            |> Option.defaultValue null

                                        let userDto = {
                                            Id = updatedUser.Id.Value.ToString()
                                            Name = updatedUser.Name
                                            Email = updatedUser.Email.Value
                                            ProfilePicUrl = url
                                            CreatedAt = updatedUser.CreatedAt
                                            UpdatedAt = updatedUser.UpdatedAt
                                        }

                                        return Ok(UserResult userDto)
                                    | Error err -> return Error err
                            | None -> return Error(NotFound "User not found")
                        | false, _ -> return Error(ValidationError "Invalid user ID format")
                    | Error err -> return Error err

                | SetProfilePicture(idStr, profilePicUrl) ->
                    match Guid.TryParse(idStr) with
                    | true, guid ->
                        let userId = UserId.FromGuid(guid)
                        let! userOpt = repository.GetByIdAsync(userId)

                        match userOpt with
                        | Some user ->
                            let url =
                                Url.Create(profilePicUrl)
                                |> Result.defaultWith (fun _ -> failwith "Invalid url in test setup")

                            let updatedUser = User.updateProfilePic (Some url) user
                            let! updateResult = repository.UpdateAsync(updatedUser)

                            match updateResult with
                            | Ok _ ->
                                let url =
                                    updatedUser.ProfilePicUrl
                                    |> Option.map (fun url -> url.Value)
                                    |> Option.defaultValue null

                                let userDto = {
                                    Id = updatedUser.Id.Value.ToString()
                                    Name = updatedUser.Name
                                    Email = updatedUser.Email.Value
                                    ProfilePicUrl = url
                                    CreatedAt = updatedUser.CreatedAt
                                    UpdatedAt = updatedUser.UpdatedAt
                                }

                                return Ok(UserResult userDto)
                            | Error err -> return Error err
                        | None -> return Error(NotFound "User not found")
                    | false, _ -> return Error(ValidationError "Invalid user ID format")

                | RemoveProfilePicture idStr ->
                    match Guid.TryParse(idStr) with
                    | true, guid ->
                        let userId = UserId.FromGuid(guid)
                        let! userOpt = repository.GetByIdAsync(userId)

                        match userOpt with
                        | Some user ->
                            let updatedUser = User.updateProfilePic None user
                            let! updateResult = repository.UpdateAsync(updatedUser)

                            match updateResult with
                            | Ok _ ->
                                let url =
                                    updatedUser.ProfilePicUrl
                                    |> Option.map (fun url -> url.Value)
                                    |> Option.defaultValue null

                                let userDto = {
                                    Id = updatedUser.Id.Value.ToString()
                                    Name = updatedUser.Name
                                    Email = updatedUser.Email.Value
                                    ProfilePicUrl = url
                                    CreatedAt = updatedUser.CreatedAt
                                    UpdatedAt = updatedUser.UpdatedAt
                                }

                                return Ok(UserResult userDto)
                            | Error err -> return Error err
                        | None -> return Error(NotFound "User not found")
                    | false, _ -> return Error(ValidationError "Invalid user ID format")

                | DeleteUser idStr ->
                    match Guid.TryParse(idStr) with
                    | true, guid ->
                        let userId = UserId.FromGuid(guid)
                        let! deleteResult = repository.DeleteAsync(userId)

                        match deleteResult with
                        | Ok _ -> return Ok(UnitResult())
                        | Error err -> return Error err
                    | false, _ -> return Error(ValidationError "Invalid user ID format")
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
    let ``CreateUser endpoint with valid data returns Created`` () = task {
        let repository = MockUserRepository() :> IUserRepository
        let commandHandler = MockCommandHandler(repository) :> ICommandHandler
        let controller = UserController(repository, commandHandler)

        let dto = createValidUserDto "John Doe" "john.doe@example.com" null

        let! result = controller.CreateUser(dto)

        match result with
        | :? CreatedAtActionResult as createdResult ->
            let userDto = createdResult.Value :?> UserDto
            Assert.Equal("John Doe", userDto.Name)
            Assert.Equal("john.doe@example.com", userDto.Email)
            Assert.Null(userDto.ProfilePicUrl)
        | _ -> Assert.True(false, "Expected CreatedAtActionResult")
    }

    [<Fact>]
    let ``CreateUser endpoint with invalid email returns BadRequest`` () = task {
        let repository = MockUserRepository() :> IUserRepository
        let commandHandler = MockCommandHandler(repository) :> ICommandHandler
        let controller = UserController(repository, commandHandler)

        let dto = createValidUserDto "John Doe" "invalid-email" null

        let! result = controller.CreateUser(dto)

        match result with
        | :? BadRequestObjectResult as badResult -> Assert.Equal(400, badResult.StatusCode.Value)
        | _ -> Assert.True(false, "Expected BadRequestObjectResult")
    }

    [<Fact>]
    let ``CreateUser endpoint with existing email returns Conflict`` () = task {
        let repository = MockUserRepository()
        let existingUser = createValidUser ()
        repository.AddUser(existingUser)

        let commandHandler =
            MockCommandHandler(repository :> IUserRepository) :> ICommandHandler

        let controller = UserController(repository :> IUserRepository, commandHandler)

        let dto = createValidUserDto "John Doe" "test@example.com" null

        let! result = controller.CreateUser(dto)

        match result with
        | :? ConflictObjectResult as conflictResult -> Assert.Equal(409, conflictResult.StatusCode.Value)
        | _ -> Assert.True(false, "Expected ConflictObjectResult")
    }

    [<Fact>]
    let ``GetUserById endpoint with valid ID returns Ok`` () = task {
        let repository = MockUserRepository()
        let user = createValidUser ()
        repository.AddUser(user)

        let commandHandler =
            MockCommandHandler(repository :> IUserRepository) :> ICommandHandler

        let controller = UserController(repository :> IUserRepository, commandHandler)

        let! result = controller.GetUserById(user.Id.Value.ToString())

        match result with
        | :? OkObjectResult as okResult ->
            let userDto = okResult.Value :?> UserDto
            Assert.Equal(user.Id.Value.ToString(), userDto.Id)
            Assert.Equal(user.Name, userDto.Name)
        | _ -> Assert.True(false, "Expected OkObjectResult")
    }

    [<Fact>]
    let ``GetUserById endpoint with invalid ID returns BadRequest`` () = task {
        let repository = MockUserRepository() :> IUserRepository
        let commandHandler = MockCommandHandler(repository) :> ICommandHandler
        let controller = UserController(repository, commandHandler)

        let! result = controller.GetUserById("invalid-guid")

        match result with
        | :? BadRequestObjectResult as badResult -> Assert.Equal(400, badResult.StatusCode.Value)
        | _ -> Assert.True(false, "Expected BadRequestObjectResult")
    }

    [<Fact>]
    let ``GetUserById endpoint with non-existent ID returns NotFound`` () = task {
        let repository = MockUserRepository() :> IUserRepository
        let commandHandler = MockCommandHandler(repository) :> ICommandHandler
        let controller = UserController(repository, commandHandler)

        let nonExistentId = Guid.NewGuid().ToString()

        let! result = controller.GetUserById(nonExistentId)

        match result with
        | :? NotFoundObjectResult as notFoundResult -> Assert.Equal(404, notFoundResult.StatusCode.Value)
        | _ -> Assert.True(false, "Expected NotFoundObjectResult")
    }

    [<Fact>]
    let ``GetUserByEmail endpoint with valid email returns Ok`` () = task {
        let repository = MockUserRepository()
        let user = createValidUser ()
        repository.AddUser(user)

        let commandHandler =
            MockCommandHandler(repository :> IUserRepository) :> ICommandHandler

        let controller = UserController(repository :> IUserRepository, commandHandler)

        let! result = controller.GetUserByEmail("test@example.com")

        match result with
        | :? OkObjectResult as okResult ->
            let userDto = okResult.Value :?> UserDto
            Assert.Equal(user.Email.Value, userDto.Email)
        | _ -> Assert.True(false, "Expected OkObjectResult")
    }

    [<Fact>]
    let ``GetUsers endpoint with valid parameters returns Ok`` () = task {
        let repository = MockUserRepository()
        let user1 = createValidUser ()
        let user2 = createValidUser ()
        repository.AddUser(user1)
        repository.AddUser(user2)

        let commandHandler =
            MockCommandHandler(repository :> IUserRepository) :> ICommandHandler

        let controller = UserController(repository :> IUserRepository, commandHandler)

        let! result = controller.GetUsers(0, 10)

        match result with
        | :? OkObjectResult as okResult ->
            let pagedDto = okResult.Value :?> PagedUsersDto
            Assert.Equal(2, pagedDto.Users.Length)
            Assert.Equal(0, pagedDto.Skip)
            Assert.Equal(10, pagedDto.Take)
        | _ -> Assert.True(false, "Expected OkObjectResult")
    }

    [<Fact>]
    let ``GetUsers endpoint with negative skip returns Ok with corrected value`` () = task {
        let repository = MockUserRepository() :> IUserRepository
        let commandHandler = MockCommandHandler(repository) :> ICommandHandler
        let controller = UserController(repository, commandHandler)

        let! result = controller.GetUsers(-5, 10)

        match result with
        | :? OkObjectResult as okResult ->
            let pagedDto = okResult.Value :?> PagedUsersDto
            Assert.Equal(0, pagedDto.Skip)
        | _ -> Assert.True(false, "Expected OkObjectResult")
    }

    [<Fact>]
    let ``GetUsers endpoint with take over 100 returns Ok with corrected value`` () = task {
        let repository = MockUserRepository() :> IUserRepository
        let commandHandler = MockCommandHandler(repository) :> ICommandHandler
        let controller = UserController(repository, commandHandler)

        let! result = controller.GetUsers(0, 150)

        match result with
        | :? OkObjectResult as okResult ->
            let pagedDto = okResult.Value :?> PagedUsersDto
            Assert.Equal(100, pagedDto.Take)
        | _ -> Assert.True(false, "Expected OkObjectResult")
    }

    [<Fact>]
    let ``UpdateUserName endpoint with valid data returns Ok`` () = task {
        let repository = MockUserRepository()
        let user = createValidUser ()
        repository.AddUser(user)

        let commandHandler =
            MockCommandHandler(repository :> IUserRepository) :> ICommandHandler

        let controller = UserController(repository :> IUserRepository, commandHandler)

        let updateDto = { Name = "Updated Name" }

        let! result = controller.UpdateUserName(user.Id.Value.ToString(), updateDto)

        match result with
        | :? OkObjectResult as okResult ->
            let userDto = okResult.Value :?> UserDto
            Assert.Equal("Updated Name", userDto.Name)
        | _ -> Assert.True(false, "Expected OkObjectResult")
    }

    [<Fact>]
    let ``UpdateUserEmail endpoint with valid email returns Ok`` () = task {
        let repository = MockUserRepository()
        let user = createValidUser ()
        repository.AddUser(user)

        let commandHandler =
            MockCommandHandler(repository :> IUserRepository) :> ICommandHandler

        let controller = UserController(repository :> IUserRepository, commandHandler)

        let updateDto = { Email = "newemail@example.com" }

        let! result = controller.UpdateUserEmail(user.Id.Value.ToString(), updateDto)

        match result with
        | :? OkObjectResult as okResult ->
            let userDto = okResult.Value :?> UserDto
            Assert.Equal("newemail@example.com", userDto.Email)
        | _ -> Assert.True(false, "Expected OkObjectResult")
    }

    [<Fact>]
    let ``SetProfilePicture endpoint sets profile picture successfully`` () = task {
        let repository = MockUserRepository()
        let user = createValidUser ()
        repository.AddUser(user)

        let commandHandler =
            MockCommandHandler(repository :> IUserRepository) :> ICommandHandler

        let controller = UserController(repository :> IUserRepository, commandHandler)

        let setDto = {
            ProfilePicUrl = "https://example.com/newpic.jpg"
        }

        let! result = controller.SetProfilePicture(user.Id.Value.ToString(), setDto)

        match result with
        | :? OkObjectResult as okResult ->
            let userDto = okResult.Value :?> UserDto
            Assert.Equal("https://example.com/newpic.jpg", userDto.ProfilePicUrl)
        | _ -> Assert.True(false, "Expected OkObjectResult")
    }

    [<Fact>]
    let ``RemoveProfilePicture endpoint removes profile picture successfully`` () = task {
        let repository = MockUserRepository()
        let user = createValidUser ()
        repository.AddUser(user)

        let commandHandler =
            MockCommandHandler(repository :> IUserRepository) :> ICommandHandler

        let controller = UserController(repository :> IUserRepository, commandHandler)

        let! result = controller.RemoveProfilePicture(user.Id.Value.ToString())

        match result with
        | :? OkObjectResult as okResult ->
            let userDto = okResult.Value :?> UserDto
            Assert.Null(userDto.ProfilePicUrl)
        | _ -> Assert.True(false, "Expected OkObjectResult")
    }

    [<Fact>]
    let ``DeleteUser endpoint with valid ID returns NoContent`` () = task {
        let repository = MockUserRepository()
        let user = createValidUser ()
        repository.AddUser(user)

        let commandHandler =
            MockCommandHandler(repository :> IUserRepository) :> ICommandHandler

        let controller = UserController(repository :> IUserRepository, commandHandler)

        let! result = controller.DeleteUser(user.Id.Value.ToString())

        match result with
        | :? NoContentResult as noContentResult -> Assert.Equal(204, noContentResult.StatusCode)
        | _ -> Assert.True(false, "Expected NoContentResult")
    }

    [<Fact>]
    let ``DeleteUser endpoint with non-existent ID returns NotFound`` () = task {
        let repository = MockUserRepository() :> IUserRepository
        let commandHandler = MockCommandHandler(repository) :> ICommandHandler
        let controller = UserController(repository, commandHandler)

        let nonExistentId = Guid.NewGuid().ToString()

        let! result = controller.DeleteUser(nonExistentId)

        match result with
        | :? NotFoundObjectResult as notFoundResult -> Assert.Equal(404, notFoundResult.StatusCode.Value)
        | _ -> Assert.True(false, "Expected NotFoundObjectResult")
    }