namespace Freetool.Domain.Events

open System
open Freetool.Domain
open Freetool.Domain.ValueObjects

type UserChange =
    | NameChanged of oldValue: string * newValue: string
    | EmailChanged of oldValue: Email * newValue: Email
    | ProfilePicChanged of oldValue: string option * newValue: string option

type UserCreatedEvent = {
    UserId: UserId
    Name: string
    Email: Email
    ProfilePicUrl: string option
    OccurredAt: DateTime
    EventId: Guid
} with

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId

type UserUpdatedEvent = {
    UserId: UserId
    Changes: UserChange list
    OccurredAt: DateTime
    EventId: Guid
} with

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId

type UserDeletedEvent = {
    UserId: UserId
    OccurredAt: DateTime
    EventId: Guid
} with

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId

module UserEvents =
    let userCreated (userId: UserId) (name: string) (email: Email) (profilePicUrl: string option) =
        {
            UserId = userId
            Name = name
            Email = email
            ProfilePicUrl = profilePicUrl
            OccurredAt = DateTime.UtcNow
            EventId = Guid.NewGuid()
        }
        : UserCreatedEvent

    let userUpdated (userId: UserId) (changes: UserChange list) =
        {
            UserId = userId
            Changes = changes
            OccurredAt = DateTime.UtcNow
            EventId = Guid.NewGuid()
        }
        : UserUpdatedEvent

    let userDeleted (userId: UserId) =
        {
            UserId = userId
            OccurredAt = DateTime.UtcNow
            EventId = Guid.NewGuid()
        }
        : UserDeletedEvent