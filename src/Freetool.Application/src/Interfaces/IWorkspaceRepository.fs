namespace Freetool.Application.Interfaces

open System.Threading.Tasks
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities

type IWorkspaceRepository =
    abstract member GetByIdAsync: WorkspaceId -> Task<ValidatedWorkspace option>

    abstract member GetByGroupIdAsync: GroupId -> Task<ValidatedWorkspace option>

    abstract member GetAllAsync: skip: int -> take: int -> Task<ValidatedWorkspace list>

    abstract member AddAsync: ValidatedWorkspace -> Task<Result<unit, DomainError>>

    abstract member UpdateAsync: ValidatedWorkspace -> Task<Result<unit, DomainError>>

    abstract member DeleteAsync: ValidatedWorkspace -> Task<Result<unit, DomainError>>

    abstract member ExistsAsync: WorkspaceId -> Task<bool>

    abstract member ExistsByGroupIdAsync: GroupId -> Task<bool>

    abstract member GetCountAsync: unit -> Task<int>
