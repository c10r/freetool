namespace Freetool.Application.DTOs

open System.ComponentModel.DataAnnotations

type CreateWorkspaceDto =
    { [<Required>]
      GroupId: string }

type UpdateWorkspaceGroupDto =
    { [<Required>]
      GroupId: string }
