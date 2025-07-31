namespace Freetool.Api.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Freetool.Application.Interfaces

[<ApiController>]
[<Route("audit")>]
type AuditController(eventRepository: IEventRepository) =
    inherit ControllerBase()

    [<HttpGet("events")>]
    member this.GetAllEvents([<FromQuery>] skip: int, [<FromQuery>] take: int) : Task<IActionResult> = task {
        let skipValue = if skip < 0 then 0 else skip

        let takeValue =
            if take <= 0 then 10
            elif take > 100 then 100
            else take

        let! result = eventRepository.GetAllEventsAsync skipValue takeValue
        return this.Ok(result) :> IActionResult
    }

    [<HttpGet("events/{entityType}/{entityId}")>]
    member this.GetEventsByEntity(entityType: string, entityId: string) : Task<IActionResult> = task {
        let! events = eventRepository.GetEventsByEntityAsync entityType entityId
        return this.Ok(events) :> IActionResult
    }

    [<HttpGet("events/type/{eventType}")>]
    member this.GetEventsByType
        (eventType: string, [<FromQuery>] skip: int, [<FromQuery>] take: int)
        : Task<IActionResult> =
        task {
            let skipValue = if skip < 0 then 0 else skip

            let takeValue =
                if take <= 0 then 10
                elif take > 100 then 100
                else take

            let! result = eventRepository.GetEventsByTypeAsync eventType skipValue takeValue
            return this.Ok(result) :> IActionResult
        }