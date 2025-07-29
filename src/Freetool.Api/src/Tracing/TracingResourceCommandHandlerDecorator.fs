namespace Freetool.Api.Tracing

open System.Diagnostics
open Freetool.Application.Commands
open Freetool.Application.Interfaces
open Freetool.Domain.Entities

type TracingResourceCommandHandlerDecorator
    (
        inner: IGenericCommandHandler<IResourceRepository, ResourceCommand, ResourceCommandResult>,
        activitySource: ActivitySource
    ) =

    interface IGenericCommandHandler<IResourceRepository, ResourceCommand, ResourceCommandResult> with
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
        | CreateResource _ -> "resource.create"
        | GetResourceById _ -> "resource.get_by_id"
        | GetAllResources _ -> "resource.get_all"
        | UpdateResourceName _ -> "resource.update_name"
        | UpdateResourceDescription _ -> "resource.update_description"
        | UpdateResourceBaseUrl _ -> "resource.update_base_url"
        | UpdateResourceUrlParameters _ -> "resource.update_url_parameters"
        | UpdateResourceHeaders _ -> "resource.update_headers"
        | UpdateResourceBody _ -> "resource.update_body"
        | DeleteResource _ -> "resource.delete"

    member private this.AddCommandAttributes activity command =
        match command with
        | CreateResource resource ->
            Tracing.addAttribute activity "resource.name" (Resource.getName resource)
            Tracing.addAttribute activity "operation.type" "create"
        | GetResourceById id ->
            Tracing.addAttribute activity "resource.id" id
            Tracing.addAttribute activity "operation.type" "read"
        | GetAllResources(skip, take) ->
            Tracing.addPaginationAttributes activity skip take None
            Tracing.addAttribute activity "operation.type" "read"
        | UpdateResourceName(id, dto) ->
            Tracing.addAttribute activity "resource.id" id
            Tracing.addAttribute activity "operation.type" "update"
            Tracing.addAttribute activity "update.field" "name"
            Tracing.addAttribute activity "update.value" dto.Name
        | UpdateResourceDescription(id, dto) ->
            Tracing.addAttribute activity "resource.id" id
            Tracing.addAttribute activity "operation.type" "update"
            Tracing.addAttribute activity "update.field" "description"
            Tracing.addAttribute activity "update.value" dto.Description
        | UpdateResourceBaseUrl(id, dto) ->
            Tracing.addAttribute activity "resource.id" id
            Tracing.addAttribute activity "operation.type" "update"
            Tracing.addAttribute activity "update.field" "base_url"
            Tracing.addAttribute activity "update.value" dto.BaseUrl
        | UpdateResourceUrlParameters(id, _) ->
            Tracing.addAttribute activity "resource.id" id
            Tracing.addAttribute activity "operation.type" "update"
            Tracing.addAttribute activity "update.field" "url_parameters"
        | UpdateResourceHeaders(id, _) ->
            Tracing.addAttribute activity "resource.id" id
            Tracing.addAttribute activity "operation.type" "update"
            Tracing.addAttribute activity "update.field" "headers"
        | UpdateResourceBody(id, _) ->
            Tracing.addAttribute activity "resource.id" id
            Tracing.addAttribute activity "operation.type" "update"
            Tracing.addAttribute activity "update.field" "body"
        | DeleteResource id ->
            Tracing.addAttribute activity "resource.id" id
            Tracing.addAttribute activity "operation.type" "delete"

    member private this.AddSuccessAttributes activity command result =
        match result with
        | ResourceResult resourceDto ->
            Tracing.addAttribute activity "resource.id" resourceDto.Id
            Tracing.addAttribute activity "resource.name" resourceDto.Name
        | ResourcesResult pagedResources ->
            Tracing.addIntAttribute activity "result.count" pagedResources.Resources.Length

            match command with
            | GetAllResources(skip, take) ->
                Tracing.addPaginationAttributes activity skip take (Some pagedResources.Resources.Length)
            | _ -> ()
        | ResourceUnitResult _ -> ()