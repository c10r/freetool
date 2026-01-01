namespace Freetool.Application.DTOs

open System.ComponentModel.DataAnnotations

/// DTO containing all 10 space permissions as boolean fields
type SpacePermissionsDto =
    { CreateResource: bool
      EditResource: bool
      DeleteResource: bool
      CreateApp: bool
      EditApp: bool
      DeleteApp: bool
      RunApp: bool
      CreateFolder: bool
      EditFolder: bool
      DeleteFolder: bool }

/// DTO for a space member with their permissions
type SpaceMemberPermissionsDto =
    { UserId: string
      UserName: string
      UserEmail: string
      ProfilePicUrl: string option
      IsModerator: bool
      IsOrgAdmin: bool
      Permissions: SpacePermissionsDto }

/// Response DTO containing all space members with their permissions
type SpaceMembersPermissionsResponseDto =
    { SpaceId: string
      SpaceName: string
      Members: SpaceMemberPermissionsDto list }

/// DTO for updating a user's permissions in a space
type UpdateUserPermissionsDto =
    { [<Required>]
      UserId: string
      [<Required>]
      Permissions: SpacePermissionsDto }
