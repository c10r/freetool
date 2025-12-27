namespace Freetool.Application.Handlers

open System
open System.Threading.Tasks
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Application.Commands
open Freetool.Application.DTOs

module WorkspaceHandler =

    let handleCommand
        (workspaceRepository: IWorkspaceRepository)
        (groupRepository: IGroupRepository)
        (command: WorkspaceCommand)
        : Task<Result<WorkspaceCommandResult, DomainError>> =
        task {
            match command with
            | CreateWorkspace(actorUserId, validatedWorkspace) ->
                let groupId = Workspace.getGroupId validatedWorkspace

                // Check if group exists
                let! groupOption = groupRepository.GetByIdAsync groupId

                match groupOption with
                | None -> return Error(NotFound "Group not found")
                | Some _ ->
                    // Check if workspace already exists for this group
                    let! existsByGroupId = workspaceRepository.ExistsByGroupIdAsync groupId

                    if existsByGroupId then
                        return Error(Conflict "A workspace for this group already exists")
                    else
                        // Create event-aware workspace
                        match Workspace.create actorUserId groupId with
                        | Error error -> return Error error
                        | Ok eventAwareWorkspace ->
                            // Save workspace and events atomically
                            match! workspaceRepository.AddAsync eventAwareWorkspace with
                            | Error error -> return Error error
                            | Ok() -> return Ok(WorkspaceResult(eventAwareWorkspace.State))

            | DeleteWorkspace(actorUserId, workspaceId) ->
                match Guid.TryParse workspaceId with
                | false, _ -> return Error(ValidationError "Invalid workspace ID format")
                | true, guid ->
                    let workspaceIdObj = WorkspaceId.FromGuid guid
                    let! workspaceOption = workspaceRepository.GetByIdAsync workspaceIdObj

                    match workspaceOption with
                    | None -> return Error(NotFound "Workspace not found")
                    | Some workspace ->
                        // Mark workspace for deletion to create the delete event
                        let workspaceWithDeleteEvent = Workspace.markForDeletion actorUserId workspace

                        // Delete workspace and save event atomically
                        match! workspaceRepository.DeleteAsync workspaceWithDeleteEvent with
                        | Error error -> return Error error
                        | Ok() -> return Ok(UnitResult())

            | UpdateWorkspaceGroup(actorUserId, workspaceId, dto) ->
                match Guid.TryParse workspaceId, Guid.TryParse dto.GroupId with
                | (false, _), _ -> return Error(ValidationError "Invalid workspace ID format")
                | _, (false, _) -> return Error(ValidationError "Invalid group ID format")
                | (true, workspaceGuid), (true, groupGuid) ->
                    let workspaceIdObj = WorkspaceId.FromGuid workspaceGuid
                    let groupIdObj = GroupId.FromGuid groupGuid

                    let! workspaceOption = workspaceRepository.GetByIdAsync workspaceIdObj

                    match workspaceOption with
                    | None -> return Error(NotFound "Workspace not found")
                    | Some workspace ->
                        // Check if new group already has a workspace (but allow same group for idempotency)
                        if Workspace.getGroupId workspace <> groupIdObj then
                            // Check if group exists
                            let! groupOption = groupRepository.GetByIdAsync groupIdObj

                            match groupOption with
                            | None -> return Error(NotFound "Group not found")
                            | Some _ ->
                                let! existsByGroupId = workspaceRepository.ExistsByGroupIdAsync groupIdObj

                                if existsByGroupId then
                                    return Error(Conflict "A workspace for this group already exists")
                                else
                                    // Update group using domain method (automatically creates event)
                                    match Workspace.updateGroup actorUserId groupIdObj workspace with
                                    | Error error -> return Error error
                                    | Ok updatedWorkspace ->
                                        // Save workspace and events atomically
                                        match! workspaceRepository.UpdateAsync updatedWorkspace with
                                        | Error error -> return Error error
                                        | Ok() -> return Ok(WorkspaceResult(updatedWorkspace.State))
                        else
                            return Ok(WorkspaceResult(workspace.State))

            | GetWorkspaceById workspaceId ->
                match Guid.TryParse workspaceId with
                | false, _ -> return Error(ValidationError "Invalid workspace ID format")
                | true, guid ->
                    let workspaceIdObj = WorkspaceId.FromGuid guid
                    let! workspaceOption = workspaceRepository.GetByIdAsync workspaceIdObj

                    match workspaceOption with
                    | None -> return Error(NotFound "Workspace not found")
                    | Some workspace -> return Ok(WorkspaceResult(workspace.State))

            | GetWorkspaceByGroupId groupId ->
                match Guid.TryParse groupId with
                | false, _ -> return Error(ValidationError "Invalid group ID format")
                | true, guid ->
                    let groupIdObj = GroupId.FromGuid guid
                    let! workspaceOption = workspaceRepository.GetByGroupIdAsync groupIdObj

                    match workspaceOption with
                    | None -> return Error(NotFound "Workspace not found")
                    | Some workspace -> return Ok(WorkspaceResult(workspace.State))

            | GetAllWorkspaces(skip, take) ->
                if skip < 0 then
                    return Error(ValidationError "Skip cannot be negative")
                elif take <= 0 || take > 100 then
                    return Error(ValidationError "Take must be between 1 and 100")
                else
                    let! workspaces = workspaceRepository.GetAllAsync skip take
                    let! totalCount = workspaceRepository.GetCountAsync()

                    let result =
                        { Items = workspaces |> List.map (fun workspace -> workspace.State)
                          TotalCount = totalCount
                          Skip = skip
                          Take = take }

                    return Ok(WorkspacesResult result)
        }

type WorkspaceHandler(workspaceRepository: IWorkspaceRepository, groupRepository: IGroupRepository) =
    interface IMultiRepositoryCommandHandler<WorkspaceCommand, WorkspaceCommandResult> with
        member this.HandleCommand command =
            WorkspaceHandler.handleCommand workspaceRepository groupRepository command
