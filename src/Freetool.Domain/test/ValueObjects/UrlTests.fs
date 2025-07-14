namespace Freetool.Domain.Tests.ValueObjects

open Xunit
open Freetool.Domain
open Freetool.Domain.ValueObjects

module UrlTests =

    [<Fact>]
    let ``Create with valid URL returns Ok`` () =
        let result = Url.Create("https://example.com")

        match result with
        | Ok url -> Assert.Equal("https://example.com", url.Value)
        | Error _ -> Assert.True(false, "Expected Ok but got Error")

    [<Fact>]
    let ``Create with empty URL returns ValidationError`` () =
        let result = Url.Create("")

        match result with
        | Ok _ -> Assert.True(false, "Expected Error but got Ok")
        | Error(ValidationError msg) -> Assert.Equal("URL cannot be empty", msg)
        | Error _ -> Assert.True(false, "Expected ValidationError")

    [<Fact>]
    let ``Create with null URL returns ValidationError`` () =
        let result = Url.Create(null)

        match result with
        | Ok _ -> Assert.True(false, "Expected Error but got Ok")
        | Error(ValidationError msg) -> Assert.Equal("URL cannot be empty", msg)
        | Error _ -> Assert.True(false, "Expected ValidationError")

    [<Fact>]
    let ``Create with whitespace URL returns ValidationError`` () =
        let result = Url.Create("   ")

        match result with
        | Ok _ -> Assert.True(false, "Expected Error but got Ok")
        | Error(ValidationError msg) -> Assert.Equal("URL cannot be empty", msg)
        | Error _ -> Assert.True(false, "Expected ValidationError")

    [<Fact>]
    let ``Create with URL exceeding 2000 characters returns ValidationError`` () =
        let longUrl = "https://example.com/" + String.replicate 2000 "a"
        let result = Url.Create(longUrl)

        match result with
        | Ok _ -> Assert.True(false, "Expected Error but got Ok")
        | Error(ValidationError msg) -> Assert.Equal("URL cannot exceed 2000 characters", msg)
        | Error _ -> Assert.True(false, "Expected ValidationError")

    [<Fact>]
    let ``Create with invalid URL format returns ValidationError`` () =
        let invalidUrls = [
            "invalid-url"
            "ftp://example.com"
            "http://"
            "https://"
            "example.com"
            "http://example"
            "https://."
            "http:// example.com"
            "https://example .com"
        ]

        for invalidUrl in invalidUrls do
            let result = Url.Create(invalidUrl)

            match result with
            | Ok _ -> Assert.True(false, $"Expected Error for '{invalidUrl}' but got Ok")
            | Error(ValidationError msg) -> Assert.Equal("Invalid URL format", msg)
            | Error _ -> Assert.True(false, "Expected ValidationError")

    [<Fact>]
    let ``Create with valid URL formats returns Ok`` () =
        let validUrls = [
            "https://example.com"
            "http://example.com"
            "https://www.example.com"
            "http://www.example.com"
            "https://subdomain.example.com"
            "https://example.co.uk"
            "https://example.com/path"
            "https://example.com/path/to/resource"
            "https://example.com:8080"
            "https://example.com?query=value"
            "https://example.com#fragment"
            "https://example.com/path?query=value&another=param#fragment"
            "https://api.example.com/v1/users/123"
            "https://user:pass@example.com"
            "https://192.168.1.1"
            "https://127.0.0.1:3000"
        ]

        for validUrl in validUrls do
            let result = Url.Create(validUrl)

            match result with
            | Ok url -> Assert.Equal(validUrl, url.Value)
            | Error _ -> Assert.True(false, $"Expected Ok for '{validUrl}' but got Error")

    [<Fact>]
    let ``Value property returns the underlying URL string`` () =
        let urlString = "https://example.com"
        let result = Url.Create(urlString)

        match result with
        | Ok url -> Assert.Equal(urlString, url.Value)
        | Error _ -> Assert.True(false, "Expected Ok but got Error")

    [<Fact>]
    let ``ToString returns the URL string`` () =
        let urlString = "https://example.com"
        let result = Url.Create(urlString)

        match result with
        | Ok url -> Assert.Equal(urlString, url.ToString())
        | Error _ -> Assert.True(false, "Expected Ok but got Error")

    [<Fact>]
    let ``Two URLs with same value are equal`` () =
        let url1Result = Url.Create("https://example.com")
        let url2Result = Url.Create("https://example.com")

        match url1Result, url2Result with
        | Ok url1, Ok url2 -> Assert.Equal(url1, url2)
        | _ -> Assert.True(false, "Expected both URLs to be created successfully")