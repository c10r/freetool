namespace Freetool.Api.Tracing

open System.Diagnostics
open Freetool.Application.Commands
open Freetool.Application.Interfaces

type TracingCommandHandlerDecorator(inner: ICommandHandler, activitySource: ActivitySource) =

    interface ICommandHandler with
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
        | CreateUser _ -> "user.create"
        | GetUserById _ -> "user.get_by_id"
        | GetUserByEmail _ -> "user.get_by_email"
        | GetAllUsers _ -> "user.get_all"
        | UpdateUserName _ -> "user.update_name"
        | UpdateUserEmail _ -> "user.update_email"
        | SetProfilePicture _ -> "user.set_profile_picture"
        | RemoveProfilePicture _ -> "user.remove_profile_picture"
        | DeleteUser _ -> "user.delete"

    member private this.AddCommandAttributes activity command =
        match command with
        | CreateUser dto ->
            Tracing.addUserAttributes activity None (Some dto.Email)
            Tracing.addAttribute activity "operation.type" "create"
        | GetUserById id ->
            Tracing.addUserAttributes activity (Some id) None
            Tracing.addAttribute activity "operation.type" "read"
        | GetUserByEmail email ->
            Tracing.addUserAttributes activity None (Some email)
            Tracing.addAttribute activity "operation.type" "read"
        | GetAllUsers(skip, take) ->
            Tracing.addPaginationAttributes activity skip take None
            Tracing.addAttribute activity "operation.type" "read"
        | UpdateUserName(id, name) ->
            Tracing.addUserAttributes activity (Some id) None
            Tracing.addAttribute activity "operation.type" "update"
            Tracing.addAttribute activity "update.field" "name"
            Tracing.addAttribute activity "update.value" name
        | UpdateUserEmail(id, email) ->
            Tracing.addUserAttributes activity (Some id) None
            Tracing.addAttribute activity "operation.type" "update"
            Tracing.addAttribute activity "update.field" "email"
            Tracing.addAttribute activity "update.value" email
        | SetProfilePicture(id, url) ->
            Tracing.addUserAttributes activity (Some id) None
            Tracing.addAttribute activity "operation.type" "update"
            Tracing.addAttribute activity "profile_picture.url" url
        | RemoveProfilePicture id ->
            Tracing.addUserAttributes activity (Some id) None
            Tracing.addAttribute activity "operation.type" "update"
        | DeleteUser id ->
            Tracing.addUserAttributes activity (Some id) None
            Tracing.addAttribute activity "operation.type" "delete"

    member private this.AddSuccessAttributes activity command result =
        match result with
        | UserResult userDto ->
            Tracing.addUserAttributes activity (Some userDto.Id) (Some userDto.Email)

            match command with
            | GetAllUsers _ -> ()
            | _ -> ()
        | UsersResult pagedUsers ->
            Tracing.addIntAttribute activity "result.count" pagedUsers.Users.Length

            match command with
            | GetAllUsers(skip, take) ->
                Tracing.addPaginationAttributes activity skip take (Some pagedUsers.Users.Length)
            | _ -> ()
        | UnitResult _ -> ()