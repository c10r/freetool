---
name: event-sourcing-audit
description: Explain and implement Freetool backend event sourcing and audit logging patterns across Domain, Application, Infrastructure, and API layers. Use when adding or reviewing domain events, wiring audit log persistence/enrichment, extending audit endpoints, or adding new event/entity types that must appear correctly in the audit UI.
---

# Event Sourcing Audit

## Overview
Use this skill to implement or review event sourcing and audit logging in Freetool's backend with correct hexagonal boundaries and complete wiring.

Treat this as the source-of-truth workflow when changing any of:
- Domain events in `src/Freetool.Domain/src/Events/`
- Event type/entity type registries in `src/Freetool.Domain/src/Entities/Event.fs`
- Event persistence in `src/Freetool.Infrastructure/src/Database/Repositories/EventRepository.fs`
- Audit enrichment/summary logic in `src/Freetool.Application/src/Services/EventEnhancementService.fs`
- Audit API responses in `src/Freetool.Api/src/Controllers/AuditController.fs`

## Architecture Boundaries

### Domain (core business facts)
Own:
- Event contracts (records implementing `IDomainEvent`) in `src/Freetool.Domain/src/Events/*.fs`
- Aggregate event collection via `EventSourcingAggregate<'T>` in `src/Freetool.Domain/src/EventSourcingAggregate.fs`
- Business methods that append uncommitted events (entity modules in `src/Freetool.Domain/src/Entities/*.fs`)
- Type registries (`EventType`, `EntityType`, converters) in `src/Freetool.Domain/src/Entities/Event.fs`

Do not:
- Persist events
- Query repositories
- Build UI-facing text summaries

### Application (orchestration + read-model enrichment)
Own:
- Command handlers invoking domain methods and repositories
- Audit enrichment service (`IEventEnhancementService`) that turns raw `EventData` into user-friendly output
- Event summary text generation and entity name lookup strategy

Do not:
- Define domain invariants in handlers
- Put EF/storage concerns here

### Infrastructure (ports/adapters, persistence)
Own:
- `IEventRepository` implementation in `src/Freetool.Infrastructure/src/Database/Repositories/EventRepository.fs`
- Mapping runtime domain events to `EventData` rows
- Transactional persistence with aggregate repositories (`AppRepository`, `UserRepository`, etc.)
- EF converters for `EventType` / `EntityType` in `src/Freetool.Infrastructure/src/Database/FreetoolDbContext.fs`

Do not:
- Generate user-facing summary strings
- Encode business rules that belong in domain methods

### API (transport)
Own:
- Endpoints in `src/Freetool.Api/src/Controllers/AuditController.fs`
- DI wiring in `src/Freetool.Api/src/Program.fs`

Do not:
- Parse/interpret event payloads in controllers

## End-to-End Flow
1. Domain method executes and appends an `IDomainEvent` to `UncommittedEvents`.
2. Application handler calls repository.
3. Aggregate repository saves entity state and events in the same EF unit-of-work, then calls `SaveChangesAsync` once.
4. Audit API reads raw `EventData` via `IEventRepository`.
5. `EventEnhancementService` enriches each row with `EntityName`, `UserName`, and `EventSummary`.

## Core Implementation Pattern

### 1) Domain event contract
```fsharp
// src/Freetool.Domain/src/Events/WidgetEvents.fs
namespace Freetool.Domain.Events

open System
open Freetool.Domain
open Freetool.Domain.ValueObjects

type WidgetCreatedEvent =
    { WidgetId: WidgetId
      Name: WidgetName
      OccurredAt: DateTime
      EventId: Guid
      ActorUserId: UserId }
    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId
        member this.UserId = this.ActorUserId
```

### 2) Aggregate appends event
```fsharp
// src/Freetool.Domain/src/Entities/Widget.fs
let rename (actorUserId: UserId) (newName: string) (widget: ValidatedWidget) =
    // validate + update state
    let updated = { widget.State with Name = newName; UpdatedAt = DateTime.UtcNow }
    let evt = WidgetEvents.widgetUpdated actorUserId widget.State.Id [ WidgetChange.NameChanged(...) ]

    Ok { State = updated
         UncommittedEvents = widget.UncommittedEvents @ [ evt :> IDomainEvent ] }
```

### 3) Repository persists entity + events atomically
```fsharp
// src/Freetool.Infrastructure/src/Database/Repositories/WidgetRepository.fs
member _.UpdateAsync(widget: ValidatedWidget) =
    task {
        // update tracked entity
        let events = Widget.getUncommittedEvents widget

        for event in events do
            do! eventRepository.SaveEventAsync event

        let! _ = context.SaveChangesAsync()
        return Ok()
    }
```

### 4) Audit enrichment builds human-readable output
```fsharp
// src/Freetool.Application/src/Services/EventEnhancementService.fs
| WidgetEvents widgetEvent ->
    match widgetEvent with
    | WidgetCreatedEvent ->
        let data = JsonSerializer.Deserialize<WidgetCreatedEvent>(eventData, jsonOptions)
        return data.Name.Value
    | WidgetUpdatedEvent ->
        let data = JsonSerializer.Deserialize<WidgetUpdatedEvent>(eventData, jsonOptions)
        let! widget = widgetRepository.GetByIdAsync data.WidgetId
        return widget |> Option.map Widget.getName |> Option.defaultValue $"Widget {data.WidgetId.Value}"
```

