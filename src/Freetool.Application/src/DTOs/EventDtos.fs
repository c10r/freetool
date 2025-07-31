namespace Freetool.Application.DTOs

open System

type EventDto = {
    Id: string
    EventId: string
    EventType: string
    EntityType: string
    EntityId: string
    EventData: string
    OccurredAt: DateTime
    CreatedAt: DateTime
}

type PagedEventsDto = {
    Events: EventDto list
    TotalCount: int
    Skip: int
    Take: int
}