namespace Freetool.Infrastructure.Database.Mappers

open System.Linq
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Infrastructure.Database

module GroupEntityMapper =
    let fromEntity (entity: GroupEntity) (userGroupEntities: UserGroupEntity list) : ValidatedGroup = {
        State = {
            Id = GroupId.FromGuid(entity.Id)
            Name = entity.Name
            UserIds = userGroupEntities |> List.map (fun ug -> UserId.FromGuid(ug.UserId))
            CreatedAt = entity.CreatedAt
            UpdatedAt = entity.UpdatedAt
        }
        UncommittedEvents = []
    }

    let toEntity (group: Group) : GroupEntity =
        let entity = GroupEntity()
        entity.Id <- group.State.Id.Value
        entity.Name <- group.State.Name
        entity.CreatedAt <- group.State.CreatedAt
        entity.UpdatedAt <- group.State.UpdatedAt
        entity

    let toUserGroupEntities (group: Group) : UserGroupEntity list =
        group.State.UserIds
        |> List.map (fun userId ->
            let entity = UserGroupEntity()
            entity.Id <- System.Guid.NewGuid()
            entity.UserId <- userId.Value
            entity.GroupId <- group.State.Id.Value
            entity.CreatedAt <- System.DateTime.UtcNow
            entity)

    let fromEntityWithoutUsers (entity: GroupEntity) : ValidatedGroup = {
        State = {
            Id = GroupId.FromGuid(entity.Id)
            Name = entity.Name
            UserIds = []
            CreatedAt = entity.CreatedAt
            UpdatedAt = entity.UpdatedAt
        }
        UncommittedEvents = []
    }