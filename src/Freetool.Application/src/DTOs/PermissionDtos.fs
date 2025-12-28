namespace Freetool.Application.DTOs

open System.Text.Json.Serialization

/// Represents the 10 workspace-level permissions
type WorkspacePermissionsData =
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

/// Response DTO for workspace permissions endpoint
type WorkspacePermissionsDto =
    { WorkspaceId: string
      UserId: string
      Permissions: WorkspacePermissionsData
      IsOrgAdmin: bool
      IsTeamAdmin: bool

      [<JsonConverter(typeof<StringOptionConverter>)>]
      TeamId: string option }
