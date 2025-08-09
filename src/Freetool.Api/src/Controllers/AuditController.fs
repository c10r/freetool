namespace Freetool.Api.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Freetool.Application.Interfaces
open Freetool.Application.DTOs

[<ApiController>]
[<Route("audit")>]
type AuditController(eventRepository: IEventRepository) =
    inherit ControllerBase()

    [<HttpGet("events")>]
    member this.GetAllEvents([<FromQuery>] filterDto: EventFilterDTO) : Task<IActionResult> = task {
        match EventFilterValidator.validate filterDto with
        | Ok filter ->
            let! result = eventRepository.GetEventsAsync filter
            return this.Ok(result) :> IActionResult
        | Error errors -> return this.BadRequest errors :> IActionResult
    }