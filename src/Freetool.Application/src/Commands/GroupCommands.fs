namespace Freetool.Application.Commands

open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Application.DTOs

type GroupCommandResult =
    | GroupResult of GroupData
    | GroupsResult of PagedResult<GroupData>
    | UnitResult of unit

type GroupCommand =
    | CreateGroup of actorUserId: UserId * ValidatedGroup
    | GetGroupById of groupId: string
    | GetGroupByName of name: string
    | GetAllGroups of skip: int * take: int
    | GetGroupsByUserId of userId: string
    | DeleteGroup of actorUserId: UserId * groupId: string
    | UpdateGroupName of actorUserId: UserId * groupId: string * UpdateGroupNameDto
    | AddUserToGroup of actorUserId: UserId * groupId: string * AddUserToGroupDto
    | RemoveUserFromGroup of actorUserId: UserId * groupId: string * RemoveUserFromGroupDto