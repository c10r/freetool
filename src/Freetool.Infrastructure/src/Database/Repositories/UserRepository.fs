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

type UserRepository(context: FreetoolDbContext, eventRepository: IEventRepository) =

    interface IUserRepository with

        member _.GetByIdAsync(userId: UserId) : Task<ValidatedUser option> = task {
            let guidId = userId.Value
            let! userEntity = context.Users.FirstOrDefaultAsync(fun u -> u.Id = guidId)

            return
                userEntity
                |> Option.ofObj
                |> Option.map (UserEntityMapper.fromEntity >> User.fromData)
        }

        member _.GetByEmailAsync(email: Email) : Task<ValidatedUser option> = task {
            let emailStr = email.Value
            let! userEntity = context.Users.FirstOrDefaultAsync(fun u -> u.Email = emailStr)

            return
                userEntity
                |> Option.ofObj
                |> Option.map (UserEntityMapper.fromEntity >> User.fromData)
        }

        member _.GetAllAsync (skip: int) (take: int) : Task<ValidatedUser list> = task {
            let! userEntities = context.Users.OrderBy(fun u -> u.CreatedAt).Skip(skip).Take(take).ToListAsync()

            return
                userEntities
                |> Seq.map (UserEntityMapper.fromEntity >> User.fromData)
                |> Seq.toList
        }

        member _.AddAsync(user: ValidatedUser) : Task<Result<unit, DomainError>> = task {
            use transaction = context.Database.BeginTransaction()

            try
                // 1. Save user to database
                let userEntity = UserEntityMapper.toEntity user
                context.Users.Add userEntity |> ignore
                let! _ = context.SaveChangesAsync()

                // 2. Save events to audit log in SAME transaction
                let events = User.getUncommittedEvents user

                for event in events do
                    do! eventRepository.SaveEventAsync event

                // 3. Commit everything atomically
                transaction.Commit()
                return Ok()

            with
            | :? DbUpdateException as ex ->
                transaction.Rollback()
                return Error(Conflict $"Failed to add user: {ex.Message}")
            | ex ->
                transaction.Rollback()
                return Error(InvalidOperation $"Transaction failed: {ex.Message}")
        }

        member _.UpdateAsync(user: ValidatedUser) : Task<Result<unit, DomainError>> = task {
            use transaction = context.Database.BeginTransaction()

            try
                let guidId = (User.getId user).Value
                let! existingEntity = context.Users.FirstOrDefaultAsync(fun u -> u.Id = guidId)

                match Option.ofObj existingEntity with
                | None ->
                    transaction.Rollback()
                    return Error(NotFound "User not found")
                | Some entity ->
                    // Update entity properties
                    let userData = user.State
                    entity.Name <- userData.Name
                    entity.Email <- userData.Email
                    entity.ProfilePicUrl <- userData.ProfilePicUrl
                    entity.UpdatedAt <- userData.UpdatedAt

                    let! _ = context.SaveChangesAsync()

                    // Save events to audit log in SAME transaction
                    let events = User.getUncommittedEvents user

                    for event in events do
                        do! eventRepository.SaveEventAsync event

                    transaction.Commit()
                    return Ok()
            with
            | :? DbUpdateException as ex ->
                transaction.Rollback()
                return Error(Conflict $"Failed to update user: {ex.Message}")
            | ex ->
                transaction.Rollback()
                return Error(InvalidOperation $"Update transaction failed: {ex.Message}")
        }

        member _.DeleteAsync (userId: UserId) (deleteEvent: IDomainEvent option) : Task<Result<unit, DomainError>> = task {
            use transaction = context.Database.BeginTransaction()

            try
                let guidId = userId.Value
                let! userEntity = context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(fun u -> u.Id = guidId)

                match Option.ofObj userEntity with
                | None ->
                    transaction.Rollback()
                    return Error(NotFound "User not found")
                | Some entity ->
                    // Soft delete: set IsDeleted flag instead of removing entity
                    entity.IsDeleted <- true
                    entity.UpdatedAt <- System.DateTime.UtcNow
                    let! _ = context.SaveChangesAsync()

                    // Save delete event if provided
                    match deleteEvent with
                    | Some event -> do! eventRepository.SaveEventAsync event
                    | None -> ()

                    transaction.Commit()
                    return Ok()
            with
            | :? DbUpdateException as ex ->
                transaction.Rollback()
                return Error(Conflict $"Failed to delete user: {ex.Message}")
            | ex ->
                transaction.Rollback()
                return Error(InvalidOperation $"Delete transaction failed: {ex.Message}")
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