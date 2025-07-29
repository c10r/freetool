namespace Freetool.Application.Handlers

open System
open System.Threading.Tasks
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces
open Freetool.Application.Commands
open Freetool.Application.Mappers

module FolderHandler =

    let private mapFolderToDto = FolderMapper.toDto

    let handleCommand
        (folderRepository: IFolderRepository)
        (command: FolderCommand)
        : Task<Result<FolderCommandResult, DomainError>> =
        task {
            match command with
            | CreateFolder validatedFolder ->
                // Check if a folder with the same name already exists in the parent
                let folderName =
                    FolderName.Create(Folder.getName validatedFolder)
                    |> function
                        | Ok n -> n
                        | Error _ -> failwith "ValidatedFolder should have valid name"

                let parentId = Folder.getParentId validatedFolder
                let! existsByNameInParent = folderRepository.ExistsByNameInParentAsync folderName parentId

                if existsByNameInParent then
                    return Error(Conflict "A folder with this name already exists in the parent directory")
                else
                    // If has parent, validate parent exists
                    match parentId with
                    | None ->
                        // Root folder - proceed with creation
                        match! folderRepository.AddAsync validatedFolder with
                        | Error error -> return Error error
                        | Ok() -> return Ok(FolderResult(mapFolderToDto validatedFolder))
                    | Some parentFolderId ->
                        // Check if parent exists
                        let! parentExists = folderRepository.ExistsAsync parentFolderId

                        if not parentExists then
                            return Error(NotFound "Parent folder not found")
                        else
                            match! folderRepository.AddAsync validatedFolder with
                            | Error error -> return Error error
                            | Ok() -> return Ok(FolderResult(mapFolderToDto validatedFolder))

            | DeleteFolder folderId ->
                match Guid.TryParse folderId with
                | false, _ -> return Error(ValidationError "Invalid folder ID format")
                | true, guid ->
                    let folderIdObj = FolderId.FromGuid guid
                    let! folderOption = folderRepository.GetByIdAsync folderIdObj

                    match folderOption with
                    | None -> return Error(NotFound "Folder not found")
                    | Some folder ->
                        // Check if folder has children
                        let! children = folderRepository.GetChildrenAsync folderIdObj

                        if not (List.isEmpty children) then
                            return Error(Conflict "Cannot delete folder that contains subfolders")
                        else
                            match! folderRepository.DeleteAsync folderIdObj with
                            | Error error -> return Error error
                            | Ok() -> return Ok(FolderUnitResult())

            | UpdateFolderName(folderId, dto) ->
                match Guid.TryParse folderId with
                | false, _ -> return Error(ValidationError "Invalid folder ID format")
                | true, guid ->
                    let folderIdObj = FolderId.FromGuid guid
                    let! folderOption = folderRepository.GetByIdAsync folderIdObj

                    match folderOption with
                    | None -> return Error(NotFound "Folder not found")
                    | Some folder ->
                        // Check if the new name already exists in the same parent (only if it's different from current name)
                        if Folder.getName folder <> dto.Name then
                            match FolderName.Create(dto.Name) with
                            | Error error -> return Error error
                            | Ok newFolderName ->
                                let parentId = Folder.getParentId folder

                                let! existsByNameInParent =
                                    folderRepository.ExistsByNameInParentAsync newFolderName parentId

                                if existsByNameInParent then
                                    return
                                        Error(Conflict "A folder with this name already exists in the parent directory")
                                else
                                    match FolderMapper.fromUpdateNameDto dto folder with
                                    | Error error -> return Error error
                                    | Ok validatedFolder ->
                                        match! folderRepository.UpdateAsync validatedFolder with
                                        | Error error -> return Error error
                                        | Ok() -> return Ok(FolderResult(mapFolderToDto validatedFolder))
                        else
                            // Name hasn't changed, just update normally
                            match FolderMapper.fromUpdateNameDto dto folder with
                            | Error error -> return Error error
                            | Ok validatedFolder ->
                                match! folderRepository.UpdateAsync validatedFolder with
                                | Error error -> return Error error
                                | Ok() -> return Ok(FolderResult(mapFolderToDto validatedFolder))

            | MoveFolder(folderId, dto) ->
                match Guid.TryParse folderId with
                | false, _ -> return Error(ValidationError "Invalid folder ID format")
                | true, guid ->
                    let folderIdObj = FolderId.FromGuid guid
                    let! folderOption = folderRepository.GetByIdAsync folderIdObj

                    match folderOption with
                    | None -> return Error(NotFound "Folder not found")
                    | Some folder ->
                        // Parse and validate parent ID format first
                        let parseResult =
                            if String.IsNullOrEmpty(dto.ParentId) then
                                Ok(None)
                            else
                                match Guid.TryParse(dto.ParentId) with
                                | true, parentGuid -> Ok(Some(FolderId.FromGuid(parentGuid)))
                                | false, _ -> Error(ValidationError "Invalid parent folder ID format")

                        match parseResult with
                        | Error err -> return Error err
                        | Ok newParentId ->
                            // Validate new parent exists (if specified)
                            match newParentId with
                            | Some parentId ->
                                let! parentExists = folderRepository.ExistsAsync parentId

                                if not parentExists then
                                    return Error(NotFound "Parent folder not found")
                                // Check if moving to a child of itself (circular reference)
                                elif parentId = folderIdObj then
                                    return Error(ValidationError "Cannot move folder to itself")
                                else
                                    // Check if folder name conflicts in new parent
                                    let folderName =
                                        FolderName.Create(Folder.getName folder)
                                        |> function
                                            | Ok n -> n
                                            | Error _ -> failwith "Folder should have valid name"

                                    let! existsByNameInParent =
                                        folderRepository.ExistsByNameInParentAsync folderName newParentId

                                    if existsByNameInParent then
                                        return
                                            Error(
                                                Conflict
                                                    "A folder with this name already exists in the target parent directory"
                                            )
                                    else
                                        let movedFolder = FolderMapper.fromMoveDto dto folder

                                        match! folderRepository.UpdateAsync movedFolder with
                                        | Error error -> return Error error
                                        | Ok() -> return Ok(FolderResult(mapFolderToDto movedFolder))
                            | None ->
                                // Moving to root - check if name conflicts with other root folders
                                let folderName =
                                    FolderName.Create(Folder.getName folder)
                                    |> function
                                        | Ok n -> n
                                        | Error _ -> failwith "Folder should have valid name"

                                let! existsByNameInParent = folderRepository.ExistsByNameInParentAsync folderName None

                                if existsByNameInParent then
                                    return
                                        Error(Conflict "A folder with this name already exists in the root directory")
                                else
                                    let movedFolder = FolderMapper.fromMoveDto dto folder

                                    match! folderRepository.UpdateAsync movedFolder with
                                    | Error error -> return Error error
                                    | Ok() -> return Ok(FolderResult(mapFolderToDto movedFolder))

            | GetFolderById folderId ->
                match Guid.TryParse folderId with
                | false, _ -> return Error(ValidationError "Invalid folder ID format")
                | true, guid ->
                    let folderIdObj = FolderId.FromGuid guid
                    let! folderOption = folderRepository.GetByIdAsync folderIdObj

                    match folderOption with
                    | None -> return Error(NotFound "Folder not found")
                    | Some folder -> return Ok(FolderResult(mapFolderToDto folder))

            | GetFolderWithChildren folderId ->
                match Guid.TryParse folderId with
                | false, _ -> return Error(ValidationError "Invalid folder ID format")
                | true, guid ->
                    let folderIdObj = FolderId.FromGuid guid
                    let! folderOption = folderRepository.GetByIdAsync folderIdObj

                    match folderOption with
                    | None -> return Error(NotFound "Folder not found")
                    | Some folder ->
                        let! children = folderRepository.GetChildrenAsync folderIdObj
                        return Ok(FolderWithChildrenResult(FolderMapper.toWithChildrenDto folder children))

            | GetRootFolders(skip, take) ->
                if skip < 0 then
                    return Error(ValidationError "Skip cannot be negative")
                elif take <= 0 || take > 100 then
                    return Error(ValidationError "Take must be between 1 and 100")
                else
                    let! folders = folderRepository.GetRootFoldersAsync skip take
                    let! totalCount = folderRepository.GetRootCountAsync()
                    let result = FolderMapper.toPagedDto folders totalCount skip take
                    return Ok(FoldersResult result)

            | GetChildFolders(parentId, skip, take) ->
                if skip < 0 then
                    return Error(ValidationError "Skip cannot be negative")
                elif take <= 0 || take > 100 then
                    return Error(ValidationError "Take must be between 1 and 100")
                else
                    match Guid.TryParse parentId with
                    | false, _ -> return Error(ValidationError "Invalid parent folder ID format")
                    | true, guid ->
                        let parentIdObj = FolderId.FromGuid guid
                        let! parentExists = folderRepository.ExistsAsync parentIdObj

                        if not parentExists then
                            return Error(NotFound "Parent folder not found")
                        else
                            let! folders = folderRepository.GetChildFoldersAsync parentIdObj skip take
                            let! totalCount = folderRepository.GetChildCountAsync parentIdObj
                            let result = FolderMapper.toPagedDto folders totalCount skip take
                            return Ok(FoldersResult result)

            | GetAllFolders(skip, take) ->
                if skip < 0 then
                    return Error(ValidationError "Skip cannot be negative")
                elif take <= 0 || take > 100 then
                    return Error(ValidationError "Take must be between 1 and 100")
                else
                    let! folders = folderRepository.GetAllAsync skip take
                    let! totalCount = folderRepository.GetCountAsync()
                    let result = FolderMapper.toPagedDto folders totalCount skip take
                    return Ok(FoldersResult result)
        }

type FolderHandler() =
    interface IGenericCommandHandler<IFolderRepository, FolderCommand, FolderCommandResult> with
        member this.HandleCommand repository command =
            FolderHandler.handleCommand repository command