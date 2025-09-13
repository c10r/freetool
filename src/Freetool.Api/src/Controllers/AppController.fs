namespace Freetool.Api.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Application.DTOs
open Freetool.Application.Commands
open Freetool.Application.Interfaces
open Freetool.Application.Mappers
open Freetool.Application.Handlers

[<ApiController>]
[<Route("app")>]
type AppController
    (
        appRepository: IAppRepository,
        resourceRepository: IResourceRepository,
        runRepository: IRunRepository,
        commandHandler: IGenericCommandHandler<IAppRepository, AppCommand, AppCommandResult>
    ) =
    inherit AuthenticatedControllerBase()

    [<HttpPost>]
    [<ProducesResponseType(typeof<AppData>, StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.CreateApp([<FromBody>] createDto: CreateAppDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
            let createRequest = AppMapper.fromCreateDto createDto

            let urlPathValidation =
                match createDto.UrlPath with
                | Some urlPath when urlPath.Length > 500 ->
                    Error(ValidationError "URL path cannot exceed 500 characters")
                | _ -> Ok()

            let folderIdResult =
                match System.Guid.TryParse createRequest.FolderId with
                | true, guid -> Ok(FolderId.FromGuid(guid))
                | false, _ -> Error(ValidationError "Invalid folder ID format")

            let resourceIdResult =
                match System.Guid.TryParse createRequest.ResourceId with
                | true, guid -> Ok(ResourceId.FromGuid(guid))
                | false, _ -> Error(ValidationError "Invalid resource ID format")

            match folderIdResult, resourceIdResult, urlPathValidation with
            | Error error, _, _ -> return this.HandleDomainError(error)
            | _, Error error, _ -> return this.HandleDomainError(error)
            | _, _, Error error -> return this.HandleDomainError(error)
            | Ok folderId, Ok resourceId, Ok _ ->
                // Fetch the resource to validate against
                let! resourceOption = resourceRepository.GetByIdAsync resourceId

                match resourceOption with
                | None -> return this.HandleDomainError(NotFound "Resource not found")
                | Some resource ->
                    // Use Domain method that enforces no-override business rule
                    match
                        App.createWithResource
                            userId
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
                        let! result = commandHandler.HandleCommand appRepository (CreateApp(userId, validatedApp))

                        return
                            match result with
                            | Ok(AppResult appDto) ->
                                this.CreatedAtAction(nameof this.GetAppById, {| id = appDto.Id |}, appDto)
                                :> IActionResult
                            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                            | Error error -> this.HandleDomainError(error)
        }

    [<HttpGet("{id}")>]
    [<ProducesResponseType(typeof<AppData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.GetAppById(id: string) : Task<IActionResult> =
        task {
            let! result = commandHandler.HandleCommand appRepository (GetAppById id)

            return
                match result with
                | Ok(AppResult appDto) -> this.Ok(appDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpGet("folder/{folderId}")>]
    [<ProducesResponseType(typeof<PagedResult<AppData>>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
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
    [<ProducesResponseType(typeof<PagedResult<AppData>>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.GetApps([<FromQuery>] skip: int, [<FromQuery>] take: int) : Task<IActionResult> =
        task {
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
    [<ProducesResponseType(typeof<AppData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateAppName(id: string, [<FromBody>] updateDto: UpdateAppNameDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
            let! result = commandHandler.HandleCommand appRepository (UpdateAppName(userId, id, updateDto))

            return
                match result with
                | Ok(AppResult appDto) -> this.Ok(appDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPut("{id}/inputs")>]
    [<ProducesResponseType(typeof<AppData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateAppInputs(id: string, [<FromBody>] updateDto: UpdateAppInputsDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
            let! result = commandHandler.HandleCommand appRepository (UpdateAppInputs(userId, id, updateDto))

            return
                match result with
                | Ok(AppResult appDto) -> this.Ok(appDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPut("{id}/query-parameters")>]
    [<ProducesResponseType(typeof<AppData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateAppQueryParameters
        (id: string, [<FromBody>] updateDto: UpdateAppQueryParametersDto)
        : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            let! result =
                AppHandler.handleCommandWithResourceRepository
                    appRepository
                    resourceRepository
                    (UpdateAppQueryParameters(userId, id, updateDto))

            return
                match result with
                | Ok(AppResult appDto) -> this.Ok(appDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPut("{id}/body")>]
    [<ProducesResponseType(typeof<AppData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateAppBody(id: string, [<FromBody>] updateDto: UpdateAppBodyDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            let! result =
                AppHandler.handleCommandWithResourceRepository
                    appRepository
                    resourceRepository
                    (UpdateAppBody(userId, id, updateDto))

            return
                match result with
                | Ok(AppResult appDto) -> this.Ok(appDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPut("{id}/headers")>]
    [<ProducesResponseType(typeof<AppData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateAppHeaders(id: string, [<FromBody>] updateDto: UpdateAppHeadersDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            let! result =
                AppHandler.handleCommandWithResourceRepository
                    appRepository
                    resourceRepository
                    (UpdateAppHeaders(userId, id, updateDto))

            return
                match result with
                | Ok(AppResult appDto) -> this.Ok(appDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPut("{id}/url-path")>]
    [<ProducesResponseType(typeof<AppData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateAppUrlPath(id: string, [<FromBody>] updateDto: UpdateAppUrlPathDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            match updateDto.UrlPath with
            | Some urlPath when urlPath.Length > 500 ->
                let error = ValidationError "URL path cannot exceed 500 characters"
                return this.HandleDomainError(error)
            | _ ->
                let! result = commandHandler.HandleCommand appRepository (UpdateAppUrlPath(userId, id, updateDto))

                match result with
                | Ok(AppResult appDto) -> return this.Ok(appDto) :> IActionResult
                | Ok _ -> return this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> return this.HandleDomainError(error)
        }

    [<HttpDelete("{id}")>]
    [<ProducesResponseType(StatusCodes.Status204NoContent)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.DeleteApp(id: string) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId
            let! result = commandHandler.HandleCommand appRepository (DeleteApp(userId, id))

            return
                match result with
                | Ok(AppUnitResult _) -> this.NoContent() :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpPost("{id}/run")>]
    [<ProducesResponseType(typeof<RunData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.RunApp(id: string, [<FromBody>] createRunDto: CreateRunDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            let! result =
                RunHandler.handleCommand
                    runRepository
                    appRepository
                    resourceRepository
                    (CreateRun(userId, id, createRunDto))

            return
                match result with
                | Ok(RunResult runDto) -> this.Ok(runDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    member private this.HandleDomainError(error: DomainError) : IActionResult =
        match error with
        | ValidationError message ->
            this.BadRequest
                {| error = "Validation failed"
                   message = message |}
            :> IActionResult
        | NotFound message ->
            this.NotFound
                {| error = "Resource not found"
                   message = message |}
            :> IActionResult
        | Conflict message ->
            this.Conflict
                {| error = "Conflict"
                   message = message |}
            :> IActionResult
        | InvalidOperation message ->
            this.UnprocessableEntity
                {| error = "Invalid operation"
                   message = message |}
            :> IActionResult
