namespace Freetool.Application.Commands

open Freetool.Domain.Entities
open Freetool.Application.DTOs

type FolderCommandResult =
    | FolderResult of FolderDto
    | FolderWithChildrenResult of FolderWithChildrenDto
    | FoldersResult of PagedFoldersDto
    | FolderUnitResult of unit

type FolderCommand =
    | CreateFolder of ValidatedFolder
    | GetFolderById of folderId: string
    | GetFolderWithChildren of folderId: string
    | GetRootFolders of skip: int * take: int
    | GetChildFolders of parentId: string * skip: int * take: int
    | GetAllFolders of skip: int * take: int
    | DeleteFolder of folderId: string
    | UpdateFolderName of folderId: string * UpdateFolderNameDto
    | MoveFolder of folderId: string * MoveFolderDto