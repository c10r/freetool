namespace Freetool.Domain.Events

open System
open Freetool.Domain
open Freetool.Domain.ValueObjects

type GroupChange =
    | NameChanged of oldValue: string * newValue: string
    | UserAdded of userId: UserId
    | UserRemoved of userId: UserId

type GroupCreatedEvent = {
    GroupId: GroupId
    Name: string
    InitialUserIds: UserId list
    OccurredAt: DateTime
    EventId: Guid
} with

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId

type GroupUpdatedEvent = {
    GroupId: GroupId
    Changes: GroupChange list
    OccurredAt: DateTime
    EventId: Guid
} with

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId

type GroupDeletedEvent = {
    GroupId: GroupId
    OccurredAt: DateTime
    EventId: Guid
} with

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId

module GroupEvents =
    let groupCreated (groupId: GroupId) (name: string) (initialUserIds: UserId list) =
        {
            GroupId = groupId
            Name = name
            InitialUserIds = initialUserIds
            OccurredAt = DateTime.UtcNow
            EventId = Guid.NewGuid()
        }
        : GroupCreatedEvent

    let groupUpdated (groupId: GroupId) (changes: GroupChange list) =
        {
            GroupId = groupId
            Changes = changes
            OccurredAt = DateTime.UtcNow
            EventId = Guid.NewGuid()
        }
        : GroupUpdatedEvent

    let groupDeleted (groupId: GroupId) =
        {
            GroupId = groupId
            OccurredAt = DateTime.UtcNow
            EventId = Guid.NewGuid()
        }
        : GroupDeletedEvent