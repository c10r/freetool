namespace Freetool.Infrastructure.Database.Repositories

open System.Linq
open System.Threading.Tasks
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Infrastructure.Database
open Freetool.Infrastructure.Database.Mappers

type UserRepository(context: FreetoolDbContext) =

    interface IUserRepository with

        member _.GetByIdAsync(userId: UserId) : Task<ValidatedUser option> = task {
            let guidId = userId.Value
            let! userEntity = context.Users.FirstOrDefaultAsync(fun u -> u.Id = guidId)

            return userEntity |> Option.ofObj |> Option.map UserEntityMapper.fromEntity
        }

        member _.GetByEmailAsync(email: Email) : Task<ValidatedUser option> = task {
            let emailStr = email.Value
            let! userEntity = context.Users.FirstOrDefaultAsync(fun u -> u.Email = emailStr)

            return userEntity |> Option.ofObj |> Option.map UserEntityMapper.fromEntity
        }

        member _.GetAllAsync (skip: int) (take: int) : Task<ValidatedUser list> = task {
            let! userEntities = context.Users.OrderBy(fun u -> u.CreatedAt).Skip(skip).Take(take).ToListAsync()

            return userEntities |> Seq.map UserEntityMapper.fromEntity |> Seq.toList
        }

        member _.AddAsync(user: ValidatedUser) : Task<Result<unit, DomainError>> = task {
            try
                let userEntity = UserEntityMapper.toEntity user
                context.Users.Add userEntity |> ignore
                let! _ = context.SaveChangesAsync()
                return Ok()
            with
            | :? DbUpdateException as ex -> return Error(Conflict $"Failed to add user: {ex.Message}")
            | ex -> return Error(InvalidOperation $"Database error: {ex.Message}")
        }

        member _.UpdateAsync(user: ValidatedUser) : Task<Result<unit, DomainError>> = task {
            try
                let guidId = (User.getId user).Value
                let! existingEntity = context.Users.FirstOrDefaultAsync(fun u -> u.Id = guidId)

                match Option.ofObj existingEntity with
                | None -> return Error(NotFound "User not found")
                | Some entity ->
                    let (User userData) = user
                    entity.Name <- userData.Name
                    entity.Email <- userData.Email
                    entity.ProfilePicUrl <- userData.ProfilePicUrl
                    entity.UpdatedAt <- userData.UpdatedAt

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

        member _.GetCountAsync() : Task<int> = task { return! context.Users.CountAsync() }