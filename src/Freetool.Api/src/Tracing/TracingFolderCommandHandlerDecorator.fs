namespace Freetool.Api.Tracing

open System
open System.Diagnostics
open Freetool.Application.Commands
open Freetool.Application.Interfaces
open Freetool.Domain.Entities

type TracingFolderCommandHandlerDecorator
    (
        inner: IGenericCommandHandler<IFolderRepository, FolderCommand, FolderCommandResult>,
        activitySource: ActivitySource
    ) =

    interface IGenericCommandHandler<IFolderRepository, FolderCommand, FolderCommandResult> with
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
        | CreateFolder _ -> "folder.create"
        | GetFolderById _ -> "folder.get_by_id"
        | GetFolderWithChildren _ -> "folder.get_with_children"
        | GetRootFolders _ -> "folder.get_root"
        | GetChildFolders _ -> "folder.get_children"
        | GetAllFolders _ -> "folder.get_all"
        | UpdateFolderName _ -> "folder.update_name"
        | MoveFolder _ -> "folder.move"
        | DeleteFolder _ -> "folder.delete"

    member private this.AddCommandAttributes activity command =
        match command with
        | CreateFolder folder ->
            Tracing.addAttribute activity "folder.name" (Folder.getName folder)
            Tracing.addAttribute activity "operation.type" "create"

            match Folder.getParentId folder with
            | Some parentId -> Tracing.addAttribute activity "folder.parent_id" (parentId.ToString())
            | None -> Tracing.addAttribute activity "folder.is_root" "true"
        | GetFolderById id ->
            Tracing.addAttribute activity "folder.id" id
            Tracing.addAttribute activity "operation.type" "read"
        | GetFolderWithChildren id ->
            Tracing.addAttribute activity "folder.id" id
            Tracing.addAttribute activity "operation.type" "read"
            Tracing.addAttribute activity "read.include_children" "true"
        | GetRootFolders(skip, take) ->
            Tracing.addPaginationAttributes activity skip take None
            Tracing.addAttribute activity "operation.type" "read"
            Tracing.addAttribute activity "folder.type" "root"
        | GetChildFolders(parentId, skip, take) ->
            Tracing.addAttribute activity "folder.parent_id" (parentId.ToString())
            Tracing.addPaginationAttributes activity skip take None
            Tracing.addAttribute activity "operation.type" "read"
            Tracing.addAttribute activity "folder.type" "children"
        | GetAllFolders(skip, take) ->
            Tracing.addPaginationAttributes activity skip take None
            Tracing.addAttribute activity "operation.type" "read"
        | UpdateFolderName(id, dto) ->
            Tracing.addAttribute activity "folder.id" id
            Tracing.addAttribute activity "operation.type" "update"
            Tracing.addAttribute activity "update.field" "name"
            Tracing.addAttribute activity "update.value" dto.Name
        | MoveFolder(id, dto) ->
            Tracing.addAttribute activity "folder.id" id
            Tracing.addAttribute activity "operation.type" "update"
            Tracing.addAttribute activity "update.field" "parent"

            if String.IsNullOrEmpty(dto.ParentId) then
                Tracing.addAttribute activity "update.move_to_root" "true"
            else
                Tracing.addAttribute activity "update.new_parent_id" dto.ParentId
        | DeleteFolder id ->
            Tracing.addAttribute activity "folder.id" id
            Tracing.addAttribute activity "operation.type" "delete"

    member private this.AddSuccessAttributes activity command result =
        match result with
        | FolderResult folderDto ->
            Tracing.addAttribute activity "folder.id" folderDto.Id
            Tracing.addAttribute activity "folder.name" folderDto.Name
        | FolderWithChildrenResult folderDto ->
            Tracing.addAttribute activity "folder.id" folderDto.Id
            Tracing.addAttribute activity "folder.name" folderDto.Name
            Tracing.addIntAttribute activity "folder.children_count" folderDto.Children.Length
        | FoldersResult pagedFolders ->
            Tracing.addIntAttribute activity "result.count" pagedFolders.Folders.Length

            match command with
            | GetRootFolders(skip, take)
            | GetChildFolders(_, skip, take)
            | GetAllFolders(skip, take) ->
                Tracing.addPaginationAttributes activity skip take (Some pagedFolders.Folders.Length)
            | _ -> ()
        | FolderUnitResult _ -> ()