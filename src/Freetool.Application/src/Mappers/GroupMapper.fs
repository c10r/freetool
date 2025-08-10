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
                CreatedAt = DateTime.UtcNow
                UpdatedAt = DateTime.UtcNow
                IsDeleted = false
                UserIds = userIds
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