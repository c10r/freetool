namespace Freetool.Application.Interfaces

open System.Threading.Tasks
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities

type IGroupRepository =
    abstract member GetByIdAsync: GroupId -> Task<ValidatedGroup option>

    abstract member GetByNameAsync: string -> Task<ValidatedGroup option>

    abstract member GetAllAsync: skip: int -> take: int -> Task<ValidatedGroup list>

    abstract member GetByUserIdAsync: UserId -> Task<ValidatedGroup list>

    abstract member AddAsync: ValidatedGroup -> Task<Result<unit, DomainError>>

    abstract member UpdateAsync: ValidatedGroup -> Task<Result<unit, DomainError>>

    abstract member DeleteAsync: ValidatedGroup -> Task<Result<unit, DomainError>>

    abstract member ExistsAsync: GroupId -> Task<bool>

    abstract member ExistsByNameAsync: string -> Task<bool>

    abstract member GetCountAsync: unit -> Task<int>
