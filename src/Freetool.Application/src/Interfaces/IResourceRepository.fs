namespace Freetool.Application.Interfaces

open System.Threading.Tasks
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities

type IResourceRepository =
    abstract member GetByIdAsync: ResourceId -> Task<ValidatedResource option>

    abstract member GetAllAsync: skip: int -> take: int -> Task<ValidatedResource list>

    abstract member AddAsync: ValidatedResource -> Task<Result<unit, DomainError>>

    abstract member UpdateAsync: ValidatedResource -> Task<Result<unit, DomainError>>

    abstract member DeleteAsync: ResourceId -> IDomainEvent option -> Task<Result<unit, DomainError>>

    abstract member ExistsAsync: ResourceId -> Task<bool>

    abstract member ExistsByNameAsync: ResourceName -> Task<bool>

    abstract member GetCountAsync: unit -> Task<int>