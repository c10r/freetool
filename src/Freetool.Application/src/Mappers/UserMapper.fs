namespace Freetool.Application.Mappers

open System
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Application.DTOs

module UserMapper =
    let fromCreateDto (dto: CreateUserDto) : UnvalidatedUser =
        User {
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

    let fromUpdateNameDto (dto: UpdateUserNameDto) (User userData: ValidatedUser) : UnvalidatedUser =
        User {
            userData with
                Name = dto.Name
                UpdatedAt = DateTime.UtcNow
        }

    let fromUpdateEmailDto (dto: UpdateUserEmailDto) (User userData: ValidatedUser) : UnvalidatedUser =
        User {
            userData with
                Email = dto.Email
                UpdatedAt = DateTime.UtcNow
        }

    let fromSetProfilePictureDto (dto: SetProfilePictureDto) (User userData: ValidatedUser) : UnvalidatedUser =
        User {
            userData with
                ProfilePicUrl = Some(dto.ProfilePicUrl)
                UpdatedAt = DateTime.UtcNow
        }

    // Domain -> DTO conversions (for API responses)
    let toDto (User userData: ValidatedUser) : UserDto = {
        Id = userData.Id.Value.ToString()
        Name = userData.Name
        Email = userData.Email
        ProfilePicUrl = userData.ProfilePicUrl |> Option.defaultValue ""
        CreatedAt = userData.CreatedAt
        UpdatedAt = userData.UpdatedAt
    }

    let toPagedDto (users: ValidatedUser list) (totalCount: int) (skip: int) (take: int) : PagedUsersDto = {
        Users = users |> List.map toDto
        TotalCount = totalCount
        Skip = skip
        Take = take
    }