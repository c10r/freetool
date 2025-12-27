namespace Freetool.Application.Commands

open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Application.DTOs

type WorkspaceCommandResult =
    | WorkspaceResult of WorkspaceData
    | WorkspacesResult of PagedResult<WorkspaceData>
    | UnitResult of unit

type WorkspaceCommand =
    | CreateWorkspace of actorUserId: UserId * ValidatedWorkspace
    | GetWorkspaceById of workspaceId: string
    | GetWorkspaceByGroupId of groupId: string
    | GetAllWorkspaces of skip: int * take: int
    | DeleteWorkspace of actorUserId: UserId * workspaceId: string
    | UpdateWorkspaceGroup of actorUserId: UserId * workspaceId: string * UpdateWorkspaceGroupDto
