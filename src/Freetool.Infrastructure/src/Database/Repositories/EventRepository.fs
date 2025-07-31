namespace Freetool.Infrastructure.Database.Repositories

open System
open System.Linq
open System.Threading.Tasks
open Microsoft.EntityFrameworkCore
open System.Text.Json
open Freetool.Domain
open Freetool.Domain.Events
open Freetool.Application.Interfaces
open Freetool.Application.DTOs
open Freetool.Infrastructure.Database

type EventRepository(context: FreetoolDbContext) =
    interface IEventRepository with
        member this.SaveEventAsync(event: IDomainEvent) = task {
            let entityType =
                event
                    .GetType()
                    .Name.Replace("Event", "")
                    .Replace("Created", "")
                    .Replace("Updated", "")
                    .Replace("Deleted", "")

            let eventType = event.GetType().Name

            // Extract entity ID from the event
            let entityId =
                match event with
                | :? Events.UserCreatedEvent as e -> e.UserId.ToString()
                | :? Events.UserUpdatedEvent as e -> e.UserId.ToString()
                | :? Events.UserDeletedEvent as e -> e.UserId.ToString()
                | :? Events.AppCreatedEvent as e -> e.AppId.ToString()
                | :? Events.AppUpdatedEvent as e -> e.AppId.ToString()
                | :? Events.AppDeletedEvent as e -> e.AppId.ToString()
                | :? Events.ResourceCreatedEvent as e -> e.ResourceId.ToString()
                | :? Events.ResourceUpdatedEvent as e -> e.ResourceId.ToString()
                | :? Events.ResourceDeletedEvent as e -> e.ResourceId.ToString()
                | :? Events.FolderCreatedEvent as e -> e.FolderId.ToString()
                | :? Events.FolderUpdatedEvent as e -> e.FolderId.ToString()
                | :? Events.FolderDeletedEvent as e -> e.FolderId.ToString()
                | _ -> "unknown"

            let eventData = JsonSerializer.Serialize(event)

            let eventEntity = EventEntity()
            eventEntity.Id <- Guid.NewGuid().ToString()
            eventEntity.EventId <- event.EventId.ToString()
            eventEntity.EventType <- eventType
            eventEntity.EntityType <- entityType
            eventEntity.EntityId <- entityId
            eventEntity.EventData <- eventData
            eventEntity.OccurredAt <- event.OccurredAt.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")
            eventEntity.CreatedAt <- DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")

            context.Events.Add(eventEntity) |> ignore
            let! _ = context.SaveChangesAsync()
            return ()
        }

        member this.GetEventsByEntityAsync (entityType: string) (entityId: string) = task {
            let! events =
                context.Events
                    .Where(fun e -> e.EntityType = entityType && e.EntityId = entityId)
                    .OrderBy(fun e -> e.OccurredAt)
                    .ToListAsync()

            return events |> List.ofSeq |> List.map this.MapToDto
        }

        member this.GetAllEventsAsync (skip: int) (take: int) = task {
            let! totalCount = context.Events.CountAsync()

            let! events =
                context.Events
                    .OrderByDescending(fun e -> e.OccurredAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync()

            let eventDtos = events |> List.ofSeq |> List.map this.MapToDto

            return {
                Events = eventDtos
                TotalCount = totalCount
                Skip = skip
                Take = take
            }
        }

        member this.GetEventsByTypeAsync (eventType: string) (skip: int) (take: int) = task {
            let! totalCount = context.Events.Where(fun e -> e.EventType = eventType).CountAsync()

            let! events =
                context.Events
                    .Where(fun e -> e.EventType = eventType)
                    .OrderByDescending(fun e -> e.OccurredAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync()

            let eventDtos = events |> List.ofSeq |> List.map this.MapToDto

            return {
                Events = eventDtos
                TotalCount = totalCount
                Skip = skip
                Take = take
            }
        }

    member private this.MapToDto(entity: EventEntity) : EventDto = {
        Id = entity.Id
        EventId = entity.EventId
        EventType = entity.EventType
        EntityType = entity.EntityType
        EntityId = entity.EntityId
        EventData = entity.EventData
        OccurredAt = DateTime.Parse(entity.OccurredAt)
        CreatedAt = DateTime.Parse(entity.CreatedAt)
    }