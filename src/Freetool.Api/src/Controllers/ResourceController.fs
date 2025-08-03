namespace Freetool.Api.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Freetool.Domain
open Freetool.Application.DTOs
open Freetool.Application.Commands
open Freetool.Application.Interfaces
open Freetool.Application.Mappers

[<ApiController>]
[<Route("resource")>]
type ResourceController(commandHandler: IMultiRepositoryCommandHandler<ResourceCommand, ResourceCommandResult>) =
    inherit ControllerBase()

    [<HttpPost>]
    member this.CreateResource([<FromBody>] createDto: CreateResourceDto) : Task<IActionResult> = task {
        match ResourceMapper.fromCreateDto createDto with
        | Error domainError -> return this.HandleDomainError(domainError)
        | Ok validatedResource ->
            let! result = commandHandler.HandleCommand(CreateResource validatedResource)

            return
                match result with
                | Ok(ResourceResult resourceDto) ->
                    this.CreatedAtAction(nameof this.GetResourceById, {| id = resourceDto.Id |}, resourceDto)
                    :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
    }

    [<HttpGet("{id}")>]
    member this.GetResourceById(id: string) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand(GetResourceById id)

        return
            match result with
            | Ok(ResourceResult resourceDto) -> this.Ok(resourceDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpGet>]
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
    member this.UpdateResourceName(id: string, [<FromBody>] updateDto: UpdateResourceNameDto) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand(UpdateResourceName(id, updateDto))

        return
            match result with
            | Ok(ResourceResult resourceDto) -> this.Ok(resourceDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpPut("{id}/description")>]
    member this.UpdateResourceDescription
        (id: string, [<FromBody>] updateDto: UpdateResourceDescriptionDto)
        : Task<IActionResult> =
        task {
            let! result = commandHandler.HandleCommand(UpdateResourceDescription(id, updateDto))

            return
                match result with
                | Ok(ResourceResult resourceDto) -> this.Ok(resourceDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPut("{id}/base-url")>]
    member this.UpdateResourceBaseUrl
        (id: string, [<FromBody>] updateDto: UpdateResourceBaseUrlDto)
        : Task<IActionResult> =
        task {
            let! result = commandHandler.HandleCommand(UpdateResourceBaseUrl(id, updateDto))

            return
                match result with
                | Ok(ResourceResult resourceDto) -> this.Ok(resourceDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPut("{id}/url-parameters")>]
    member this.UpdateResourceUrlParameters
        (id: string, [<FromBody>] updateDto: UpdateResourceUrlParametersDto)
        : Task<IActionResult> =
        task {
            let! result = commandHandler.HandleCommand(UpdateResourceUrlParameters(id, updateDto))

            return
                match result with
                | Ok(ResourceResult resourceDto) -> this.Ok(resourceDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPut("{id}/headers")>]
    member this.UpdateResourceHeaders
        (id: string, [<FromBody>] updateDto: UpdateResourceHeadersDto)
        : Task<IActionResult> =
        task {
            let! result = commandHandler.HandleCommand(UpdateResourceHeaders(id, updateDto))

            return
                match result with
                | Ok(ResourceResult resourceDto) -> this.Ok(resourceDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPut("{id}/body")>]
    member this.UpdateResourceBody(id: string, [<FromBody>] updateDto: UpdateResourceBodyDto) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand(UpdateResourceBody(id, updateDto))

        return
            match result with
            | Ok(ResourceResult resourceDto) -> this.Ok(resourceDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpDelete("{id}")>]
    member this.DeleteResource(id: string) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand(DeleteResource id)

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