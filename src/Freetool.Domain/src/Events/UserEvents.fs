namespace Freetool.Domain.Events

open System
open Freetool.Domain
open Freetool.Domain.ValueObjects

type UserChange =
    | NameChanged of oldValue: string * newValue: string
    | EmailChanged of oldValue: Email * newValue: Email
    | ProfilePicChanged of oldValue: Url option * newValue: Url option

type UserCreatedEvent =
    { UserId: UserId
      Name: string
      Email: Email
      ProfilePicUrl: Url option
      OccurredAt: DateTime
      EventId: Guid
      ActorUserId: UserId }

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId
        member this.UserId = this.ActorUserId

type UserUpdatedEvent =
    { UserId: UserId
      Changes: UserChange list
      OccurredAt: DateTime
      EventId: Guid
      ActorUserId: UserId }

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId
        member this.UserId = this.ActorUserId

type UserDeletedEvent =
    { UserId: UserId
      OccurredAt: DateTime
      EventId: Guid
      ActorUserId: UserId }

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId
        member this.UserId = this.ActorUserId

module UserEvents =
    let userCreated (actorUserId: UserId) (userId: UserId) (name: string) (email: Email) (profilePicUrl: Url option) =
        { UserId = userId
          Name = name
          Email = email
          ProfilePicUrl = profilePicUrl
          OccurredAt = DateTime.UtcNow
          EventId = Guid.NewGuid()
          ActorUserId = actorUserId }
        : UserCreatedEvent

    let userUpdated (actorUserId: UserId) (userId: UserId) (changes: UserChange list) =
        { UserId = userId
          Changes = changes
          OccurredAt = DateTime.UtcNow
          EventId = Guid.NewGuid()
          ActorUserId = actorUserId }
        : UserUpdatedEvent

    let userDeleted (actorUserId: UserId) (userId: UserId) =
        { UserId = userId
          OccurredAt = DateTime.UtcNow
          EventId = Guid.NewGuid()
          ActorUserId = actorUserId }
        : UserDeletedEvent
