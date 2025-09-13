namespace Freetool.Application.DTOs

open System.ComponentModel.DataAnnotations

type KeyValuePairDto =
    {
      // Intentionally not moved to SharedDtos yet - this is only the first usage
      [<Required>]
      [<StringLength(100, MinimumLength = 1, ErrorMessage = "Key must be between 1 and 100 characters")>]
      Key: string

      [<Required>]
      [<StringLength(ValidationConstants.InputValueMaxLength, ErrorMessage = ValidationConstants.InputValueErrorMessage)>]
      Value: string }

type CreateResourceDto =
    { [<Required>]
      [<StringLength(ValidationConstants.NameMaxLength,
                     MinimumLength = ValidationConstants.NameMinLength,
                     ErrorMessage = ValidationConstants.NameErrorMessage)>]
      Name: string

      [<Required>]
      [<StringLength(ValidationConstants.DescriptionMaxLength,
                     MinimumLength = ValidationConstants.DescriptionMinLength,
                     ErrorMessage = ValidationConstants.DescriptionErrorMessage)>]
      Description: string

      [<Required>]
      [<StringLength(ValidationConstants.InputValueMaxLength,
                     MinimumLength = ValidationConstants.InputValueMinLength,
                     ErrorMessage = ValidationConstants.InputValueErrorMessage)>]
      [<Url(ErrorMessage = "Base URL must be a valid URL")>]
      BaseUrl: string

      // Intentionally not moved to SharedDtos yet - this is only the first usage
      [<Required>]
      [<StringLength(10, MinimumLength = 1, ErrorMessage = "HTTP method must be between 1 and 10 characters")>]
      HttpMethod: string

      UrlParameters: KeyValuePairDto list

      Headers: KeyValuePairDto list

      Body: KeyValuePairDto list }

type UpdateResourceNameDto =
    { [<Required>]
      [<StringLength(ValidationConstants.NameMaxLength,
                     MinimumLength = ValidationConstants.NameMinLength,
                     ErrorMessage = ValidationConstants.NameErrorMessage)>]
      Name: string }

type UpdateResourceDescriptionDto =
    { [<Required>]
      [<StringLength(ValidationConstants.DescriptionMaxLength,
                     MinimumLength = ValidationConstants.DescriptionMinLength,
                     ErrorMessage = ValidationConstants.DescriptionErrorMessage)>]
      Description: string }

type UpdateResourceBaseUrlDto =
    { [<Required>]
      [<StringLength(ValidationConstants.InputValueMaxLength,
                     MinimumLength = ValidationConstants.InputValueMinLength,
                     ErrorMessage = ValidationConstants.InputValueErrorMessage)>]
      [<Url(ErrorMessage = "Base URL must be a valid URL")>]
      BaseUrl: string }

type UpdateResourceUrlParametersDto = { UrlParameters: KeyValuePairDto list }

type UpdateResourceHeadersDto = { Headers: KeyValuePairDto list }

type UpdateResourceBodyDto = { Body: KeyValuePairDto list }
