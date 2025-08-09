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

module AppHandler =

    let handleCommand
        (appRepository: IAppRepository)
        (command: AppCommand)
        : Task<Result<AppCommandResult, DomainError>> =
        task {
            match command with
            | CreateApp(actorUserId, validatedApp) ->
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
                    | Ok() -> return Ok(AppResult(validatedApp.State))

            | DeleteApp(actorUserId, appId) ->
                match Guid.TryParse appId with
                | false, _ -> return Error(ValidationError "Invalid app ID format")
                | true, guid ->
                    let appIdObj = AppId.FromGuid guid
                    let! appOption = appRepository.GetByIdAsync appIdObj

                    match appOption with
                    | None -> return Error(NotFound "App not found")
                    | Some app ->
                        // Mark app for deletion to create the delete event
                        let appWithDeleteEvent = App.markForDeletion actorUserId app

                        // Delete app and save event atomically
                        match! appRepository.DeleteAsync appWithDeleteEvent with
                        | Error error -> return Error error
                        | Ok() -> return Ok(AppUnitResult())

            | UpdateAppName(actorUserId, appId, dto) ->
                match Guid.TryParse appId with
                | false, _ -> return Error(ValidationError "Invalid app ID format")
                | true, guid ->
                    let appIdObj = AppId.FromGuid guid
                    let! appOption = appRepository.GetByIdAsync appIdObj

                    match appOption with
                    | None -> return Error(NotFound "App not found")
                    | Some app ->
                        // Check if new name conflicts with existing app in same folder (only if name changed)
                        if App.getName app <> dto.Name then
                            match AppName.Create(Some dto.Name) with
                            | Error error -> return Error error
                            | Ok newAppName ->
                                let folderId = App.getFolderId app

                                let! existsByNameAndFolder =
                                    appRepository.ExistsByNameAndFolderIdAsync newAppName folderId

                                if existsByNameAndFolder then
                                    return Error(Conflict "An app with this name already exists in the folder")
                                else
                                    // Update name using domain method (automatically creates event)
                                    match App.updateName actorUserId dto.Name app with
                                    | Error error -> return Error error
                                    | Ok updatedApp ->
                                        // Save app and events atomically
                                        match! appRepository.UpdateAsync updatedApp with
                                        | Error error -> return Error error
                                        | Ok() -> return Ok(AppResult(updatedApp.State))
                        else
                            return Ok(AppResult(app.State))

            | UpdateAppInputs(actorUserId, appId, dto) ->
                match Guid.TryParse appId with
                | false, _ -> return Error(ValidationError "Invalid app ID format")
                | true, guid ->
                    let appIdObj = AppId.FromGuid guid
                    let! appOption = appRepository.GetByIdAsync appIdObj

                    match appOption with
                    | None -> return Error(NotFound "App not found")
                    | Some app ->
                        let inputs = dto.Inputs |> List.map AppMapper.inputFromDto

                        // Update inputs using domain method (automatically creates event)
                        match App.updateInputs actorUserId inputs app with
                        | Error error -> return Error error
                        | Ok updatedApp ->
                            // Save app and events atomically
                            match! appRepository.UpdateAsync updatedApp with
                            | Error error -> return Error error
                            | Ok() -> return Ok(AppResult(updatedApp.State))

            | GetAppById appId ->
                match Guid.TryParse appId with
                | false, _ -> return Error(ValidationError "Invalid app ID format")
                | true, guid ->
                    let appIdObj = AppId.FromGuid guid
                    let! appOption = appRepository.GetByIdAsync appIdObj

                    match appOption with
                    | None -> return Error(NotFound "App not found")
                    | Some app -> return Ok(AppResult(app.State))

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

                        let result = {
                            Items = apps |> List.map (fun app -> app.State)
                            TotalCount = totalCount
                            Skip = skip
                            Take = take
                        }

                        return Ok(AppsResult result)

            | GetAllApps(skip, take) ->
                if skip < 0 then
                    return Error(ValidationError "Skip cannot be negative")
                elif take <= 0 || take > 100 then
                    return Error(ValidationError "Take must be between 1 and 100")
                else
                    let! apps = appRepository.GetAllAsync skip take
                    let! totalCount = appRepository.GetCountAsync()

                    let result = {
                        Items = apps |> List.map (fun app -> app.State)
                        TotalCount = totalCount
                        Skip = skip
                        Take = take
                    }

                    return Ok(AppsResult result)
        }

type AppHandler() =
    interface IGenericCommandHandler<IAppRepository, AppCommand, AppCommandResult> with
        member this.HandleCommand repository command =
            AppHandler.handleCommand repository command