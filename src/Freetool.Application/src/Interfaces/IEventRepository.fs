namespace Freetool.Application.Interfaces

open System.Threading.Tasks
open Freetool.Domain
open Freetool.Application.DTOs
open Freetool.Domain.Entities

type IEventRepository =
    abstract member SaveEventAsync: event: IDomainEvent -> Task<unit>
    abstract member GetEventsAsync: filter: EventFilter -> Task<PagedResult<EventData>>
