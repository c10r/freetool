namespace Freetool.Api.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Application.DTOs
open Freetool.Application.Commands
open Freetool.Application.Interfaces
open Freetool.Application.Mappers

[<ApiController>]
[<Route("app")>]
type AppController
    (
        appRepository: IAppRepository,
        resourceRepository: IResourceRepository,
        commandHandler: IGenericCommandHandler<IAppRepository, AppCommand, AppCommandResult>
    ) =
    inherit ControllerBase()

    [<HttpPost>]
    member this.CreateApp([<FromBody>] createDto: CreateAppDto) : Task<IActionResult> = task {
        let createRequest = AppMapper.fromCreateDto createDto

        // Parse FolderId and ResourceId
        let folderIdResult =
            match System.Guid.TryParse createRequest.FolderId with
            | true, guid -> Ok(FolderId.FromGuid(guid))
            | false, _ -> Error(ValidationError "Invalid folder ID format")

        let resourceIdResult =
            match System.Guid.TryParse createRequest.ResourceId with
            | true, guid -> Ok(ResourceId.FromGuid(guid))
            | false, _ -> Error(ValidationError "Invalid resource ID format")

        match folderIdResult, resourceIdResult with
        | Error error, _
        | _, Error error -> return this.HandleDomainError(error)
        | Ok folderId, Ok resourceId ->
            // Fetch the resource to validate against
            let! resourceOption = resourceRepository.GetByIdAsync resourceId

            match resourceOption with
            | None -> return this.HandleDomainError(NotFound "Resource not found")
            | Some resource ->
                // Use Domain method that enforces no-override business rule
                match
                    App.createWithResource
                        createRequest.Name
                        folderId
                        resource
                        createRequest.Inputs
                        createRequest.UrlPath
                        createRequest.UrlParameters
                        createRequest.Headers
                        createRequest.Body
                with
                | Error domainError -> return this.HandleDomainError(domainError)
                | Ok validatedApp ->
                    let! result = commandHandler.HandleCommand appRepository (CreateApp validatedApp)

                    return
                        match result with
                        | Ok(AppResult appDto) ->
                            this.CreatedAtAction(nameof this.GetAppById, {| id = appDto.Id |}, appDto) :> IActionResult
                        | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                        | Error error -> this.HandleDomainError(error)
    }

    [<HttpGet("{id}")>]
    member this.GetAppById(id: string) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand appRepository (GetAppById id)

        return
            match result with
            | Ok(AppResult appDto) -> this.Ok(appDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpGet("folder/{folderId}")>]
    member this.GetAppsByFolderId
        (folderId: string, [<FromQuery>] skip: int, [<FromQuery>] take: int)
        : Task<IActionResult> =
        task {
            let skipValue = if skip < 0 then 0 else skip

            let takeValue =
                if take <= 0 then 10
                elif take > 100 then 100
                else take

            let! result = commandHandler.HandleCommand appRepository (GetAppsByFolderId(folderId, skipValue, takeValue))

            return
                match result with
                | Ok(AppsResult pagedApps) -> this.Ok(pagedApps) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpGet>]
    member this.GetApps([<FromQuery>] skip: int, [<FromQuery>] take: int) : Task<IActionResult> = task {
        let skipValue = if skip < 0 then 0 else skip

        let takeValue =
            if take <= 0 then 10
            elif take > 100 then 100
            else take

        let! result = commandHandler.HandleCommand appRepository (GetAllApps(skipValue, takeValue))

        return
            match result with
            | Ok(AppsResult pagedApps) -> this.Ok(pagedApps) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpPut("{id}/name")>]
    member this.UpdateAppName(id: string, [<FromBody>] updateDto: UpdateAppNameDto) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand appRepository (UpdateAppName(id, updateDto))

        return
            match result with
            | Ok(AppResult appDto) -> this.Ok(appDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpPut("{id}/inputs")>]
    member this.UpdateAppInputs(id: string, [<FromBody>] updateDto: UpdateAppInputsDto) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand appRepository (UpdateAppInputs(id, updateDto))

        return
            match result with
            | Ok(AppResult appDto) -> this.Ok(appDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpDelete("{id}")>]
    member this.DeleteApp(id: string) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand appRepository (DeleteApp id)

        return
            match result with
            | Ok(AppUnitResult _) -> this.NoContent() :> IActionResult
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