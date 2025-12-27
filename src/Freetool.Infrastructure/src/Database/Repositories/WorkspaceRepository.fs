namespace Freetool.Infrastructure.Database.Repositories

open System.Linq
open System.Threading.Tasks
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Infrastructure.Database

type WorkspaceRepository(context: FreetoolDbContext, eventRepository: IEventRepository) =

    interface IWorkspaceRepository with

        member _.GetByIdAsync(workspaceId: WorkspaceId) : Task<ValidatedWorkspace option> =
            task {
                let! workspaceData = context.Workspaces.FirstOrDefaultAsync(fun w -> w.Id = workspaceId)

                match Option.ofObj workspaceData with
                | None -> return None
                | Some data -> return Some(Workspace.fromData data)
            }

        member _.GetByGroupIdAsync(groupId: GroupId) : Task<ValidatedWorkspace option> =
            task {
                let! workspaceData = context.Workspaces.FirstOrDefaultAsync(fun w -> w.GroupId = groupId)

                match Option.ofObj workspaceData with
                | None -> return None
                | Some data -> return Some(Workspace.fromData data)
            }

        member _.GetAllAsync (skip: int) (take: int) : Task<ValidatedWorkspace list> =
            task {
                let! workspaceDatas =
                    context.Workspaces.OrderBy(fun w -> w.CreatedAt).Skip(skip).Take(take).ToListAsync()

                return workspaceDatas |> Seq.map Workspace.fromData |> Seq.toList
            }

        member _.AddAsync(workspace: ValidatedWorkspace) : Task<Result<unit, DomainError>> =
            task {
                try
                    // 1. Save workspace to database
                    context.Workspaces.Add workspace.State |> ignore
                    let! _ = context.SaveChangesAsync()

                    // 2. Save events to audit log in SAME transaction
                    let events = Workspace.getUncommittedEvents workspace

                    for event in events do
                        do! eventRepository.SaveEventAsync event

                    // 3. Commit everything atomically
                    let! _ = context.SaveChangesAsync()
                    return Ok()

                with
                | :? DbUpdateException as ex -> return Error(Conflict $"Failed to add workspace: {ex.Message}")
                | ex -> return Error(InvalidOperation $"Transaction failed: {ex.Message}")
            }

        member _.UpdateAsync(workspace: ValidatedWorkspace) : Task<Result<unit, DomainError>> =
            task {
                try
                    let workspaceId = Workspace.getId workspace
                    let! existingData = context.Workspaces.FirstOrDefaultAsync(fun w -> w.Id = workspaceId)

                    match Option.ofObj existingData with
                    | None -> return Error(NotFound "Workspace not found")
                    | Some existingEntity ->
                        // Update the already-tracked entity to avoid tracking conflicts
                        context.Entry(existingEntity).CurrentValues.SetValues(workspace.State)
                        let! _ = context.SaveChangesAsync()

                        // Save events to audit log
                        let events = Workspace.getUncommittedEvents workspace

                        for event in events do
                            do! eventRepository.SaveEventAsync event

                        let! _ = context.SaveChangesAsync()
                        return Ok()
                with
                | :? DbUpdateException as ex -> return Error(Conflict $"Failed to update workspace: {ex.Message}")
                | ex -> return Error(InvalidOperation $"Update transaction failed: {ex.Message}")
            }

        member _.DeleteAsync(workspaceWithDeleteEvent: ValidatedWorkspace) : Task<Result<unit, DomainError>> =
            task {
                try
                    // Soft delete: create updated record with IsDeleted flag
                    let updatedData =
                        { workspaceWithDeleteEvent.State with
                            IsDeleted = true
                            UpdatedAt = System.DateTime.UtcNow }

                    context.Workspaces.Update(updatedData) |> ignore

                    // Save all uncommitted events
                    let events = Workspace.getUncommittedEvents workspaceWithDeleteEvent

                    for event in events do
                        do! eventRepository.SaveEventAsync event

                    let! _ = context.SaveChangesAsync()
                    return Ok()
                with
                | :? DbUpdateException as ex -> return Error(Conflict $"Failed to delete workspace: {ex.Message}")
                | ex -> return Error(InvalidOperation $"Delete transaction failed: {ex.Message}")
            }

        member _.ExistsAsync(workspaceId: WorkspaceId) : Task<bool> =
            task { return! context.Workspaces.AnyAsync(fun w -> w.Id = workspaceId) }

        member _.ExistsByGroupIdAsync(groupId: GroupId) : Task<bool> =
            task { return! context.Workspaces.AnyAsync(fun w -> w.GroupId = groupId) }

        member _.GetCountAsync() : Task<int> =
            task { return! context.Workspaces.CountAsync() }