## Add a New Event Type (existing entity)
Use this checklist in order.

1. Add event record(s) and constructor helpers in `src/Freetool.Domain/src/Events/<Entity>Events.fs`.
2. Emit the event in domain entity methods in `src/Freetool.Domain/src/Entities/<Entity>.fs`.
3. Register new DU case mapping in `src/Freetool.Domain/src/Entities/Event.fs`:
- `EventType` DU
- `EventTypeConverter.toString`
- `EventTypeConverter.fromString`
4. Update event persistence pattern match in `src/Freetool.Infrastructure/src/Database/Repositories/EventRepository.fs`:
- Add typed case
- Return correct `(entityId, serializedEventData)`
5. Update audit enrichment in `src/Freetool.Application/src/Services/EventEnhancementService.fs`:
- Add entity-name extraction case
- Add `generateEventSummary` case
6. If enrichment needs a new repository, add it to DI in `src/Freetool.Api/src/Program.fs` and constructor args for `EventEnhancementService`.
7. Update frontend unions in `www/src/features/space/components/AuditLogView.tsx` if event/entity types changed.
8. Add tests:
- Domain event emission test
- Event repository serialization/type mapping test
- Event enhancement parsing/name/summary tests

## Add a New Auditable Entity Type
Do everything above plus:
1. Add new event family DU in `src/Freetool.Domain/src/Entities/Event.fs` (for example `| WidgetEvents of WidgetEvents`).
2. Add new entity type to `EntityType` DU and converter functions.
3. Extend entity type mapping in `EventRepository.SaveEventAsync`:
```fsharp
let entityType =
    match eventType with
    | WidgetEvents _ -> EntityType.Widget
    | ...
```
4. Extend EF conversion coverage (usually already generic via converters, but verify in `src/Freetool.Infrastructure/src/Database/FreetoolDbContext.fs`).
5. Ensure frontend `EntityType` union includes the new type.

## Gotchas
- `EventTypeConverter.fromString` must match runtime `event.GetType().Name`. If names diverge, save fails with `Unknown event type`.
- Keep `JsonFSharpConverter()` for event payload serialization/deserialization. Removing it breaks F# DU/option parsing.
- Do not omit null option fields during event JSON serialization. `EventRepository` intentionally does not set `WhenWritingNull` because missing fields can break deserialization.
- For `UpdatedEvent`, resolve current entity name through repository lookup in `EventEnhancementService`.
- For `DeletedEvent`, include entity name inside the event payload itself; repository lookup may fail after deletion.
- Always add `generateEventSummary` cases, or UI summaries become inconsistent.
- Keep transactional semantics: aggregate repositories should call `SaveEventAsync` for each uncommitted event and a single `SaveChangesAsync`.
- Exception pattern: some non-aggregate operations (for example, permission-change events in `SpaceHandler`) call `eventRepository.SaveEventAsync` + `CommitAsync` directly. Use this only for standalone events not tied to an aggregate write.
- Update any frontend type unions for new event/entity names, or audit pages may fail type checks.
- If old DB rows contain legacy JSON shapes, enrichment can show parse-error fallback text; handle compatibility or refresh dev data.

## Copy-Paste Plan Template
```markdown
1. Add domain event type(s) in `src/Freetool.Domain/src/Events/<Entity>Events.fs`.
2. Emit event(s) from domain methods in `src/Freetool.Domain/src/Entities/<Entity>.fs`.
3. Register event strings in `src/Freetool.Domain/src/Entities/Event.fs` (`EventType`, `toString`, `fromString`).
4. Extend `src/Freetool.Infrastructure/src/Database/Repositories/EventRepository.fs` mapping:
   - runtime event match
   - entity id extraction
   - entity type assignment
5. Extend `src/Freetool.Application/src/Services/EventEnhancementService.fs`:
   - entity name extraction
   - event summary text
6. Wire any new repositories required by enhancement in `src/Freetool.Api/src/Program.fs`.
7. Update frontend audit type unions in `www/src/features/space/components/AuditLogView.tsx`.
8. Add/adjust tests in:
   - `src/Freetool.Domain/test/`
   - `src/Freetool.Infrastructure/test/EventRepositoryTests.fs`
   - `src/Freetool.Application/test/EventEnhancementServiceTests.fs`
9. Run tests and lint/format before commit.
```

## Validation Commands
```bash
# Backend tests most relevant to audit/event changes
dotnet test src/Freetool.Application/test/Freetool.Application.Tests.fsproj --filter "FullyQualifiedName~EventEnhancementService"
dotnet test src/Freetool.Infrastructure/test/Freetool.Infrastructure.Tests.fsproj --filter "FullyQualifiedName~EventRepository"

# Full safety pass
dotnet test Freetool.sln
```
