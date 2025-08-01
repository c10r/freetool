namespace Freetool.Domain

open System

type DomainError =
    | ValidationError of string
    | NotFound of string
    | Conflict of string
    | InvalidOperation of string

type IEntity<'TId> =
    abstract member Id: 'TId

type IDomainEvent =
    abstract member OccurredAt: DateTime
    abstract member EventId: Guid

type EventSourcingAggregate<'T> = {
    State: 'T
    UncommittedEvents: IDomainEvent list
}