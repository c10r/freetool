namespace Freetool.Domain.Tests.ValueObjects

open Xunit
open Freetool.Domain
open Freetool.Domain.ValueObjects

module EmailTests =

    [<Fact>]
    let ``Create with valid email returns Ok`` () =
        let result = Email.Create("test@example.com")

        match result with
        | Ok email -> Assert.Equal("test@example.com", email.Value)
        | Error _ -> Assert.True(false, "Expected Ok but got Error")

    [<Fact>]
    let ``Create with empty email returns ValidationError`` () =
        let result = Email.Create("")

        match result with
        | Ok _ -> Assert.True(false, "Expected Error but got Ok")
        | Error(ValidationError msg) -> Assert.Equal("Email cannot be empty", msg)
        | Error _ -> Assert.True(false, "Expected ValidationError")

    [<Fact>]
    let ``Create with null email returns ValidationError`` () =
        let result = Email.Create(null)

        match result with
        | Ok _ -> Assert.True(false, "Expected Error but got Ok")
        | Error(ValidationError msg) -> Assert.Equal("Email cannot be empty", msg)
        | Error _ -> Assert.True(false, "Expected ValidationError")

    [<Fact>]
    let ``Create with whitespace email returns ValidationError`` () =
        let result = Email.Create("   ")

        match result with
        | Ok _ -> Assert.True(false, "Expected Error but got Ok")
        | Error(ValidationError msg) -> Assert.Equal("Email cannot be empty", msg)
        | Error _ -> Assert.True(false, "Expected ValidationError")

    [<Fact>]
    let ``Create with email exceeding 254 characters returns ValidationError`` () =
        let longEmail = String.replicate 250 "a" + "@example.com"
        let result = Email.Create(longEmail)

        match result with
        | Ok _ -> Assert.True(false, "Expected Error but got Ok")
        | Error(ValidationError msg) -> Assert.Equal("Email cannot exceed 254 characters", msg)
        | Error _ -> Assert.True(false, "Expected ValidationError")

    [<Fact>]
    let ``Create with invalid email format returns ValidationError`` () =
        let invalidEmails = [
            "invalid-email"
            "@example.com"
            "test@"
            "test.example.com"
            "test@@example.com"
            "test@example"
            "test@.com"
        ]

        for invalidEmail in invalidEmails do
            let result = Email.Create(invalidEmail)

            match result with
            | Ok _ -> Assert.True(false, $"Expected Error for '{invalidEmail}' but got Ok")
            | Error(ValidationError msg) -> Assert.Equal("Invalid email format", msg)
            | Error _ -> Assert.True(false, "Expected ValidationError")

    [<Fact>]
    let ``Create with valid email formats returns Ok`` () =
        let validEmails = [
            "test@example.com"
            "user.name@example.com"
            "user+tag@example.co.uk"
            "123@example.org"
            "test.email-with-hyphen@example.com"
            "test@subdomain.example.com"
        ]

        for validEmail in validEmails do
            let result = Email.Create(validEmail)

            match result with
            | Ok email -> Assert.Equal(validEmail, email.Value)
            | Error _ -> Assert.True(false, $"Expected Ok for '{validEmail}' but got Error")

    [<Fact>]
    let ``Value property returns the underlying email string`` () =
        let emailString = "test@example.com"
        let result = Email.Create(emailString)

        match result with
        | Ok email -> Assert.Equal(emailString, email.Value)
        | Error _ -> Assert.True(false, "Expected Ok but got Error")

    [<Fact>]
    let ``ToString returns the email string`` () =
        let emailString = "test@example.com"
        let result = Email.Create(emailString)

        match result with
        | Ok email -> Assert.Equal(emailString, email.ToString())
        | Error _ -> Assert.True(false, "Expected Ok but got Error")

    [<Fact>]
    let ``Two emails with same value are equal`` () =
        let email1Result = Email.Create("test@example.com")
        let email2Result = Email.Create("test@example.com")

        match email1Result, email2Result with
        | Ok email1, Ok email2 -> Assert.Equal(email1, email2)
        | _ -> Assert.True(false, "Expected both emails to be created successfully")