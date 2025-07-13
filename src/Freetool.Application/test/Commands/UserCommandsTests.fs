namespace Freetool.Application.Tests.Commands

open System
open Xunit
open Freetool.Application.DTOs
open Freetool.Application.Commands

module UserCommandsTests =

    [<Fact>]
    let ``CreateUser command can be created with CreateUserDto`` () =
        let dto = {
            Name = "John Doe"
            Email = "john.doe@example.com"
            ProfilePicUrl = "https://example.com/pic.jpg"
        }

        let command = CreateUser dto

        match command with
        | CreateUser actualDto -> Assert.Equal(dto, actualDto)
        | _ -> Assert.True(false, "Expected CreateUser command")

    [<Fact>]
    let ``GetUserById command can be created with userId string`` () =
        let userId = "12345678-1234-1234-1234-123456789012"
        let command = GetUserById userId

        match command with
        | GetUserById actualUserId -> Assert.Equal(userId, actualUserId)
        | _ -> Assert.True(false, "Expected GetUserById command")

    [<Fact>]
    let ``GetUserByEmail command can be created with email string`` () =
        let email = "test@example.com"
        let command = GetUserByEmail email

        match command with
        | GetUserByEmail actualEmail -> Assert.Equal(email, actualEmail)
        | _ -> Assert.True(false, "Expected GetUserByEmail command")

    [<Fact>]
    let ``GetAllUsers command can be created with skip and take parameters`` () =
        let skip = 10
        let take = 20
        let command = GetAllUsers(skip, take)

        match command with
        | GetAllUsers(actualSkip, actualTake) ->
            Assert.Equal(skip, actualSkip)
            Assert.Equal(take, actualTake)
        | _ -> Assert.True(false, "Expected GetAllUsers command")

    [<Fact>]
    let ``DeleteUser command can be created with userId`` () =
        let userId = "12345678-1234-1234-1234-123456789012"
        let command = DeleteUser userId

        match command with
        | DeleteUser actualUserId -> Assert.Equal(userId, actualUserId)
        | _ -> Assert.True(false, "Expected DeleteUser command")

    [<Fact>]
    let ``UpdateUserName command can be created with userId and name`` () =
        let userId = "12345678-1234-1234-1234-123456789012"
        let name = "New Name"
        let command = UpdateUserName(userId, name)

        match command with
        | UpdateUserName(actualUserId, actualName) ->
            Assert.Equal(userId, actualUserId)
            Assert.Equal(name, actualName)
        | _ -> Assert.True(false, "Expected UpdateUserName command")

    [<Fact>]
    let ``UpdateUserEmail command can be created with userId and email`` () =
        let userId = "12345678-1234-1234-1234-123456789012"
        let email = "newemail@example.com"
        let command = UpdateUserEmail(userId, email)

        match command with
        | UpdateUserEmail(actualUserId, actualEmail) ->
            Assert.Equal(userId, actualUserId)
            Assert.Equal(email, actualEmail)
        | _ -> Assert.True(false, "Expected UpdateUserEmail command")

    [<Fact>]
    let ``SetProfilePicture command can be created with userId and profilePicUrl`` () =
        let userId = "12345678-1234-1234-1234-123456789012"
        let profilePicUrl = "https://example.com/newpic.jpg"
        let command = SetProfilePicture(userId, profilePicUrl)

        match command with
        | SetProfilePicture(actualUserId, actualProfilePicUrl) ->
            Assert.Equal(userId, actualUserId)
            Assert.Equal(profilePicUrl, actualProfilePicUrl)
        | _ -> Assert.True(false, "Expected SetProfilePicture command")

    [<Fact>]
    let ``RemoveProfilePicture command can be created with userId`` () =
        let userId = "12345678-1234-1234-1234-123456789012"
        let command = RemoveProfilePicture userId

        match command with
        | RemoveProfilePicture actualUserId -> Assert.Equal(userId, actualUserId)
        | _ -> Assert.True(false, "Expected RemoveProfilePicture command")

    [<Fact>]
    let ``Different UserCommand cases are distinct`` () =
        let userId = "12345678-1234-1234-1234-123456789012"

        let createDto = {
            Name = "John Doe"
            Email = "john.doe@example.com"
            ProfilePicUrl = null
        }

        let commands = [
            CreateUser createDto
            GetUserById userId
            GetUserByEmail "test@example.com"
            GetAllUsers(0, 10)
            DeleteUser userId
            UpdateUserName(userId, "New Name")
            UpdateUserEmail(userId, "new@example.com")
            SetProfilePicture(userId, "https://example.com/pic.jpg")
            RemoveProfilePicture userId
        ]

        // Verify all commands are different from each other
        for i in 0 .. commands.Length - 2 do
            for j in i + 1 .. commands.Length - 1 do
                Assert.NotEqual(commands.[i], commands.[j])

    [<Fact>]
    let ``UserResult can contain UserDto`` () =
        let userDto = {
            Id = "12345678-1234-1234-1234-123456789012"
            Name = "John Doe"
            Email = "john.doe@example.com"
            ProfilePicUrl = null
            CreatedAt = DateTime.UtcNow
            UpdatedAt = DateTime.UtcNow
        }

        let result = UserResult userDto

        match result with
        | UserResult actualDto -> Assert.Equal(userDto, actualDto)
        | _ -> Assert.True(false, "Expected UserResult")

    [<Fact>]
    let ``UsersResult can contain PagedUsersDto`` () =
        let pagedDto = {
            Users = []
            TotalCount = 0
            Skip = 0
            Take = 10
        }

        let result = UsersResult pagedDto

        match result with
        | UsersResult actualDto -> Assert.Equal(pagedDto, actualDto)
        | _ -> Assert.True(false, "Expected UsersResult")

    [<Fact>]
    let ``UnitResult can contain unit`` () =
        let result = UnitResult()

        match result with
        | UnitResult _ -> Assert.True(true)
        | _ -> Assert.True(false, "Expected UnitResult")

    [<Fact>]
    let ``Different UserCommandResult cases are distinct`` () =
        let userDto = {
            Id = "12345678-1234-1234-1234-123456789012"
            Name = "John Doe"
            Email = "john.doe@example.com"
            ProfilePicUrl = null
            CreatedAt = DateTime.UtcNow
            UpdatedAt = DateTime.UtcNow
        }

        let pagedDto = {
            Users = []
            TotalCount = 0
            Skip = 0
            Take = 10
        }

        let userResult = UserResult userDto
        let usersResult = UsersResult pagedDto
        let unitResult = UnitResult()

        Assert.NotEqual(userResult, usersResult)
        Assert.NotEqual(userResult, unitResult)
        Assert.NotEqual(usersResult, unitResult)