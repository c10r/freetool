namespace Freetool.Infrastructure.Database.Mappers

open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Infrastructure.Database

module UserEntityMapper =
    let fromEntity (entity: UserEntity) : UserData = {
        Id = UserId.FromGuid(entity.Id)
        Name = entity.Name
        Email = entity.Email
        ProfilePicUrl = entity.ProfilePicUrl
        CreatedAt = entity.CreatedAt
        UpdatedAt = entity.UpdatedAt
    }

    let toEntity (user: User) : UserEntity =
        let entity = UserEntity()
        entity.Id <- user.State.Id.Value
        entity.Name <- user.State.Name
        entity.Email <- user.State.Email
        entity.ProfilePicUrl <- user.State.ProfilePicUrl
        entity.CreatedAt <- user.State.CreatedAt
        entity.UpdatedAt <- user.State.UpdatedAt
        entity