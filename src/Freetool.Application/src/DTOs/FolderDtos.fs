namespace Freetool.Application.DTOs

open System.ComponentModel
open System.ComponentModel.DataAnnotations
open System.Text.Json.Serialization

type CreateFolderDto =
    { [<Required>]
      [<StringLength(ValidationConstants.NameMaxLength,
                     MinimumLength = ValidationConstants.NameMinLength,
                     ErrorMessage = ValidationConstants.NameErrorMessage)>]
      Name: string

      [<JsonConverter(typeof<FolderLocationConverter>)>]
      [<Description("Parent folder ID. Leave null to create a root folder.")>]
      Location: FolderLocation

      [<Required>]
      WorkspaceId: string }

type UpdateFolderNameDto =
    { [<Required>]
      [<StringLength(ValidationConstants.NameMaxLength,
                     MinimumLength = ValidationConstants.NameMinLength,
                     ErrorMessage = ValidationConstants.NameErrorMessage)>]
      Name: string }

type MoveFolderDto =
    { [<JsonConverter(typeof<FolderLocationConverter>)>]
      [<Description("Parent folder ID. Leave null or empty to move to the root folder.")>]
      ParentId: FolderLocation }
