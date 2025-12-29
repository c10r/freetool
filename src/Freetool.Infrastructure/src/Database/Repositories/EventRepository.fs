namespace Freetool.Infrastructure.Database.Repositories

open System
open System.Linq
open Microsoft.EntityFrameworkCore
open System.Text.Json
open System.Text.Json.Serialization
open Freetool.Domain
open Freetool.Domain.Events
open Freetool.Application.Interfaces
open Freetool.Application.DTOs
open Freetool.Domain.Entities
open Freetool.Infrastructure.Database

type EventRepository(context: FreetoolDbContext) =
    interface IEventRepository with
        member this.SaveEventAsync(event: IDomainEvent) =
            task {
                let eventTypeName = event.GetType().Name

                let eventType =
                    EventTypeConverter.fromString eventTypeName
                    |> Option.defaultWith (fun () -> failwith $"Unknown event type: {eventTypeName}")

                let entityType =
                    match eventType with
                    | UserEvents _ -> EntityType.User
                    | AppEvents _ -> EntityType.App
                    | ResourceEvents _ -> EntityType.Resource
                    | FolderEvents _ -> EntityType.Folder
                    | GroupEvents _ -> EntityType.Group
                    | RunEvents _ -> EntityType.Run
                    | WorkspaceEvents _ -> EntityType.Workspace

                let jsonOptions = JsonSerializerOptions()
                jsonOptions.DefaultIgnoreCondition <- JsonIgnoreCondition.WhenWritingNull
                jsonOptions.Converters.Add(JsonFSharpConverter())

                let (entityId, eventData) =
                    match event with
                    | :? Events.UserCreatedEvent as e -> (e.UserId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.UserUpdatedEvent as e -> (e.UserId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.UserDeletedEvent as e -> (e.UserId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.UserInvitedEvent as e -> (e.UserId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.UserActivatedEvent as e ->
                        (e.UserId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.AppCreatedEvent as e -> (e.AppId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.AppUpdatedEvent as e -> (e.AppId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.AppDeletedEvent as e -> (e.AppId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.ResourceCreatedEvent as e ->
                        (e.ResourceId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.ResourceUpdatedEvent as e ->
                        (e.ResourceId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.ResourceDeletedEvent as e ->
                        (e.ResourceId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.FolderCreatedEvent as e ->
                        (e.FolderId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.FolderUpdatedEvent as e ->
                        (e.FolderId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.FolderDeletedEvent as e ->
                        (e.FolderId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.GroupCreatedEvent as e ->
                        (e.GroupId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.GroupUpdatedEvent as e ->
                        (e.GroupId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.GroupDeletedEvent as e ->
                        (e.GroupId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.RunCreatedEvent as e -> (e.RunId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.RunStatusChangedEvent as e ->
                        (e.RunId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.WorkspaceCreatedEvent as e ->
                        (e.WorkspaceId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.WorkspaceUpdatedEvent as e ->
                        (e.WorkspaceId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | :? Events.WorkspaceDeletedEvent as e ->
                        (e.WorkspaceId.ToString(), JsonSerializer.Serialize(e, jsonOptions))
                    | _ -> ("unknown", JsonSerializer.Serialize(event, jsonOptions))

                // Temporary debug logging
                System.Console.WriteLine($"=== SERIALIZED EVENT DATA ===")
                System.Console.WriteLine(eventData)
                System.Console.WriteLine($"=============================\n")

                let eventDataRecord: Entities.EventData =
                    { Id = Guid.NewGuid()
                      EventId = event.EventId.ToString()
                      EventType = eventType
                      EntityType = entityType
                      EntityId = entityId
                      EventData = eventData
                      OccurredAt = event.OccurredAt
                      CreatedAt = DateTime.UtcNow
                      UserId = event.UserId }

                context.Events.Add(eventDataRecord) |> ignore
                return ()
            }

        member this.GetEventsAsync(filter: EventFilter) : Threading.Tasks.Task<PagedResult<EventData>> =
            task {
                let query = context.Events.AsQueryable()

                // Apply filters
                let filteredQuery =
                    query
                    |> fun q ->
                        match filter.UserId with
                        | Some userId -> q.Where(fun e -> e.UserId = userId)
                        | None -> q
                    |> fun q ->
                        match filter.EventType with
                        | Some eventType -> q.Where(fun e -> e.EventType = eventType)
                        | None -> q
                    |> fun q ->
                        match filter.EntityType with
                        | Some entityType -> q.Where(fun e -> e.EntityType = entityType)
                        | None -> q
                    |> fun q ->
                        match filter.FromDate with
                        | Some fromDate -> q.Where(fun e -> e.OccurredAt >= fromDate)
                        | None -> q
                    |> fun q ->
                        match filter.ToDate with
                        | Some toDate -> q.Where(fun e -> e.OccurredAt <= toDate)
                        | None -> q

                // Get total count before pagination
                let! totalCount = filteredQuery.CountAsync()

                // Apply pagination and ordering (most recent first)
                let! items =
                    filteredQuery
                        .OrderByDescending(fun e -> e.OccurredAt)
                        .Skip(filter.Skip)
                        .Take(filter.Take)
                        .ToListAsync()

                return
                    { Items = items |> List.ofSeq
                      TotalCount = totalCount
                      Skip = filter.Skip
                      Take = filter.Take }
            }
