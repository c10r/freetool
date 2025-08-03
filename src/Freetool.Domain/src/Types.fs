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

type ExecutableHttpRequest = {
    BaseUrl: string
    UrlParameters: (string * string) list
    Headers: (string * string) list
    Body: (string * string) list
    HttpMethod: string
}