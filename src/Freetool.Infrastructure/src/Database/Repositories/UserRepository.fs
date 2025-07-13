namespace Freetool.Infrastructure.Database.Repositories

open System.Linq
open System.Threading.Tasks
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Infrastructure.Database

module UserEntityMapper =

    let toEntityUser (domainUser: User) : UserEntity =
        UserEntity(
            Id = domainUser.Id.Value,
            Name = domainUser.Name,
            Email = domainUser.Email.Value,
            ProfilePicUrl = domainUser.ProfilePicUrl,
            CreatedAt = domainUser.CreatedAt,
            UpdatedAt = domainUser.UpdatedAt
        )

    let toDomainUser (entity: UserEntity) : User =
        let email =
            match Email.Create entity.Email with
            | Ok email -> email
            | Error _ -> failwith $"Invalid email in database: {entity.Email}"

        {
            Id = UserId.FromGuid entity.Id
            Name = entity.Name
            Email = email
            ProfilePicUrl = entity.ProfilePicUrl
            CreatedAt = entity.CreatedAt
            UpdatedAt = entity.UpdatedAt
        }

type UserRepository(context: FreetoolDbContext) =

    interface IUserRepository with

        member _.GetByIdAsync(userId: UserId) : Task<User option> = task {
            let guidId = userId.Value
            let! userEntity = context.Users.FirstOrDefaultAsync(fun u -> u.Id = guidId)

            return userEntity |> Option.ofObj |> Option.map UserEntityMapper.toDomainUser
        }

        member _.GetByEmailAsync(email: Email) : Task<User option> = task {
            let emailStr = email.Value
            let! userEntity = context.Users.FirstOrDefaultAsync(fun u -> u.Email = emailStr)

            return userEntity |> Option.ofObj |> Option.map UserEntityMapper.toDomainUser
        }

        member _.GetAllAsync (skip: int) (take: int) : Task<User list> = task {
            let! userEntities = context.Users.OrderBy(fun u -> u.CreatedAt).Skip(skip).Take(take).ToListAsync()

            return userEntities |> Seq.map UserEntityMapper.toDomainUser |> Seq.toList
        }

        member _.AddAsync(user: User) : Task<Result<unit, DomainError>> = task {
            try
                let userEntity = UserEntityMapper.toEntityUser user
                context.Users.Add userEntity |> ignore
                let! _ = context.SaveChangesAsync()
                return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to add user: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.UpdateAsync(user: User) : Task<Result<unit, DomainError>> = task {
            try
                let guidId = user.Id.Value
                let! existingEntity = context.Users.FirstOrDefaultAsync(fun u -> u.Id = guidId)

                match Option.ofObj existingEntity with
                | None -> return Error(NotFound "User not found")
                | Some entity ->
                    entity.Name <- user.Name
                    entity.Email <- user.Email.Value
                    entity.ProfilePicUrl <- user.ProfilePicUrl
                    entity.UpdatedAt <- user.UpdatedAt

                    let! _ = context.SaveChangesAsync()
                    return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to update user: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.DeleteAsync(userId: UserId) : Task<Result<unit, DomainError>> = task {
            try
                let guidId = userId.Value
                let! userEntity = context.Users.FirstOrDefaultAsync(fun u -> u.Id = guidId)

                match Option.ofObj userEntity with
                | None -> return Error(NotFound "User not found")
                | Some entity ->
                    context.Users.Remove entity |> ignore
                    let! _ = context.SaveChangesAsync()
                    return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to delete user: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.ExistsAsync(userId: UserId) : Task<bool> = task {
            let guidId = userId.Value
            return! context.Users.AnyAsync(fun u -> u.Id = guidId)
        }

        member _.ExistsByEmailAsync(email: Email) : Task<bool> = task {
            let emailStr = email.Value
            return! context.Users.AnyAsync(fun u -> u.Email = emailStr)
        }