namespace Freetool.Api.Tracing

open System.Diagnostics
open Freetool.Application.Commands
open Freetool.Application.Interfaces
open Freetool.Domain.Entities

type TracingAppCommandHandlerDecorator
    (inner: IGenericCommandHandler<IAppRepository, AppCommand, AppCommandResult>, activitySource: ActivitySource) =

    interface IGenericCommandHandler<IAppRepository, AppCommand, AppCommandResult> with
        member this.HandleCommand repository command =
            let spanName = this.GetSpanName command

            Tracing.withSpan activitySource spanName (fun activity ->
                this.AddCommandAttributes activity command

                task {
                    let! result = inner.HandleCommand repository command

                    match result with
                    | Ok commandResult ->
                        this.AddSuccessAttributes activity command commandResult
                        Tracing.setSpanStatus activity true None
                        return result
                    | Error error ->
                        Tracing.addDomainErrorEvent activity error
                        Tracing.setSpanStatus activity false None
                        return result
                })

    member private this.GetSpanName command =
        match command with
        | CreateApp _ -> "app.create"
        | GetAppById _ -> "app.get_by_id"
        | GetAppsByFolderId _ -> "app.get_by_folder_id"
        | GetAllApps _ -> "app.get_all"
        | UpdateAppName _ -> "app.update_name"
        | UpdateAppInputs _ -> "app.update_inputs"
        | DeleteApp _ -> "app.delete"

    member private this.AddCommandAttributes activity command =
        match command with
        | CreateApp app ->
            Tracing.addAttribute activity "app.name" (App.getName app)
            Tracing.addAttribute activity "app.folder_id" ((App.getFolderId app).Value.ToString())
            Tracing.addIntAttribute activity "app.inputs_count" (App.getInputs app).Length
            Tracing.addAttribute activity "operation.type" "create"
        | GetAppById id ->
            Tracing.addAttribute activity "app.id" id
            Tracing.addAttribute activity "operation.type" "read"
        | GetAppsByFolderId(folderId, skip, take) ->
            Tracing.addAttribute activity "app.folder_id" folderId
            Tracing.addPaginationAttributes activity skip take None
            Tracing.addAttribute activity "operation.type" "read"
        | GetAllApps(skip, take) ->
            Tracing.addPaginationAttributes activity skip take None
            Tracing.addAttribute activity "operation.type" "read"
        | UpdateAppName(id, dto) ->
            Tracing.addAttribute activity "app.id" id
            Tracing.addAttribute activity "operation.type" "update"
            Tracing.addAttribute activity "update.field" "name"
            Tracing.addAttribute activity "update.value" dto.Name
        | UpdateAppInputs(id, dto) ->
            Tracing.addAttribute activity "app.id" id
            Tracing.addAttribute activity "operation.type" "update"
            Tracing.addAttribute activity "update.field" "inputs"
            Tracing.addIntAttribute activity "update.inputs_count" dto.Inputs.Length
        | DeleteApp id ->
            Tracing.addAttribute activity "app.id" id
            Tracing.addAttribute activity "operation.type" "delete"

    member private this.AddSuccessAttributes activity command result =
        match result with
        | AppResult appDto ->
            Tracing.addAttribute activity "app.id" appDto.Id
            Tracing.addAttribute activity "app.name" appDto.Name
            Tracing.addAttribute activity "app.folder_id" appDto.FolderId
            Tracing.addIntAttribute activity "app.inputs_count" appDto.Inputs.Length
        | AppsResult pagedApps ->
            Tracing.addIntAttribute activity "result.count" pagedApps.Apps.Length

            match command with
            | GetAppsByFolderId(_, skip, take)
            | GetAllApps(skip, take) -> Tracing.addPaginationAttributes activity skip take (Some pagedApps.Apps.Length)
            | _ -> ()
        | AppUnitResult _ -> ()