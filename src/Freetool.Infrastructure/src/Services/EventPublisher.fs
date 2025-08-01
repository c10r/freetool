namespace Freetool.Infrastructure.Services

open Freetool.Domain
open Freetool.Application.Interfaces

type EventPublisher(eventRepository: IEventRepository) =
    interface IEventPublisher with
        member this.PublishAsync(event: IDomainEvent) = eventRepository.SaveEventAsync(event)