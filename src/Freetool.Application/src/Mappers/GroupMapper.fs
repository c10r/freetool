namespace Freetool.Application.Mappers

open System
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Application.DTOs

module GroupMapper =
    let fromCreateDto (dto: CreateGroupDto) : UnvalidatedGroup =
        // Convert string UserIds to UserId list, filtering out invalid GUIDs
        let userIds =
            dto.UserIds
            |> Option.defaultValue []
            |> List.choose (fun userIdStr ->
                match Guid.TryParse userIdStr with
                | true, guid -> Some(UserId.FromGuid guid)
                | false, _ -> None)
            |> List.distinct

        {
            State = {
                Id = GroupId.NewId()
                Name = dto.Name
                UserIds = userIds
                CreatedAt = DateTime.UtcNow
                UpdatedAt = DateTime.UtcNow
            }
            UncommittedEvents = []
        }

    let fromUpdateNameDto (dto: UpdateGroupNameDto) (group: ValidatedGroup) : UnvalidatedGroup = {
        State = {
            group.State with
                Name = dto.Name
                UpdatedAt = DateTime.UtcNow
        }
        UncommittedEvents = []
    }

    // Domain -> DTO conversions (for API responses)
    let toDto (group: ValidatedGroup) : GroupDto = {
        Id = group.State.Id.Value.ToString()
        Name = group.State.Name
        UserIds = group.State.UserIds |> List.map (fun userId -> userId.Value.ToString())
        CreatedAt = group.State.CreatedAt
        UpdatedAt = group.State.UpdatedAt
    }

    let toGroupWithUsersDto (group: ValidatedGroup) (users: ValidatedUser list) : GroupWithUsersDto = {
        Id = group.State.Id.Value.ToString()
        Name = group.State.Name
        Users = users |> List.map UserMapper.toDto
        CreatedAt = group.State.CreatedAt
        UpdatedAt = group.State.UpdatedAt
    }

    let toPagedDto (groups: ValidatedGroup list) (totalCount: int) (skip: int) (take: int) : PagedGroupsDto = {
        Groups = groups |> List.map toDto
        TotalCount = totalCount
        Skip = skip
        Take = take
    }