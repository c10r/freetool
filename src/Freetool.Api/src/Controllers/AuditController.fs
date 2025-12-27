namespace Freetool.Api.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Freetool.Application.Interfaces
open Freetool.Application.DTOs
open Freetool.Application.Services

[<ApiController>]
[<Route("audit")>]
type AuditController(eventRepository: IEventRepository, eventEnhancementService: IEventEnhancementService) =
    inherit ControllerBase()

    [<HttpGet("events")>]
    [<ProducesResponseType(typeof<PagedResult<EnhancedEventData>>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    member this.GetAllEvents([<FromQuery>] filterDto: EventFilterDTO) : Task<IActionResult> =
        task {
            match EventFilterValidator.validate filterDto with
            | Ok filter ->
                let! result = eventRepository.GetEventsAsync filter

                // Enhance each event with human-readable information
                let enhancementTasks =
                    result.Items
                    |> List.map (fun event -> eventEnhancementService.EnhanceEventAsync event)
                    |> List.toArray

                let! enhancedItemsArray = Task.WhenAll(enhancementTasks)
                let enhancedItems = enhancedItemsArray |> Array.toList

                let enhancedResult =
                    { Items = enhancedItems
                      TotalCount = result.TotalCount
                      Skip = result.Skip
                      Take = result.Take }

                return this.Ok(enhancedResult) :> IActionResult
            | Error errors -> return this.BadRequest errors :> IActionResult
        }
