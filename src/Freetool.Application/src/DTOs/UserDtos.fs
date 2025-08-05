namespace Freetool.Application.DTOs

open System
open System.ComponentModel.DataAnnotations
open System.Text.Json.Serialization

type CreateUserDto = {
    [<Required>]
    [<StringLength(ValidationConstants.NameMaxLength,
                   MinimumLength = ValidationConstants.NameMinLength,
                   ErrorMessage = ValidationConstants.NameErrorMessage)>]
    Name: string

    [<Required>]
    [<StringLength(ValidationConstants.EmailMaxLength, ErrorMessage = ValidationConstants.EmailErrorMessage)>]
    [<EmailAddress(ErrorMessage = "Invalid email format")>]
    Email: string

    [<StringLength(ValidationConstants.URLMaxLength, ErrorMessage = ValidationConstants.URLErrorMessage)>]
    [<Url(ErrorMessage = "Profile picture URL must be a valid URL")>]
    [<JsonConverter(typeof<StringOptionConverter>)>]
    ProfilePicUrl: string option
}

type UpdateUserNameDto = {
    [<Required>]
    [<StringLength(ValidationConstants.NameMaxLength,
                   MinimumLength = ValidationConstants.NameMinLength,
                   ErrorMessage = ValidationConstants.NameErrorMessage)>]
    Name: string
}

type UpdateUserEmailDto = {
    [<Required>]
    [<StringLength(ValidationConstants.EmailMaxLength, ErrorMessage = ValidationConstants.EmailErrorMessage)>]
    [<EmailAddress(ErrorMessage = "Invalid email format")>]
    Email: string
}

type SetProfilePictureDto = {
    [<Required>]
    [<StringLength(ValidationConstants.URLMaxLength,
                   MinimumLength = ValidationConstants.URLMinLength,
                   ErrorMessage = ValidationConstants.URLErrorMessage)>]
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