namespace Freetool.Application.DTOs

open System
open System.ComponentModel.DataAnnotations

type CreateGroupDto = {
    [<Required>]
    [<StringLength(ValidationConstants.NameMaxLength,
                   MinimumLength = ValidationConstants.NameMinLength,
                   ErrorMessage = ValidationConstants.NameErrorMessage)>]
    Name: string
    UserIds: string list option
}

type UpdateGroupNameDto = {
    [<Required>]
    [<StringLength(ValidationConstants.NameMaxLength,
                   MinimumLength = ValidationConstants.NameMinLength,
                   ErrorMessage = ValidationConstants.NameErrorMessage)>]
    Name: string
}

type AddUserToGroupDto = {
    [<Required>]
    UserId: string
}

type RemoveUserFromGroupDto = {
    [<Required>]
    UserId: string
}

type GroupDto = {
    Id: string
    Name: string
    UserIds: string list
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

type GroupWithUsersDto = {
    Id: string
    Name: string
    Users: UserDto list
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

type PagedGroupsDto = {
    Groups: GroupDto list
    TotalCount: int
    Skip: int
    Take: int
}