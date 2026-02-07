module Freetool.Infrastructure.Tests.AuditControllerTests

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Xunit
open Freetool.Api.Controllers
open Freetool.Application.DTOs
open Freetool.Application.Interfaces
open Freetool.Application.Services
open Freetool.Domain
open Freetool.Domain.Entities

type MockEventRepository() =
    let mutable lastFilter: EventFilter option = None

    member _.LastFilter = lastFilter

    interface IEventRepository with
        member _.SaveEventAsync(_event: IDomainEvent) = Task.FromResult(())

        member _.CommitAsync() = Task.FromResult(())

        member _.GetEventsAsync(filter: EventFilter) =
            task {
                lastFilter <- Some filter

                return
                    { Items = []
                      TotalCount = 0
                      Skip = filter.Skip
                      Take = filter.Take }
            }

type MockEventEnhancementService() =
    interface IEventEnhancementService with
        member _.EnhanceEventAsync(event: EventData) =
            Task.FromResult(
                { Id = event.Id
                  EventId = event.EventId
                  EventType = event.EventType
                  EntityType = event.EntityType
                  EntityId = event.EntityId
                  EntityName = "Test"
                  EventData = event.EventData
                  OccurredAt = event.OccurredAt
                  CreatedAt = event.CreatedAt
                  UserId = event.UserId
                  UserName = "Test User"
                  EventSummary = "Test summary" }
            )

[<Fact>]
let ``GetAllEvents passes skip and take query params to repository filter`` () : Task =
    task {
        let eventRepository = MockEventRepository()
        let controller = AuditController(eventRepository, MockEventEnhancementService())

        let! result = controller.GetAllEvents(null, null, null, Nullable(), Nullable(), Nullable 2000, Nullable 50)

        let okResult = Assert.IsType<OkObjectResult>(result)
        let payload = Assert.IsType<PagedResult<EnhancedEventData>>(okResult.Value)

        Assert.Equal(2000, payload.Skip)
        Assert.Equal(50, payload.Take)
        Assert.True(eventRepository.LastFilter.IsSome)
        Assert.Equal(2000, eventRepository.LastFilter.Value.Skip)
        Assert.Equal(50, eventRepository.LastFilter.Value.Take)
    }
