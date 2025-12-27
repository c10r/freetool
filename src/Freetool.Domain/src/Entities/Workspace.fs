namespace Freetool.Domain.Entities

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open System.Text.Json.Serialization
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events

[<Table("Workspaces")>]
[<Index([| "GroupId" |], IsUnique = true, Name = "IX_Workspaces_GroupId")>]
// CLIMutable for EntityFramework
[<CLIMutable>]
type WorkspaceData =
    { [<Key>]
      Id: WorkspaceId

      [<Required>]
      GroupId: GroupId

      [<Required>]
      [<JsonIgnore>]
      CreatedAt: DateTime

      [<Required>]
      [<JsonIgnore>]
      UpdatedAt: DateTime

      [<JsonIgnore>]
      IsDeleted: bool }

type Workspace = EventSourcingAggregate<WorkspaceData>

module WorkspaceAggregateHelpers =
    let getEntityId (workspace: Workspace) : WorkspaceId = workspace.State.Id

    let implementsIEntity (workspace: Workspace) =
        { new IEntity<WorkspaceId> with
            member _.Id = workspace.State.Id }

// Type aliases for clarity
type UnvalidatedWorkspace = Workspace // From DTOs - potentially unsafe
type ValidatedWorkspace = Workspace // Validated domain model and database data

module Workspace =
    let create (actorUserId: UserId) (groupId: GroupId) : Result<ValidatedWorkspace, DomainError> =
        let workspaceData =
            { Id = WorkspaceId.NewId()
              GroupId = groupId
              CreatedAt = DateTime.UtcNow
              UpdatedAt = DateTime.UtcNow
              IsDeleted = false }

        let workspaceCreatedEvent =
            WorkspaceEvents.workspaceCreated actorUserId workspaceData.Id groupId

        Ok
            { State = workspaceData
              UncommittedEvents = [ workspaceCreatedEvent :> IDomainEvent ] }

    let fromData (workspaceData: WorkspaceData) : ValidatedWorkspace =
        { State = workspaceData
          UncommittedEvents = [] }

    let validate (workspace: UnvalidatedWorkspace) : Result<ValidatedWorkspace, DomainError> =
        Ok
            { State = workspace.State
              UncommittedEvents = workspace.UncommittedEvents }

    let updateGroup
        (actorUserId: UserId)
        (newGroupId: GroupId)
        (workspace: ValidatedWorkspace)
        : Result<ValidatedWorkspace, DomainError> =
        let oldGroupId = workspace.State.GroupId

        if oldGroupId = newGroupId then
            Ok workspace // No change needed
        else
            let updatedWorkspaceData =
                { workspace.State with
                    GroupId = newGroupId
                    UpdatedAt = DateTime.UtcNow }

            let groupChangedEvent =
                WorkspaceEvents.workspaceUpdated
                    actorUserId
                    workspace.State.Id
                    [ WorkspaceChange.GroupChanged(oldGroupId, newGroupId) ]

            Ok
                { State = updatedWorkspaceData
                  UncommittedEvents = workspace.UncommittedEvents @ [ groupChangedEvent :> IDomainEvent ] }

    let markForDeletion (actorUserId: UserId) (workspace: ValidatedWorkspace) : ValidatedWorkspace =
        let workspaceDeletedEvent =
            WorkspaceEvents.workspaceDeleted actorUserId workspace.State.Id

        { workspace with
            UncommittedEvents = workspace.UncommittedEvents @ [ workspaceDeletedEvent :> IDomainEvent ] }

    let getUncommittedEvents (workspace: ValidatedWorkspace) : IDomainEvent list = workspace.UncommittedEvents

    let markEventsAsCommitted (workspace: ValidatedWorkspace) : ValidatedWorkspace =
        { workspace with
            UncommittedEvents = [] }

    let getId (workspace: Workspace) : WorkspaceId = workspace.State.Id

    let getGroupId (workspace: Workspace) : GroupId = workspace.State.GroupId

    let getCreatedAt (workspace: Workspace) : DateTime = workspace.State.CreatedAt

    let getUpdatedAt (workspace: Workspace) : DateTime = workspace.State.UpdatedAt
