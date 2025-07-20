namespace Freetool.Infrastructure.Database.Mappers

open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Infrastructure.Database

module UserEntityMapper =
    // Entity -> Domain conversions (database data is trusted, so directly to ValidatedUser)
    let fromEntity (entity: UserEntity) : ValidatedUser =
        User {
            Id = UserId.FromGuid(entity.Id)
            Name = entity.Name
            Email = entity.Email
            ProfilePicUrl = entity.ProfilePicUrl
            CreatedAt = entity.CreatedAt
            UpdatedAt = entity.UpdatedAt
        }

    // Domain -> Entity conversions (can convert from any validation state)
    let toEntity (User userData: User<'State>) : UserEntity =
        let entity = UserEntity()
        entity.Id <- userData.Id.Value
        entity.Name <- userData.Name
        entity.Email <- userData.Email
        entity.ProfilePicUrl <- userData.ProfilePicUrl
        entity.CreatedAt <- userData.CreatedAt
        entity.UpdatedAt <- userData.UpdatedAt
        entity