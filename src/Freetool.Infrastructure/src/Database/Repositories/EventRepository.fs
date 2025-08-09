namespace Freetool.Infrastructure.Database.Repositories

open System
open System.Linq
open Microsoft.EntityFrameworkCore
open System.Text.Json
open Freetool.Domain
open Freetool.Domain.Events
open Freetool.Application.Interfaces
open Freetool.Application.DTOs
open Freetool.Domain.Entities
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

            let eventDataRecord: Entities.EventData = {
                Id = Guid.NewGuid()
                EventId = event.EventId.ToString()
                EventType = eventType
                EntityType = entityType
                EntityId = entityId
                EventData = eventData
                OccurredAt = event.OccurredAt
                CreatedAt = DateTime.UtcNow
                UserId = event.UserId
            }

            context.Events.Add(eventDataRecord) |> ignore
            let! _ = context.SaveChangesAsync()
            return ()
        }

        member this.GetEventsByEntityAsync (entityType: string) (entityId: string) = task {
            let! events =
                context.Events
                    .Where(fun e -> e.EntityType = entityType && e.EntityId = entityId)
                    .OrderBy(fun e -> e.OccurredAt)
                    .ToListAsync()

            return events |> List.ofSeq
        }

        member this.GetAllEventsAsync (skip: int) (take: int) = task {
            let! totalCount = context.Events.CountAsync()

            let! events =
                context.Events
                    .OrderByDescending(fun e -> e.OccurredAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync()

            return {
                Items = events |> List.ofSeq
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

            return {
                Items = events |> List.ofSeq
                TotalCount = totalCount
                Skip = skip
                Take = take
            }
        }