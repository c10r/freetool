namespace Freetool.Application.DTOs

open System
open System.ComponentModel.DataAnnotations

type CreateUserDto = {
    [<Required>]
    [<StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")>]
    Name: string

    [<Required>]
    [<StringLength(254, ErrorMessage = "Email cannot exceed 254 characters")>]
    [<EmailAddress(ErrorMessage = "Invalid email format")>]
    Email: string

    [<StringLength(2000, ErrorMessage = "Profile picture URL cannot exceed 2000 characters")>]
    [<Url(ErrorMessage = "Profile picture URL must be a valid URL")>]
    ProfilePicUrl: string
}

type UpdateUserNameDto = {
    [<Required>]
    [<StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")>]
    Name: string
}

type UpdateUserEmailDto = {
    [<Required>]
    [<StringLength(254, ErrorMessage = "Email cannot exceed 254 characters")>]
    [<EmailAddress(ErrorMessage = "Invalid email format")>]
    Email: string
}

type SetProfilePictureDto = {
    [<Required>]
    [<StringLength(2000, MinimumLength = 1, ErrorMessage = "Profile picture URL must be between 1 and 2000 characters")>]
    [<Url(ErrorMessage = "Profile picture URL must be a valid URL")>]
    ProfilePicUrl: string
}

type UserDto = {
    Id: string
    Name: string
    Email: string
    ProfilePicUrl: string
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

type PagedUsersDto = {
    Users: UserDto list
    TotalCount: int
    Skip: int
    Take: int
}