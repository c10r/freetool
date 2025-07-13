namespace Freetool.Application.Interfaces

open System.Threading.Tasks
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities

type IUserRepository =
    abstract member GetByIdAsync: UserId -> Task<User option>

    abstract member GetByEmailAsync: Email -> Task<User option>

    abstract member GetAllAsync: skip: int -> take: int -> Task<User list>

    abstract member AddAsync: User -> Task<Result<unit, DomainError>>

    abstract member UpdateAsync: User -> Task<Result<unit, DomainError>>

    abstract member DeleteAsync: UserId -> Task<Result<unit, DomainError>>

    abstract member ExistsAsync: UserId -> Task<bool>

    abstract member ExistsByEmailAsync: Email -> Task<bool>