namespace Freetool.Application.Interfaces

open System.Threading.Tasks
open Freetool.Domain

type IEventPublisher =
    abstract member PublishAsync: event: IDomainEvent -> Task<unit>