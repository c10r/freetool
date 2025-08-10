namespace Freetool.Api.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Application.DTOs
open Freetool.Application.Commands
open Freetool.Application.Interfaces
open Freetool.Application.Mappers

[<ApiController>]
[<Route("resource")>]
type ResourceController(commandHandler: IMultiRepositoryCommandHandler<ResourceCommand, ResourceCommandResult>) =
    inherit AuthenticatedControllerBase()

    [<HttpPost>]
    [<ProducesResponseType(typeof<ResourceData>, StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.CreateResource([<FromBody>] createDto: CreateResourceDto) : Task<IActionResult> = task {
        let userId = this.CurrentUserId

        match ResourceMapper.fromCreateDto userId createDto with
        | Error domainError -> return this.HandleDomainError(domainError)
        | Ok validatedResource ->
            let! result = commandHandler.HandleCommand(CreateResource(userId, validatedResource))

            return
                match result with
                | Ok(ResourceResult resourceDto) ->
                    this.CreatedAtAction(nameof this.GetResourceById, {| id = resourceDto.Id |}, resourceDto)
                    :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
    }

    [<HttpGet("{id}")>]
    [<ProducesResponseType(typeof<ResourceData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.GetResourceById(id: string) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand(GetResourceById id)

        return
            match result with
            | Ok(ResourceResult resourceDto) -> this.Ok(resourceDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpGet>]
    [<ProducesResponseType(typeof<PagedResult<ResourceData>>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.GetResources([<FromQuery>] skip: int, [<FromQuery>] take: int) : Task<IActionResult> = task {
        let skipValue = if skip < 0 then 0 else skip

        let takeValue =
            if take <= 0 then 10
            elif take > 100 then 100
            else take

        let! result = commandHandler.HandleCommand(GetAllResources(skipValue, takeValue))

        return
            match result with
            | Ok(ResourcesResult pagedResources) -> this.Ok(pagedResources) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpPut("{id}/name")>]
    [<ProducesResponseType(typeof<ResourceData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateResourceName(id: string, [<FromBody>] updateDto: UpdateResourceNameDto) : Task<IActionResult> = task {
        let userId = this.CurrentUserId
        let! result = commandHandler.HandleCommand(UpdateResourceName(userId, id, updateDto))

        return
            match result with
            | Ok(ResourceResult resourceDto) -> this.Ok(resourceDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpPut("{id}/description")>]
    [<ProducesResponseType(typeof<ResourceData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateResourceDescription
        (id: string, [<FromBody>] updateDto: UpdateResourceDescriptionDto)
        : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
            let! result = commandHandler.HandleCommand(UpdateResourceDescription(userId, id, updateDto))

            return
                match result with
                | Ok(ResourceResult resourceDto) -> this.Ok(resourceDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPut("{id}/base-url")>]
    [<ProducesResponseType(typeof<ResourceData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateResourceBaseUrl
        (id: string, [<FromBody>] updateDto: UpdateResourceBaseUrlDto)
        : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
            let! result = commandHandler.HandleCommand(UpdateResourceBaseUrl(userId, id, updateDto))

            return
                match result with
                | Ok(ResourceResult resourceDto) -> this.Ok(resourceDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPut("{id}/url-parameters")>]
    [<ProducesResponseType(typeof<ResourceData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateResourceUrlParameters
        (id: string, [<FromBody>] updateDto: UpdateResourceUrlParametersDto)
        : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
            let! result = commandHandler.HandleCommand(UpdateResourceUrlParameters(userId, id, updateDto))

            return
                match result with
                | Ok(ResourceResult resourceDto) -> this.Ok(resourceDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPut("{id}/headers")>]
    [<ProducesResponseType(typeof<ResourceData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateResourceHeaders
        (id: string, [<FromBody>] updateDto: UpdateResourceHeadersDto)
        : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
            let! result = commandHandler.HandleCommand(UpdateResourceHeaders(userId, id, updateDto))

            return
                match result with
                | Ok(ResourceResult resourceDto) -> this.Ok(resourceDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPut("{id}/body")>]
    [<ProducesResponseType(typeof<ResourceData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateResourceBody(id: string, [<FromBody>] updateDto: UpdateResourceBodyDto) : Task<IActionResult> = task {
        let userId = this.CurrentUserId
        let! result = commandHandler.HandleCommand(UpdateResourceBody(userId, id, updateDto))

        return
            match result with
            | Ok(ResourceResult resourceDto) -> this.Ok(resourceDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpDelete("{id}")>]
    [<ProducesResponseType(StatusCodes.Status204NoContent)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.DeleteResource(id: string) : Task<IActionResult> = task {
        let userId = this.CurrentUserId
        let! result = commandHandler.HandleCommand(DeleteResource(userId, id))

        return
            match result with
            | Ok(ResourceUnitResult _) -> this.NoContent() :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    member private this.HandleDomainError(error: DomainError) : IActionResult =
        match error with
        | ValidationError message ->
            this.BadRequest {|
                error = "Validation failed"
                message = message
            |}
            :> IActionResult
        | NotFound message ->
            this.NotFound {|
                error = "Resource not found"
                message = message
            |}
            :> IActionResult
        | Conflict message ->
            this.Conflict {|
                error = "Conflict"
                message = message
            |}
            :> IActionResult
        | InvalidOperation message ->
            this.UnprocessableEntity {|
                error = "Invalid operation"
                message = message
            |}
            :> IActionResult