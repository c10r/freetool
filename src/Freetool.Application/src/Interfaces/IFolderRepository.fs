namespace Freetool.Application.Interfaces

open System.Threading.Tasks
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities

type IFolderRepository =
    abstract member GetByIdAsync: FolderId -> Task<ValidatedFolder option>

    abstract member GetChildrenAsync: FolderId -> Task<ValidatedFolder list>

    abstract member GetRootFoldersAsync: skip: int -> take: int -> Task<ValidatedFolder list>

    abstract member GetAllAsync: skip: int -> take: int -> Task<ValidatedFolder list>

    abstract member GetByWorkspaceAsync:
        workspaceId: WorkspaceId -> skip: int -> take: int -> Task<ValidatedFolder list>

    abstract member AddAsync: ValidatedFolder -> Task<Result<unit, DomainError>>

    abstract member UpdateAsync: ValidatedFolder -> Task<Result<unit, DomainError>>

    abstract member DeleteAsync: ValidatedFolder -> Task<Result<unit, DomainError>>

    abstract member ExistsAsync: FolderId -> Task<bool>

    abstract member ExistsByNameInParentAsync: FolderName -> FolderId option -> Task<bool>

    abstract member GetCountAsync: unit -> Task<int>

    abstract member GetCountByWorkspaceAsync: workspaceId: WorkspaceId -> Task<int>

    abstract member GetRootCountAsync: unit -> Task<int>

    abstract member GetChildCountAsync: FolderId -> Task<int>
