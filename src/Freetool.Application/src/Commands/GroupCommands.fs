namespace Freetool.Application.Commands

open Freetool.Domain.Entities
open Freetool.Application.DTOs

type GroupCommandResult =
    | GroupResult of GroupDto
    | GroupWithUsersResult of GroupWithUsersDto
    | GroupsResult of PagedGroupsDto
    | UnitResult of unit

type GroupCommand =
    | CreateGroup of ValidatedGroup
    | GetGroupById of groupId: string
    | GetGroupByName of name: string
    | GetAllGroups of skip: int * take: int
    | GetGroupsByUserId of userId: string
    | DeleteGroup of groupId: string
    | UpdateGroupName of groupId: string * UpdateGroupNameDto
    | AddUserToGroup of groupId: string * AddUserToGroupDto
    | RemoveUserFromGroup of groupId: string * RemoveUserFromGroupDto