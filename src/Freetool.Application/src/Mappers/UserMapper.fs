namespace Freetool.Application.Mappers

open System
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Application.DTOs

module UserMapper =
    let fromCreateDto (dto: CreateUserDto) : UnvalidatedUser = {
        State = {
            Id = UserId.NewId()
            Name = dto.Name
            Email = dto.Email
            ProfilePicUrl =
                if String.IsNullOrEmpty(dto.ProfilePicUrl) then
                    None
                else
                    Some(dto.ProfilePicUrl)
            CreatedAt = DateTime.UtcNow
            UpdatedAt = DateTime.UtcNow
        }
        UncommittedEvents = []
    }

    let fromUpdateNameDto (dto: UpdateUserNameDto) (user: ValidatedUser) : UnvalidatedUser = {
        State = {
            user.State with
                Name = dto.Name
                UpdatedAt = DateTime.UtcNow
        }
        UncommittedEvents = []
    }

    let fromUpdateEmailDto (dto: UpdateUserEmailDto) (user: ValidatedUser) : UnvalidatedUser = {
        State = {
            user.State with
                Email = dto.Email
                UpdatedAt = DateTime.UtcNow
        }
        UncommittedEvents = []
    }

    let fromSetProfilePictureDto (dto: SetProfilePictureDto) (user: ValidatedUser) : UnvalidatedUser = {
        State = {
            user.State with
                ProfilePicUrl = Some(dto.ProfilePicUrl)
                UpdatedAt = DateTime.UtcNow
        }
        UncommittedEvents = []
    }

    // Domain -> DTO conversions (for API responses)
    let toDto (user: ValidatedUser) : UserDto = {
        Id = user.State.Id.Value.ToString()
        Name = user.State.Name
        Email = user.State.Email
        ProfilePicUrl = user.State.ProfilePicUrl |> Option.defaultValue ""
        CreatedAt = user.State.CreatedAt
        UpdatedAt = user.State.UpdatedAt
    }

    let toPagedDto (users: ValidatedUser list) (totalCount: int) (skip: int) (take: int) : PagedUsersDto = {
        Users = users |> List.map toDto
        TotalCount = totalCount
        Skip = skip
        Take = take
    }