namespace Freetool.Api.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Application.DTOs

[<ApiController>]
[<Route("audit")>]
type AuditController(eventRepository: IEventRepository) =
    inherit ControllerBase()

    [<HttpGet("events")>]
    [<ProducesResponseType(typeof<PagedResult<EventData>>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member this.GetAllEvents([<FromQuery>] filterDto: EventFilterDTO) : Task<IActionResult> =
        task {
            match EventFilterValidator.validate filterDto with
            | Ok filter ->
                let! result = eventRepository.GetEventsAsync filter
                return this.Ok(result) :> IActionResult
            | Error errors -> return this.BadRequest errors :> IActionResult
        }
