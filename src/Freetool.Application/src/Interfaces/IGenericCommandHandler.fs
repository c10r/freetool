namespace Freetool.Application.Interfaces

open System.Threading.Tasks
open Freetool.Domain

type IGenericCommandHandler<'TRepository, 'TCommand, 'TResult> =
    abstract member HandleCommand: 'TRepository -> 'TCommand -> Task<Result<'TResult, DomainError>>