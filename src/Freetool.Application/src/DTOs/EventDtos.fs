namespace Freetool.Application.DTOs

open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open System
open System.ComponentModel.DataAnnotations

[<CLIMutable>]
type EventFilterDTO =
    { UserId: string option
      EventType: string option
      EntityType: string option
      FromDate: DateTime option
      ToDate: DateTime option

      [<Range(0, 2147483647)>]
      Skip: int option

      [<Range(0, 100)>]
      Take: int option }

type EventFilter =
    { UserId: UserId option
      EventType: EventType option
      EntityType: EntityType option
      FromDate: DateTime option
      ToDate: DateTime option
      Skip: int
      Take: int }

module EventFilterValidator =
    let validate (dto: EventFilterDTO) : Result<EventFilter, string list> =
        let mutable errors = []
        let mutable userId = None
        let mutable eventType = None
        let mutable entityType = None

        // Validate UserId
        match dto.UserId with
        | Some userIdStr ->
            match Guid.TryParse(userIdStr) with
            | true, guid -> userId <- Some(UserId.FromGuid guid)
            | false, _ -> errors <- "Invalid UserId format - must be a valid GUID" :: errors
        | None -> ()

        // Validate EventType
        match dto.EventType with
        | Some eventTypeStr ->
            match EventTypeConverter.fromString eventTypeStr with
            | Some et -> eventType <- Some et
            | None -> errors <- $"Invalid EventType: {eventTypeStr}" :: errors
        | None -> ()

        // Validate EntityType
        match dto.EntityType with
        | Some entityTypeStr ->
            match EntityTypeConverter.fromString entityTypeStr with
            | Some et -> entityType <- Some et
            | None -> errors <- $"Invalid EntityType: {entityTypeStr}" :: errors
        | None -> ()

        // Validate pagination
        match dto.Skip with
        | Some skip when skip < 0 -> errors <- "Skip must be greater than or equal to 0" :: errors
        | _ -> ()

        match dto.Take with
        | Some take when take < 0 || take > 100 -> errors <- "Take must be between 0 and 100" :: errors
        | _ -> ()

        if List.isEmpty errors then
            Ok
                { UserId = userId
                  EventType = eventType
                  EntityType = entityType
                  FromDate = dto.FromDate
                  ToDate = dto.ToDate
                  Skip = dto.Skip |> Option.defaultValue 0
                  Take = dto.Take |> Option.defaultValue 50 }
        else
            Error(List.rev errors)

[<CLIMutable>]
type EnhancedEventData =
    { Id: Guid
      EventId: string
      EventType: EventType
      EntityType: EntityType
      EntityId: string
      EntityName: string
      EventData: string
      OccurredAt: DateTime
      CreatedAt: DateTime
      UserId: UserId
      UserName: string
      EventSummary: string }
