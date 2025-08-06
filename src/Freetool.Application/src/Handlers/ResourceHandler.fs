namespace Freetool.Application.Handlers

open System
open System.Threading.Tasks
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Application.Commands
open Freetool.Application.Mappers
open Freetool.Application.DTOs

module ResourceHandler =

    let handleCommand
        (resourceRepository: IResourceRepository)
        (appRepository: IAppRepository)
        (command: ResourceCommand)
        : Task<Result<ResourceCommandResult, DomainError>> =
        task {
            match command with
            | CreateResource validatedResource ->
                // Check if name already exists
                let resourceName =
                    ResourceName.Create(Some(Resource.getName validatedResource))
                    |> function
                        | Ok n -> n
                        | Error _ -> failwith "ValidatedResource should have valid name"

                let! existsByName = resourceRepository.ExistsByNameAsync resourceName

                if existsByName then
                    return Error(Conflict "A resource with this name already exists")
                else
                    match! resourceRepository.AddAsync validatedResource with
                    | Error error -> return Error error
                    | Ok() -> return Ok(ResourceResult(validatedResource.State))

            | DeleteResource resourceId ->
                match Guid.TryParse resourceId with
                | false, _ -> return Error(ValidationError "Invalid resource ID format")
                | true, guid ->
                    let resourceIdObj = ResourceId.FromGuid guid
                    let! exists = resourceRepository.ExistsAsync resourceIdObj

                    if not exists then
                        return Error(NotFound "Resource not found")
                    else
                        match! resourceRepository.DeleteAsync resourceIdObj with
                        | Error error -> return Error error
                        | Ok() -> return Ok(ResourceUnitResult())

            | UpdateResourceName(resourceId, dto) ->
                match Guid.TryParse resourceId with
                | false, _ -> return Error(ValidationError "Invalid resource ID format")
                | true, guid ->
                    let resourceIdObj = ResourceId.FromGuid guid
                    let! resourceOption = resourceRepository.GetByIdAsync resourceIdObj

                    match resourceOption with
                    | None -> return Error(NotFound "Resource not found")
                    | Some resource ->
                        // Check if the new name already exists (only if it's different from current name)
                        if Resource.getName resource <> dto.Name then
                            match ResourceName.Create(Some dto.Name) with
                            | Error error -> return Error error
                            | Ok newResourceName ->
                                let! existsByName = resourceRepository.ExistsByNameAsync newResourceName

                                if existsByName then
                                    return Error(Conflict "A resource with this name already exists")
                                else
                                    match ResourceMapper.fromUpdateNameDto dto resource with
                                    | Error error -> return Error error
                                    | Ok validatedResource ->
                                        match! resourceRepository.UpdateAsync validatedResource with
                                        | Error error -> return Error error
                                        | Ok() -> return Ok(ResourceResult(validatedResource.State))
                        else
                            // Name hasn't changed, just update normally
                            match ResourceMapper.fromUpdateNameDto dto resource with
                            | Error error -> return Error error
                            | Ok validatedResource ->
                                match! resourceRepository.UpdateAsync validatedResource with
                                | Error error -> return Error error
                                | Ok() -> return Ok(ResourceResult(validatedResource.State))

            | UpdateResourceDescription(resourceId, dto) ->
                match Guid.TryParse resourceId with
                | false, _ -> return Error(ValidationError "Invalid resource ID format")
                | true, guid ->
                    let resourceIdObj = ResourceId.FromGuid guid
                    let! resourceOption = resourceRepository.GetByIdAsync resourceIdObj

                    match resourceOption with
                    | None -> return Error(NotFound "Resource not found")
                    | Some resource ->
                        match ResourceMapper.fromUpdateDescriptionDto dto resource with
                        | Error error -> return Error error
                        | Ok validatedResource ->
                            match! resourceRepository.UpdateAsync validatedResource with
                            | Error error -> return Error error
                            | Ok() -> return Ok(ResourceResult(validatedResource.State))

            | UpdateResourceBaseUrl(resourceId, dto) ->
                match Guid.TryParse resourceId with
                | false, _ -> return Error(ValidationError "Invalid resource ID format")
                | true, guid ->
                    let resourceIdObj = ResourceId.FromGuid guid
                    let! resourceOption = resourceRepository.GetByIdAsync resourceIdObj

                    match resourceOption with
                    | None -> return Error(NotFound "Resource not found")
                    | Some resource ->
                        match ResourceMapper.fromUpdateBaseUrlDto dto resource with
                        | Error error -> return Error error
                        | Ok validatedResource ->
                            match! resourceRepository.UpdateAsync validatedResource with
                            | Error error -> return Error error
                            | Ok() -> return Ok(ResourceResult(validatedResource.State))

            | UpdateResourceUrlParameters(resourceId, dto) ->
                match Guid.TryParse resourceId with
                | false, _ -> return Error(ValidationError "Invalid resource ID format")
                | true, guid ->
                    let resourceIdObj = ResourceId.FromGuid guid
                    let! resourceOption = resourceRepository.GetByIdAsync resourceIdObj

                    match resourceOption with
                    | None -> return Error(NotFound "Resource not found")
                    | Some resource ->
                        let! apps = appRepository.GetByResourceIdAsync resourceIdObj

                        let appConflictData =
                            apps
                            |> List.map (fun app -> {
                                AppId = (App.getId app).Value.ToString()
                                UrlParameters = App.getUrlParameters app
                                Headers = App.getHeaders app
                                Body = App.getBody app
                            })

                        match ResourceMapper.fromUpdateUrlParametersDto dto appConflictData resource with
                        | Error error -> return Error error
                        | Ok validatedResource ->
                            match! resourceRepository.UpdateAsync validatedResource with
                            | Error error -> return Error error
                            | Ok() -> return Ok(ResourceResult(validatedResource.State))

            | UpdateResourceHeaders(resourceId, dto) ->
                match Guid.TryParse resourceId with
                | false, _ -> return Error(ValidationError "Invalid resource ID format")
                | true, guid ->
                    let resourceIdObj = ResourceId.FromGuid guid
                    let! resourceOption = resourceRepository.GetByIdAsync resourceIdObj

                    match resourceOption with
                    | None -> return Error(NotFound "Resource not found")
                    | Some resource ->
                        let! apps = appRepository.GetByResourceIdAsync resourceIdObj

                        let appConflictData =
                            apps
                            |> List.map (fun app -> {
                                AppId = (App.getId app).Value.ToString()
                                UrlParameters = App.getUrlParameters app
                                Headers = App.getHeaders app
                                Body = App.getBody app
                            })

                        match ResourceMapper.fromUpdateHeadersDto dto appConflictData resource with
                        | Error error -> return Error error
                        | Ok validatedResource ->
                            match! resourceRepository.UpdateAsync validatedResource with
                            | Error error -> return Error error
                            | Ok() -> return Ok(ResourceResult(validatedResource.State))

            | UpdateResourceBody(resourceId, dto) ->
                match Guid.TryParse resourceId with
                | false, _ -> return Error(ValidationError "Invalid resource ID format")
                | true, guid ->
                    let resourceIdObj = ResourceId.FromGuid guid
                    let! resourceOption = resourceRepository.GetByIdAsync resourceIdObj

                    match resourceOption with
                    | None -> return Error(NotFound "Resource not found")
                    | Some resource ->
                        let! apps = appRepository.GetByResourceIdAsync resourceIdObj

                        let appConflictData =
                            apps
                            |> List.map (fun app -> {
                                AppId = (App.getId app).Value.ToString()
                                UrlParameters = App.getUrlParameters app
                                Headers = App.getHeaders app
                                Body = App.getBody app
                            })

                        match ResourceMapper.fromUpdateBodyDto dto appConflictData resource with
                        | Error error -> return Error error
                        | Ok validatedResource ->
                            match! resourceRepository.UpdateAsync validatedResource with
                            | Error error -> return Error error
                            | Ok() -> return Ok(ResourceResult(validatedResource.State))

            | GetResourceById resourceId ->
                match Guid.TryParse resourceId with
                | false, _ -> return Error(ValidationError "Invalid resource ID format")
                | true, guid ->
                    let resourceIdObj = ResourceId.FromGuid guid
                    let! resourceOption = resourceRepository.GetByIdAsync resourceIdObj

                    match resourceOption with
                    | None -> return Error(NotFound "Resource not found")
                    | Some resource -> return Ok(ResourceResult(resource.State))

            | GetAllResources(skip, take) ->
                if skip < 0 then
                    return Error(ValidationError "Skip cannot be negative")
                elif take <= 0 || take > 100 then
                    return Error(ValidationError "Take must be between 1 and 100")
                else
                    let! resources = resourceRepository.GetAllAsync skip take
                    let! totalCount = resourceRepository.GetCountAsync()

                    let result = {
                        Items = resources |> List.map (fun resource -> resource.State)
                        TotalCount = totalCount
                        Skip = skip
                        Take = take
                    }

                    return Ok(ResourcesResult result)
        }

type ResourceHandler(resourceRepository: IResourceRepository, appRepository: IAppRepository) =
    interface IMultiRepositoryCommandHandler<ResourceCommand, ResourceCommandResult> with
        member this.HandleCommand command =
            ResourceHandler.handleCommand resourceRepository appRepository command