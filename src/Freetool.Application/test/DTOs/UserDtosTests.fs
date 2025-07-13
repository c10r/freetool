namespace Freetool.Application.Tests.DTOs

open System
open Xunit
open Freetool.Application.DTOs

module UserDtosTests =

    [<Fact>]
    let ``CreateUserDto can be created with valid properties`` () =
        let dto = {
            Name = "John Doe"
            Email = "john.doe@example.com"
            ProfilePicUrl = "https://example.com/pic.jpg"
        }

        Assert.Equal("John Doe", dto.Name)
        Assert.Equal("john.doe@example.com", dto.Email)
        Assert.Equal("https://example.com/pic.jpg", dto.ProfilePicUrl)

    [<Fact>]
    let ``CreateUserDto can be created with null ProfilePicUrl`` () =
        let dto = {
            Name = "John Doe"
            Email = "john.doe@example.com"
            ProfilePicUrl = null
        }

        Assert.Equal("John Doe", dto.Name)
        Assert.Equal("john.doe@example.com", dto.Email)
        Assert.Null(dto.ProfilePicUrl)

    [<Fact>]
    let ``UpdateUserNameDto can be created with valid name`` () =
        let dto = { Name = "Jane Smith" }

        Assert.Equal("Jane Smith", dto.Name)

    [<Fact>]
    let ``UpdateUserEmailDto can be created with valid email`` () =
        let dto = { Email = "new.email@example.com" }

        Assert.Equal("new.email@example.com", dto.Email)

    [<Fact>]
    let ``SetProfilePictureDto can be created with valid URL`` () =
        let dto = {
            ProfilePicUrl = "https://example.com/newpic.jpg"
        }

        Assert.Equal("https://example.com/newpic.jpg", dto.ProfilePicUrl)

    [<Fact>]
    let ``UserDto can be created with all properties`` () =
        let createdAt = DateTime.UtcNow.AddDays(-1.0)
        let updatedAt = DateTime.UtcNow

        let dto = {
            Id = "12345678-1234-1234-1234-123456789012"
            Name = "John Doe"
            Email = "john.doe@example.com"
            ProfilePicUrl = "https://example.com/pic.jpg"
            CreatedAt = createdAt
            UpdatedAt = updatedAt
        }

        Assert.Equal("12345678-1234-1234-1234-123456789012", dto.Id)
        Assert.Equal("John Doe", dto.Name)
        Assert.Equal("john.doe@example.com", dto.Email)
        Assert.Equal("https://example.com/pic.jpg", dto.ProfilePicUrl)
        Assert.Equal(createdAt, dto.CreatedAt)
        Assert.Equal(updatedAt, dto.UpdatedAt)

    [<Fact>]
    let ``UserDto can be created with null ProfilePicUrl`` () =
        let dto = {
            Id = "12345678-1234-1234-1234-123456789012"
            Name = "John Doe"
            Email = "john.doe@example.com"
            ProfilePicUrl = null
            CreatedAt = DateTime.UtcNow
            UpdatedAt = DateTime.UtcNow
        }

        Assert.Null(dto.ProfilePicUrl)

    [<Fact>]
    let ``PagedUsersDto can be created with empty user list`` () =
        let dto = {
            Users = []
            TotalCount = 0
            Skip = 0
            Take = 10
        }

        Assert.Empty(dto.Users)
        Assert.Equal(0, dto.TotalCount)
        Assert.Equal(0, dto.Skip)
        Assert.Equal(10, dto.Take)

    [<Fact>]
    let ``PagedUsersDto can be created with user list`` () =
        let user1 = {
            Id = "12345678-1234-1234-1234-123456789012"
            Name = "John Doe"
            Email = "john.doe@example.com"
            ProfilePicUrl = null
            CreatedAt = DateTime.UtcNow
            UpdatedAt = DateTime.UtcNow
        }

        let user2 = {
            Id = "87654321-4321-4321-4321-210987654321"
            Name = "Jane Smith"
            Email = "jane.smith@example.com"
            ProfilePicUrl = "https://example.com/jane.jpg"
            CreatedAt = DateTime.UtcNow
            UpdatedAt = DateTime.UtcNow
        }

        let dto = {
            Users = [ user1; user2 ]
            TotalCount = 2
            Skip = 0
            Take = 10
        }

        Assert.Equal(2, dto.Users.Length)
        Assert.Equal(2, dto.TotalCount)
        Assert.Equal(0, dto.Skip)
        Assert.Equal(10, dto.Take)
        Assert.Contains(user1, dto.Users)
        Assert.Contains(user2, dto.Users)

    [<Fact>]
    let ``PagedUsersDto supports pagination parameters`` () =
        let dto = {
            Users = []
            TotalCount = 100
            Skip = 20
            Take = 10
        }

        Assert.Equal(100, dto.TotalCount)
        Assert.Equal(20, dto.Skip)
        Assert.Equal(10, dto.Take)

    [<Fact>]
    let ``DTOs can be compared for equality`` () =
        let dto1 = { Name = "John Doe" }
        let dto2 = { Name = "John Doe" }
        let dto3 = { Name = "Jane Smith" }

        Assert.Equal(dto1, dto2)
        Assert.NotEqual(dto1, dto3)

    [<Fact>]
    let ``CreateUserDto with different cases are not equal`` () =
        let dto1 = {
            Name = "John Doe"
            Email = "john.doe@example.com"
            ProfilePicUrl = null
        }

        let dto2 = {
            Name = "john doe"
            Email = "john.doe@example.com"
            ProfilePicUrl = null
        }

        Assert.NotEqual(dto1, dto2)