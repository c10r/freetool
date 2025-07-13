namespace Freetool.Domain.Events

open System
open Freetool.Domain
open Freetool.Domain.ValueObjects

type UserEvent =
    | UserCreated of UserCreatedEvent
    | UserUpdated of UserUpdatedEvent
    | UserDeleted of UserDeletedEvent

and UserCreatedEvent = {
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

and UserUpdatedEvent = {
    UserId: UserId
    Changes: UserChange list
    OccurredAt: DateTime
    EventId: Guid
} with

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId

and UserDeletedEvent = {
    UserId: UserId
    OccurredAt: DateTime
    EventId: Guid
} with

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId

and UserChange =
    | NameChanged of oldValue: string * newValue: string
    | EmailChanged of oldValue: Email * newValue: Email
    | ProfilePicChanged of oldValue: string option * newValue: string option

module UserEvents =
    let userCreated (userId: UserId) (name: string) (email: Email) (profilePicUrl: string option) =
        UserCreated {
            UserId = userId
            Name = name
            Email = email
            ProfilePicUrl = profilePicUrl
            OccurredAt = DateTime.UtcNow
            EventId = Guid.NewGuid()
        }

    let userUpdated (userId: UserId) (changes: UserChange list) =
        UserUpdated {
            UserId = userId
            Changes = changes
            OccurredAt = DateTime.UtcNow
            EventId = Guid.NewGuid()
        }

    let userDeleted (userId: UserId) =
        UserDeleted {
            UserId = userId
            OccurredAt = DateTime.UtcNow
            EventId = Guid.NewGuid()
        }