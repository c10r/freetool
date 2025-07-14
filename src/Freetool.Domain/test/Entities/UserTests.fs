namespace Freetool.Domain.Tests.Entities

open System
open Xunit
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities

module UserTests =

    let createValidEmail () =
        Email.Create("test@example.com")
        |> Result.defaultWith (fun _ -> failwith "Invalid email in test setup")

    let createValidUrl () =
        Url.Create("https://google.com")
        |> Result.defaultWith (fun _ -> failwith "Invalid url in test setup")

    [<Fact>]
    let ``create with valid parameters returns Ok with User`` () =
        let email = createValidEmail ()
        let url = Some(createValidUrl ())
        let result = User.create "John Doe" email url

        match result with
        | Ok user ->
            Assert.Equal("John Doe", user.Name)
            Assert.Equal(email, user.Email)
            Assert.Equal(url, user.ProfilePicUrl)
            Assert.True(user.CreatedAt <= DateTime.UtcNow)
            Assert.True(user.UpdatedAt <= DateTime.UtcNow)
            Assert.Equal(user.CreatedAt, user.UpdatedAt)
        | Error _ -> Assert.True(false, "Expected Ok but got Error")

    [<Fact>]
    let ``create with None profile pic returns Ok`` () =
        let email = createValidEmail ()
        let result = User.create "John Doe" email None

        match result with
        | Ok user -> Assert.Equal(None, user.ProfilePicUrl)
        | Error _ -> Assert.True(false, "Expected Ok but got Error")

    [<Fact>]
    let ``create with empty name returns ValidationError`` () =
        let email = createValidEmail ()
        let result = User.create "" email None

        match result with
        | Ok _ -> Assert.True(false, "Expected Error but got Ok")
        | Error(ValidationError msg) -> Assert.Equal("User name cannot be empty", msg)
        | Error _ -> Assert.True(false, "Expected ValidationError")

    [<Fact>]
    let ``create with null name returns ValidationError`` () =
        let email = createValidEmail ()
        let result = User.create null email None

        match result with
        | Ok _ -> Assert.True(false, "Expected Error but got Ok")
        | Error(ValidationError msg) -> Assert.Equal("User name cannot be empty", msg)
        | Error _ -> Assert.True(false, "Expected ValidationError")

    [<Fact>]
    let ``create with whitespace name returns ValidationError`` () =
        let email = createValidEmail ()
        let result = User.create "   " email None

        match result with
        | Ok _ -> Assert.True(false, "Expected Error but got Ok")
        | Error(ValidationError msg) -> Assert.Equal("User name cannot be empty", msg)
        | Error _ -> Assert.True(false, "Expected ValidationError")

    [<Fact>]
    let ``create with name exceeding 100 characters returns ValidationError`` () =
        let email = createValidEmail ()
        let longName = String.replicate 101 "a"
        let result = User.create longName email None

        match result with
        | Ok _ -> Assert.True(false, "Expected Error but got Ok")
        | Error(ValidationError msg) -> Assert.Equal("User name cannot exceed 100 characters", msg)
        | Error _ -> Assert.True(false, "Expected ValidationError")

    [<Fact>]
    let ``create trims whitespace from name`` () =
        let email = createValidEmail ()
        let result = User.create "  John Doe  " email None

        match result with
        | Ok user -> Assert.Equal("John Doe", user.Name)
        | Error _ -> Assert.True(false, "Expected Ok but got Error")

    [<Fact>]
    let ``create generates unique UserId`` () =
        let email = createValidEmail ()
        let result1 = User.create "John Doe" email None
        let result2 = User.create "Jane Doe" email None

        match result1, result2 with
        | Ok user1, Ok user2 -> Assert.NotEqual(user1.Id, user2.Id)
        | _ -> Assert.True(false, "Expected both users to be created successfully")

    [<Fact>]
    let ``updateName with valid name returns Ok with updated User`` () =
        let email = createValidEmail ()
        let originalResult = User.create "John Doe" email None

        match originalResult with
        | Ok originalUser ->
            let updateResult = User.updateName "Jane Smith" originalUser

            match updateResult with
            | Ok updatedUser ->
                Assert.Equal("Jane Smith", updatedUser.Name)
                Assert.Equal(originalUser.Email, updatedUser.Email)
                Assert.Equal(originalUser.ProfilePicUrl, updatedUser.ProfilePicUrl)
                Assert.Equal(originalUser.CreatedAt, updatedUser.CreatedAt)
                Assert.True(updatedUser.UpdatedAt >= originalUser.UpdatedAt)
            | Error _ -> Assert.True(false, "Expected Ok but got Error")
        | Error _ -> Assert.True(false, "Failed to create original user")

    [<Fact>]
    let ``updateName with empty name returns ValidationError`` () =
        let email = createValidEmail ()
        let originalResult = User.create "John Doe" email None

        match originalResult with
        | Ok originalUser ->
            let updateResult = User.updateName "" originalUser

            match updateResult with
            | Ok _ -> Assert.True(false, "Expected Error but got Ok")
            | Error(ValidationError msg) -> Assert.Equal("User name cannot be empty", msg)
            | Error _ -> Assert.True(false, "Expected ValidationError")
        | Error _ -> Assert.True(false, "Failed to create original user")

    [<Fact>]
    let ``updateName with name exceeding 100 characters returns ValidationError`` () =
        let email = createValidEmail ()
        let originalResult = User.create "John Doe" email None

        match originalResult with
        | Ok originalUser ->
            let longName = String.replicate 101 "a"
            let updateResult = User.updateName longName originalUser

            match updateResult with
            | Ok _ -> Assert.True(false, "Expected Error but got Ok")
            | Error(ValidationError msg) -> Assert.Equal("User name cannot exceed 100 characters", msg)
            | Error _ -> Assert.True(false, "Expected ValidationError")
        | Error _ -> Assert.True(false, "Failed to create original user")

    [<Fact>]
    let ``updateName trims whitespace`` () =
        let email = createValidEmail ()
        let originalResult = User.create "John Doe" email None

        match originalResult with
        | Ok originalUser ->
            let updateResult = User.updateName "  Jane Smith  " originalUser

            match updateResult with
            | Ok updatedUser -> Assert.Equal("Jane Smith", updatedUser.Name)
            | Error _ -> Assert.True(false, "Expected Ok but got Error")
        | Error _ -> Assert.True(false, "Failed to create original user")

    [<Fact>]
    let ``updateEmail returns User with updated email and UpdatedAt`` () =
        let originalEmail = createValidEmail ()
        let newEmailResult = Email.Create("newemail@example.com")

        match newEmailResult with
        | Ok newEmail ->
            let originalResult = User.create "John Doe" originalEmail None

            match originalResult with
            | Ok originalUser ->
                let updatedUser = User.updateEmail newEmail originalUser
                Assert.Equal(newEmail, updatedUser.Email)
                Assert.Equal(originalUser.Name, updatedUser.Name)
                Assert.Equal(originalUser.ProfilePicUrl, updatedUser.ProfilePicUrl)
                Assert.Equal(originalUser.CreatedAt, updatedUser.CreatedAt)
                Assert.True(updatedUser.UpdatedAt >= originalUser.UpdatedAt)
            | Error _ -> Assert.True(false, "Failed to create original user")
        | Error _ -> Assert.True(false, "Failed to create new email")

    [<Fact>]
    let ``updateProfilePic with Some URL returns User with updated profile pic`` () =
        let email = createValidEmail ()
        let originalResult = User.create "John Doe" email None

        match originalResult with
        | Ok originalUser ->
            let url = createValidUrl ()
            let updatedUser = User.updateProfilePic (Some url) originalUser

            Assert.Equal(Some(url), updatedUser.ProfilePicUrl)
            Assert.Equal(originalUser.Name, updatedUser.Name)
            Assert.Equal(originalUser.Email, updatedUser.Email)
            Assert.Equal(originalUser.CreatedAt, updatedUser.CreatedAt)
            Assert.True(updatedUser.UpdatedAt >= originalUser.UpdatedAt)
        | Error _ -> Assert.True(false, "Failed to create original user")

    [<Fact>]
    let ``updateProfilePic with None removes profile pic`` () =
        let email = createValidEmail ()
        let url = createValidUrl ()

        let originalResult = User.create "John Doe" email (Some url)

        match originalResult with
        | Ok originalUser ->
            let updatedUser = User.updateProfilePic None originalUser
            Assert.Equal(None, updatedUser.ProfilePicUrl)
            Assert.Equal(originalUser.Name, updatedUser.Name)
            Assert.Equal(originalUser.Email, updatedUser.Email)
            Assert.Equal(originalUser.CreatedAt, updatedUser.CreatedAt)
            Assert.True(updatedUser.UpdatedAt >= originalUser.UpdatedAt)
        | Error _ -> Assert.True(false, "Failed to create original user")

    [<Fact>]
    let ``User implements IEntity interface`` () =
        let email = createValidEmail ()
        let result = User.create "John Doe" email None

        match result with
        | Ok user ->
            let entity = user :> IEntity<UserId>
            Assert.Equal(user.Id, entity.Id)
        | Error _ -> Assert.True(false, "Failed to create user")