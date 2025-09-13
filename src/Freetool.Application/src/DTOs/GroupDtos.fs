namespace Freetool.Application.DTOs

open System.ComponentModel.DataAnnotations

type CreateGroupDto =
    { [<Required>]
      [<StringLength(ValidationConstants.NameMaxLength,
                     MinimumLength = ValidationConstants.NameMinLength,
                     ErrorMessage = ValidationConstants.NameErrorMessage)>]
      Name: string
      UserIds: string list option }

type UpdateGroupNameDto =
    { [<Required>]
      [<StringLength(ValidationConstants.NameMaxLength,
                     MinimumLength = ValidationConstants.NameMinLength,
                     ErrorMessage = ValidationConstants.NameErrorMessage)>]
      Name: string }

type AddUserToGroupDto =
    { [<Required>]
      UserId: string }

type RemoveUserFromGroupDto =
    { [<Required>]
      UserId: string }
