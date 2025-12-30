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
        folderRepository: IFolderRepository,
        spaceRepository: ISpaceRepository,
        authorizationService: IAuthorizationService,
        commandHandler: IGenericCommandHandler<IAppRepository, AppCommand, AppCommandResult>
    ) =
    inherit AuthenticatedControllerBase()

    // Helper method to get space ID from folder ID
    member private this.GetSpaceIdFromFolder(folderId: FolderId) : Task<Result<SpaceId, DomainError>> =
        task {
            let! folderOption = folderRepository.GetByIdAsync folderId

            match folderOption with
            | None -> return Error(NotFound "Folder not found")
            | Some folder -> return Ok(Folder.getSpaceId folder)
        }

    // Helper method to get space ID from app ID
    member private this.GetSpaceIdFromApp(appId: string) : Task<Result<SpaceId, DomainError>> =
        task {
            match System.Guid.TryParse appId with
            | false, _ -> return Error(ValidationError "Invalid app ID format")
            | true, guid ->
                let appIdObj = AppId.FromGuid(guid)
                let! appOption = appRepository.GetByIdAsync appIdObj

                match appOption with
                | None -> return Error(NotFound "App not found")
                | Some app ->
                    let folderId = App.getFolderId app
                    return! this.GetSpaceIdFromFolder folderId
        }

    // Helper method to check authorization
    member private this.CheckAuthorization
        (spaceId: SpaceId)
        (permission: AuthRelation)
        : Task<Result<unit, DomainError>> =
        task {
            let userId = this.CurrentUserId

            let! hasPermission =
                authorizationService.CheckPermissionAsync
                    (User(userId.Value.ToString()))
                    permission
                    (SpaceObject(spaceId.Value.ToString()))

            if hasPermission then
                return Ok()
            else
                let permissionName = AuthTypes.relationToString permission
                return Error(InvalidOperation $"User does not have {permissionName} permission on this space")
        }

    member private this.IsOrganizationAdmin() : Task<bool> =
        task {
            let userId = this.CurrentUserId

            return!
                authorizationService.CheckPermissionAsync
                    (User(userId.Value.ToString()))
                    OrganizationAdmin
                    (OrganizationObject "default")
        }

    member private this.GetAccessibleSpaceIdsForCurrentUser() : Task<SpaceId list> =
        task {
            let! isOrgAdmin = this.IsOrganizationAdmin()

            if isOrgAdmin then
                // Org admins can see all spaces
                let! allSpaces = spaceRepository.GetAllAsync 0 System.Int32.MaxValue
                return allSpaces |> List.map (fun space -> space.State.Id)
            else
                // Regular users see only spaces they're members of
                let userId = this.CurrentUserId
                let! spaces = spaceRepository.GetByUserIdAsync userId
                return spaces |> List.map (fun space -> space.State.Id)
        }

    // Helper method to handle authorization errors
    member private this.HandleAuthorizationError() : IActionResult =
        this.StatusCode(
            403,
            {| error = "Forbidden"
               message = "You do not have permission to perform this action" |}
        )
        :> IActionResult

    [<HttpPost>]
    [<ProducesResponseType(typeof<AppData>, StatusCodes.Status201Created)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
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
                // Check authorization: user must have create_app permission on the space
                let! spaceIdResult = this.GetSpaceIdFromFolder folderId

                match spaceIdResult with
                | Error error -> return this.HandleDomainError(error)
                | Ok spaceId ->
                    let! authResult = this.CheckAuthorization spaceId AppCreate

                    match authResult with
                    | Error _ -> return this.HandleAuthorizationError()
                    | Ok() ->
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
                                let! result =
                                    commandHandler.HandleCommand appRepository (CreateApp(userId, validatedApp))

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
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.GetAppById(id: string) : Task<IActionResult> =
        task {
            // Check authorization: user must have run_app permission on the space
            let! spaceIdResult = this.GetSpaceIdFromApp id

            match spaceIdResult with
            | Error error -> return this.HandleDomainError(error)
            | Ok spaceId ->
                let! authResult = this.CheckAuthorization spaceId AppRun

                match authResult with
                | Error _ -> return this.HandleAuthorizationError()
                | Ok() ->
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
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
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

            // Parse and validate folderId
            match System.Guid.TryParse folderId with
            | false, _ -> return this.HandleDomainError(ValidationError "Invalid folder ID format")
            | true, guid ->
                let folderIdObj = FolderId.FromGuid(guid)

                // Check authorization: user must have run_app permission on the space
                let! spaceIdResult = this.GetSpaceIdFromFolder folderIdObj

                match spaceIdResult with
                | Error error -> return this.HandleDomainError(error)
                | Ok spaceId ->
                    let! authResult = this.CheckAuthorization spaceId AppRun

                    match authResult with
                    | Error _ -> return this.HandleAuthorizationError()
                    | Ok() ->
                        let! result =
                            commandHandler.HandleCommand
                                appRepository
                                (GetAppsByFolderId(folderId, skipValue, takeValue))

                        return
                            match result with
                            | Ok(AppsResult pagedApps) -> this.Ok(pagedApps) :> IActionResult
                            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                            | Error error -> this.HandleDomainError(error)
        }

    [<HttpGet>]
    [<ProducesResponseType(typeof<PagedResult<AppData>>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.GetApps([<FromQuery>] skip: int, [<FromQuery>] take: int) : Task<IActionResult> =
        task {
            let skipValue = if skip < 0 then 0 else skip

            let takeValue =
                if take <= 0 then 10
                elif take > 100 then 100
                else take

            let! spaceIds = this.GetAccessibleSpaceIdsForCurrentUser()

            if List.isEmpty spaceIds then
                let emptyResult: PagedResult<AppData> =
                    { Items = []
                      TotalCount = 0
                      Skip = skipValue
                      Take = takeValue }

                return this.Ok(emptyResult) :> IActionResult
            else
                let! result =
                    commandHandler.HandleCommand appRepository (GetAppsBySpaceIds(spaceIds, skipValue, takeValue))

                return
                    match result with
                    | Ok(AppsResult pagedApps) -> this.Ok(pagedApps) :> IActionResult
                    | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                    | Error error -> this.HandleDomainError(error)
        }

    [<HttpPut("{id}/name")>]
    [<ProducesResponseType(typeof<AppData>, StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateAppName(id: string, [<FromBody>] updateDto: UpdateAppNameDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            // Check authorization: user must have edit_app permission on the space
            let! spaceIdResult = this.GetSpaceIdFromApp id

            match spaceIdResult with
            | Error error -> return this.HandleDomainError(error)
            | Ok spaceId ->
                let! authResult = this.CheckAuthorization spaceId AppEdit

                match authResult with
                | Error _ -> return this.HandleAuthorizationError()
                | Ok() ->
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
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateAppInputs(id: string, [<FromBody>] updateDto: UpdateAppInputsDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            // Check authorization: user must have edit_app permission on the space
            let! spaceIdResult = this.GetSpaceIdFromApp id

            match spaceIdResult with
            | Error error -> return this.HandleDomainError(error)
            | Ok spaceId ->
                let! authResult = this.CheckAuthorization spaceId AppEdit

                match authResult with
                | Error _ -> return this.HandleAuthorizationError()
                | Ok() ->
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
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateAppQueryParameters
        (id: string, [<FromBody>] updateDto: UpdateAppQueryParametersDto)
        : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            // Check authorization: user must have edit_app permission on the space
            let! spaceIdResult = this.GetSpaceIdFromApp id

            match spaceIdResult with
            | Error error -> return this.HandleDomainError(error)
            | Ok spaceId ->
                let! authResult = this.CheckAuthorization spaceId AppEdit

                match authResult with
                | Error _ -> return this.HandleAuthorizationError()
                | Ok() ->
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
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateAppBody(id: string, [<FromBody>] updateDto: UpdateAppBodyDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            // Check authorization: user must have edit_app permission on the space
            let! spaceIdResult = this.GetSpaceIdFromApp id

            match spaceIdResult with
            | Error error -> return this.HandleDomainError(error)
            | Ok spaceId ->
                let! authResult = this.CheckAuthorization spaceId AppEdit

                match authResult with
                | Error _ -> return this.HandleAuthorizationError()
                | Ok() ->
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
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.UpdateAppHeaders(id: string, [<FromBody>] updateDto: UpdateAppHeadersDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            // Check authorization: user must have edit_app permission on the space
            let! spaceIdResult = this.GetSpaceIdFromApp id

            match spaceIdResult with
            | Error error -> return this.HandleDomainError(error)
            | Ok spaceId ->
                let! authResult = this.CheckAuthorization spaceId AppEdit

                match authResult with
                | Error _ -> return this.HandleAuthorizationError()
                | Ok() ->
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
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
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
                // Check authorization: user must have edit_app permission on the space
                let! spaceIdResult = this.GetSpaceIdFromApp id

                match spaceIdResult with
                | Error error -> return this.HandleDomainError(error)
                | Ok spaceId ->
                    let! authResult = this.CheckAuthorization spaceId AppEdit

                    match authResult with
                    | Error _ -> return this.HandleAuthorizationError()
                    | Ok() ->
                        let! result =
                            commandHandler.HandleCommand appRepository (UpdateAppUrlPath(userId, id, updateDto))

                        match result with
                        | Ok(AppResult appDto) -> return this.Ok(appDto) :> IActionResult
                        | Ok _ -> return this.StatusCode(500, "Unexpected result type") :> IActionResult
                        | Error error -> return this.HandleDomainError(error)
        }

    [<HttpDelete("{id}")>]
    [<ProducesResponseType(StatusCodes.Status204NoContent)>]
    [<ProducesResponseType(StatusCodes.Status400BadRequest)>]
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.DeleteApp(id: string) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            // Check authorization: user must have delete_app permission on the space
            let! spaceIdResult = this.GetSpaceIdFromApp id

            match spaceIdResult with
            | Error error -> return this.HandleDomainError(error)
            | Ok spaceId ->
                let! authResult = this.CheckAuthorization spaceId AppDelete

                match authResult with
                | Error _ -> return this.HandleAuthorizationError()
                | Ok() ->
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
    [<ProducesResponseType(StatusCodes.Status403Forbidden)>]
    [<ProducesResponseType(StatusCodes.Status404NotFound)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.RunApp(id: string, [<FromBody>] createRunDto: CreateRunDto) : Task<IActionResult> =
        task {
            let userId = this.CurrentUserId

            // Check authorization: user must have run_app permission on the space
            let! spaceIdResult = this.GetSpaceIdFromApp id

            match spaceIdResult with
            | Error error -> return this.HandleDomainError(error)
            | Ok spaceId ->
                let! authResult = this.CheckAuthorization spaceId AppRun

                match authResult with
                | Error _ -> return this.HandleAuthorizationError()
                | Ok() ->
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
