namespace Freetool.Domain.Events

open System
open Freetool.Domain
open Freetool.Domain.ValueObjects

type WorkspaceChange = GroupChanged of oldValue: GroupId * newValue: GroupId

type WorkspaceCreatedEvent =
    { WorkspaceId: WorkspaceId
      GroupId: GroupId
      OccurredAt: DateTime
      EventId: Guid
      ActorUserId: UserId }

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId
        member this.UserId = this.ActorUserId

type WorkspaceUpdatedEvent =
    { WorkspaceId: WorkspaceId
      Changes: WorkspaceChange list
      OccurredAt: DateTime
      EventId: Guid
      ActorUserId: UserId }

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId
        member this.UserId = this.ActorUserId

type WorkspaceDeletedEvent =
    { WorkspaceId: WorkspaceId
      OccurredAt: DateTime
      EventId: Guid
      ActorUserId: UserId }

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId
        member this.UserId = this.ActorUserId

module WorkspaceEvents =
    let workspaceCreated (actorUserId: UserId) (workspaceId: WorkspaceId) (groupId: GroupId) =
        { WorkspaceId = workspaceId
          GroupId = groupId
          OccurredAt = DateTime.UtcNow
          EventId = Guid.NewGuid()
          ActorUserId = actorUserId }
        : WorkspaceCreatedEvent

    let workspaceUpdated (actorUserId: UserId) (workspaceId: WorkspaceId) (changes: WorkspaceChange list) =
        { WorkspaceId = workspaceId
          Changes = changes
          OccurredAt = DateTime.UtcNow
          EventId = Guid.NewGuid()
          ActorUserId = actorUserId }
        : WorkspaceUpdatedEvent

    let workspaceDeleted (actorUserId: UserId) (workspaceId: WorkspaceId) =
        { WorkspaceId = workspaceId
          OccurredAt = DateTime.UtcNow
          EventId = Guid.NewGuid()
          ActorUserId = actorUserId }
        : WorkspaceDeletedEvent
