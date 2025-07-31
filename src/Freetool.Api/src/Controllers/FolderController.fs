namespace Freetool.Api.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Freetool.Domain
open Freetool.Application.DTOs
open Freetool.Application.Commands
open Freetool.Application.Interfaces
open Freetool.Application.Mappers

[<ApiController>]
[<Route("folder")>]
type FolderController
    (
        folderRepository: IFolderRepository,
        commandHandler: IGenericCommandHandler<IFolderRepository, FolderCommand, FolderCommandResult>
    ) =
    inherit ControllerBase()

    [<HttpPost>]
    member this.CreateFolder([<FromBody>] createDto: CreateFolderDto) : Task<IActionResult> = task {
        match FolderMapper.fromCreateDto createDto with
        | Error domainError -> return this.HandleDomainError(domainError)
        | Ok validatedFolder ->
            let! result = commandHandler.HandleCommand folderRepository (CreateFolder validatedFolder)

            return
                match result with
                | Ok(FolderResult folderDto) ->
                    this.CreatedAtAction(nameof this.GetFolderById, {| id = folderDto.Id |}, folderDto) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
    }

    [<HttpGet("{id}")>]
    member this.GetFolderById(id: string) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand folderRepository (GetFolderById id)

        return
            match result with
            | Ok(FolderResult folderDto) -> this.Ok(folderDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpGet("{id}/children")>]
    member this.GetFolderWithChildren(id: string) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand folderRepository (GetFolderWithChildren id)

        return
            match result with
            | Ok(FolderWithChildrenResult folderWithChildrenDto) -> this.Ok(folderWithChildrenDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpGet("root")>]
    member this.GetRootFolders([<FromQuery>] skip: int, [<FromQuery>] take: int) : Task<IActionResult> = task {
        let skipValue = if skip < 0 then 0 else skip

        let takeValue =
            if take <= 0 then 10
            elif take > 100 then 100
            else take

        let! result = commandHandler.HandleCommand folderRepository (GetRootFolders(skipValue, takeValue))

        return
            match result with
            | Ok(FoldersResult pagedFolders) -> this.Ok(pagedFolders) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpGet("{parentId}/children")>]
    member this.GetChildFolders
        (parentId: string, [<FromQuery>] skip: int, [<FromQuery>] take: int)
        : Task<IActionResult> =
        task {
            let skipValue = if skip < 0 then 0 else skip

            let takeValue =
                if take <= 0 then 10
                elif take > 100 then 100
                else take

            let! result =
                commandHandler.HandleCommand folderRepository (GetChildFolders(parentId, skipValue, takeValue))

            return
                match result with
                | Ok(FoldersResult pagedFolders) -> this.Ok(pagedFolders) :> IActionResult
                | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
                | Error error -> this.HandleDomainError(error)
        }

    [<HttpGet>]
    member this.GetAllFolders([<FromQuery>] skip: int, [<FromQuery>] take: int) : Task<IActionResult> = task {
        let skipValue = if skip < 0 then 0 else skip

        let takeValue =
            if take <= 0 then 10
            elif take > 100 then 100
            else take

        let! result = commandHandler.HandleCommand folderRepository (GetAllFolders(skipValue, takeValue))

        return
            match result with
            | Ok(FoldersResult pagedFolders) -> this.Ok(pagedFolders) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpPut("{id}/name")>]
    member this.UpdateFolderName(id: string, [<FromBody>] updateDto: UpdateFolderNameDto) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand folderRepository (UpdateFolderName(id, updateDto))

        return
            match result with
            | Ok(FolderResult folderDto) -> this.Ok(folderDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpPut("{id}/move")>]
    member this.MoveFolder(id: string, [<FromBody>] moveDto: MoveFolderDto) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand folderRepository (MoveFolder(id, moveDto))

        return
            match result with
            | Ok(FolderResult folderDto) -> this.Ok(folderDto) :> IActionResult
            | Ok _ -> this.StatusCode(500, "Unexpected result type") :> IActionResult
            | Error error -> this.HandleDomainError(error)
    }

    [<HttpDelete("{id}")>]
    member this.DeleteFolder(id: string) : Task<IActionResult> = task {
        let! result = commandHandler.HandleCommand folderRepository (DeleteFolder id)

        return
            match result with
            | Ok(FolderUnitResult _) -> this.NoContent() :> IActionResult
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