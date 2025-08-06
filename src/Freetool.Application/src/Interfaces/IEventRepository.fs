namespace Freetool.Application.Interfaces

open System.Threading.Tasks
open Freetool.Domain
open Freetool.Application.DTOs
open Freetool.Domain.Entities

type IEventRepository =
    abstract member SaveEventAsync: event: IDomainEvent -> Task<unit>
    abstract member GetEventsByEntityAsync: entityType: string -> entityId: string -> Task<EventData list>
    abstract member GetAllEventsAsync: skip: int -> take: int -> Task<PagedResult<EventData>>
    abstract member GetEventsByTypeAsync: eventType: string -> skip: int -> take: int -> Task<PagedResult<EventData>>