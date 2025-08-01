namespace Freetool.Application.Handlers

open System
open System.Threading.Tasks
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Application.Commands
open Freetool.Application.Mappers

module AppHandler =

    let private mapAppToDto = AppMapper.toDto

    let handleCommand
        (appRepository: IAppRepository)
        (command: AppCommand)
        : Task<Result<AppCommandResult, DomainError>> =
        task {
            match command with
            | CreateApp validatedApp ->
                // Check if app name already exists in folder
                let appName =
                    AppName.Create(Some(App.getName validatedApp))
                    |> function
                        | Ok name -> name
                        | Error _ -> failwith "ValidatedApp should have valid name"

                let folderId = App.getFolderId validatedApp

                let! existsByNameAndFolder = appRepository.ExistsByNameAndFolderIdAsync appName folderId

                if existsByNameAndFolder then
                    return Error(Conflict "An app with this name already exists in the folder")
                else
                    match! appRepository.AddAsync validatedApp with
                    | Error error -> return Error error
                    | Ok() -> return Ok(AppResult(mapAppToDto validatedApp))

            | DeleteApp appId ->
                match Guid.TryParse appId with
                | false, _ -> return Error(ValidationError "Invalid app ID format")
                | true, guid ->
                    let appIdObj = AppId.FromGuid guid
                    let! exists = appRepository.ExistsAsync appIdObj

                    if not exists then
                        return Error(NotFound "App not found")
                    else
                        match! appRepository.DeleteAsync appIdObj with
                        | Error error -> return Error error
                        | Ok() -> return Ok(AppUnitResult())

            | UpdateAppName(appId, dto) ->
                match Guid.TryParse appId with
                | false, _ -> return Error(ValidationError "Invalid app ID format")
                | true, guid ->
                    let appIdObj = AppId.FromGuid guid
                    let! appOption = appRepository.GetByIdAsync appIdObj

                    match appOption with
                    | None -> return Error(NotFound "App not found")
                    | Some app ->
                        let unvalidatedApp = AppMapper.fromUpdateNameDto dto app

                        match App.validate unvalidatedApp with
                        | Error error -> return Error error
                        | Ok validatedApp ->
                            // Check if new name conflicts with existing app in same folder
                            let newAppName =
                                AppName.Create(Some dto.Name)
                                |> function
                                    | Ok name -> name
                                    | Error error -> failwith $"DTO validation should have caught this: {error}"

                            let folderId = App.getFolderId validatedApp
                            let! existsByNameAndFolder = appRepository.ExistsByNameAndFolderIdAsync newAppName folderId

                            if existsByNameAndFolder then
                                return Error(Conflict "An app with this name already exists in the folder")
                            else
                                match! appRepository.UpdateAsync validatedApp with
                                | Error error -> return Error error
                                | Ok() -> return Ok(AppResult(mapAppToDto validatedApp))

            | UpdateAppInputs(appId, dto) ->
                match Guid.TryParse appId with
                | false, _ -> return Error(ValidationError "Invalid app ID format")
                | true, guid ->
                    let appIdObj = AppId.FromGuid guid
                    let! appOption = appRepository.GetByIdAsync appIdObj

                    match appOption with
                    | None -> return Error(NotFound "App not found")
                    | Some app ->
                        let unvalidatedApp = AppMapper.fromUpdateInputsDto dto app

                        match App.validate unvalidatedApp with
                        | Error error -> return Error error
                        | Ok validatedApp ->
                            match! appRepository.UpdateAsync validatedApp with
                            | Error error -> return Error error
                            | Ok() -> return Ok(AppResult(mapAppToDto validatedApp))

            | GetAppById appId ->
                match Guid.TryParse appId with
                | false, _ -> return Error(ValidationError "Invalid app ID format")
                | true, guid ->
                    let appIdObj = AppId.FromGuid guid
                    let! appOption = appRepository.GetByIdAsync appIdObj

                    match appOption with
                    | None -> return Error(NotFound "App not found")
                    | Some app -> return Ok(AppResult(mapAppToDto app))

            | GetAppsByFolderId(folderId, skip, take) ->
                match Guid.TryParse folderId with
                | false, _ -> return Error(ValidationError "Invalid folder ID format")
                | true, guid ->
                    if skip < 0 then
                        return Error(ValidationError "Skip cannot be negative")
                    elif take <= 0 || take > 100 then
                        return Error(ValidationError "Take must be between 1 and 100")
                    else
                        let folderIdObj = FolderId.FromGuid guid
                        let! apps = appRepository.GetByFolderIdAsync folderIdObj skip take
                        let! totalCount = appRepository.GetCountByFolderIdAsync folderIdObj
                        let result = AppMapper.toPagedDto apps totalCount skip take

                        return Ok(AppsResult result)

            | GetAllApps(skip, take) ->
                if skip < 0 then
                    return Error(ValidationError "Skip cannot be negative")
                elif take <= 0 || take > 100 then
                    return Error(ValidationError "Take must be between 1 and 100")
                else
                    let! apps = appRepository.GetAllAsync skip take
                    let! totalCount = appRepository.GetCountAsync()
                    let result = AppMapper.toPagedDto apps totalCount skip take

                    return Ok(AppsResult result)
        }

type AppHandler() =
    interface IGenericCommandHandler<IAppRepository, AppCommand, AppCommandResult> with
        member this.HandleCommand repository command =
            AppHandler.handleCommand repository command