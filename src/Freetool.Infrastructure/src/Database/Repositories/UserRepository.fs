namespace Freetool.Infrastructure.Database.Repositories

open System.Linq
open System.Threading.Tasks
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Infrastructure.Database

type UserRepository(context: FreetoolDbContext, eventRepository: IEventRepository) =

    interface IUserRepository with

        member _.GetByIdAsync(userId: UserId) : Task<ValidatedUser option> = task {
            let guidId = userId.Value
            let! userData = context.Users.FirstOrDefaultAsync(fun u -> u.Id.Value = guidId)

            return userData |> Option.ofObj |> Option.map (fun data -> User.fromData data)
        }

        member _.GetByEmailAsync(email: Email) : Task<ValidatedUser option> = task {
            let emailStr = email.Value
            let! userData = context.Users.FirstOrDefaultAsync(fun u -> u.Email = emailStr)

            return userData |> Option.ofObj |> Option.map (fun data -> User.fromData data)
        }

        member _.GetAllAsync (skip: int) (take: int) : Task<ValidatedUser list> = task {
            let! userDatas = context.Users.OrderBy(fun u -> u.CreatedAt).Skip(skip).Take(take).ToListAsync()

            return userDatas |> Seq.map (fun data -> User.fromData data) |> Seq.toList
        }

        member _.AddAsync(user: ValidatedUser) : Task<Result<unit, DomainError>> = task {
            use transaction = context.Database.BeginTransaction()

            try
                // 1. Save user to database
                context.Users.Add user.State |> ignore
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
                let! existingUserData = context.Users.FirstOrDefaultAsync(fun u -> u.Id.Value = guidId)

                match Option.ofObj existingUserData with
                | None ->
                    transaction.Rollback()
                    return Error(NotFound "User not found")
                | Some _ ->
                    // Update the entity directly
                    context.Users.Update(user.State) |> ignore
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

                let! userData =
                    context.Users
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(fun u -> u.Id.Value = guidId)

                match Option.ofObj userData with
                | None ->
                    transaction.Rollback()
                    return Error(NotFound "User not found")
                | Some data ->
                    // Soft delete: create updated record with IsDeleted flag
                    let updatedData = {
                        data with
                            IsDeleted = true
                            UpdatedAt = System.DateTime.UtcNow
                    }

                    context.Users.Update(updatedData) |> ignore
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
            return! context.Users.AnyAsync(fun u -> u.Id.Value = guidId)
        }

        member _.ExistsByEmailAsync(email: Email) : Task<bool> = task {
            let emailStr = email.Value
            return! context.Users.AnyAsync(fun u -> u.Email = emailStr)
        }

        member _.GetCountAsync() : Task<int> = task { return! context.Users.CountAsync() }