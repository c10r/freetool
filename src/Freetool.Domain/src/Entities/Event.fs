namespace Freetool.Domain.Entities

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open Freetool.Domain.ValueObjects
open Microsoft.EntityFrameworkCore

type UserEvents =
    | UserCreatedEvent
    | UserUpdatedEvent
    | UserDeletedEvent

type AppEvents =
    | AppCreatedEvent
    | AppUpdatedEvent
    | AppDeletedEvent

type FolderEvents =
    | FolderCreatedEvent
    | FolderUpdatedEvent
    | FolderDeletedEvent

type GroupEvents =
    | GroupCreatedEvent
    | GroupUpdatedEvent
    | GroupDeletedEvent

type ResourceEvents =
    | ResourceCreatedEvent
    | ResourceUpdatedEvent
    | ResourceDeletedEvent

type RunEvents =
    | RunCreatedEvent
    | RunStatusChangedEvent

type EventType =
    | UserEvents of UserEvents
    | AppEvents of AppEvents
    | ResourceEvents of ResourceEvents
    | FolderEvents of FolderEvents
    | GroupEvents of GroupEvents
    | RunEvents of RunEvents

type EntityType =
    | User
    | App
    | Resource
    | Folder
    | Group
    | Run

module EventTypeConverter =
    let toString (eventType: EventType) : string =
        match eventType with
        | UserEvents userEvent ->
            match userEvent with
            | UserCreatedEvent -> "UserCreatedEvent"
            | UserUpdatedEvent -> "UserUpdatedEvent"
            | UserDeletedEvent -> "UserDeletedEvent"
        | AppEvents appEvent ->
            match appEvent with
            | AppCreatedEvent -> "AppCreatedEvent"
            | AppUpdatedEvent -> "AppUpdatedEvent"
            | AppDeletedEvent -> "AppDeletedEvent"
        | ResourceEvents resourceEvent ->
            match resourceEvent with
            | ResourceCreatedEvent -> "ResourceCreatedEvent"
            | ResourceUpdatedEvent -> "ResourceUpdatedEvent"
            | ResourceDeletedEvent -> "ResourceDeletedEvent"
        | FolderEvents folderEvent ->
            match folderEvent with
            | FolderCreatedEvent -> "FolderCreatedEvent"
            | FolderUpdatedEvent -> "FolderUpdatedEvent"
            | FolderDeletedEvent -> "FolderDeletedEvent"
        | GroupEvents groupEvent ->
            match groupEvent with
            | GroupCreatedEvent -> "GroupCreatedEvent"
            | GroupUpdatedEvent -> "GroupUpdatedEvent"
            | GroupDeletedEvent -> "GroupDeletedEvent"
        | RunEvents runEvent ->
            match runEvent with
            | RunCreatedEvent -> "RunCreatedEvent"
            | RunStatusChangedEvent -> "RunStatusChangedEvent"

    let fromString (str: string) : EventType option =
        match str with
        | "UserCreatedEvent" -> Some(UserEvents UserCreatedEvent)
        | "UserUpdatedEvent" -> Some(UserEvents UserUpdatedEvent)
        | "UserDeletedEvent" -> Some(UserEvents UserDeletedEvent)
        | "AppCreatedEvent" -> Some(AppEvents AppCreatedEvent)
        | "AppUpdatedEvent" -> Some(AppEvents AppUpdatedEvent)
        | "AppDeletedEvent" -> Some(AppEvents AppDeletedEvent)
        | "ResourceCreatedEvent" -> Some(ResourceEvents ResourceCreatedEvent)
        | "ResourceUpdatedEvent" -> Some(ResourceEvents ResourceUpdatedEvent)
        | "ResourceDeletedEvent" -> Some(ResourceEvents ResourceDeletedEvent)
        | "FolderCreatedEvent" -> Some(FolderEvents FolderCreatedEvent)
        | "FolderUpdatedEvent" -> Some(FolderEvents FolderUpdatedEvent)
        | "FolderDeletedEvent" -> Some(FolderEvents FolderDeletedEvent)
        | "GroupCreatedEvent" -> Some(GroupEvents GroupCreatedEvent)
        | "GroupUpdatedEvent" -> Some(GroupEvents GroupUpdatedEvent)
        | "GroupDeletedEvent" -> Some(GroupEvents GroupDeletedEvent)
        | "RunCreatedEvent" -> Some(RunEvents RunCreatedEvent)
        | "RunStatusChangedEvent" -> Some(RunEvents RunStatusChangedEvent)
        | _ -> None

module EntityTypeConverter =
    let toString (entityType: EntityType) : string =
        match entityType with
        | User -> "User"
        | App -> "App"
        | Resource -> "Resource"
        | Folder -> "Folder"
        | Group -> "Group"
        | Run -> "Run"

    let fromString (str: string) : EntityType option =
        match str with
        | "User" -> Some User
        | "App" -> Some App
        | "Resource" -> Some Resource
        | "Folder" -> Some Folder
        | "Group" -> Some Group
        | "Run" -> Some Run
        | _ -> None

[<Table("Events")>]
[<Index([| "EventId" |], IsUnique = true, Name = "IX_Events_EventId")>]
[<Index([| "UserId" |], Name = "IX_Events_UserId")>]
[<Index([| "EventType" |], Name = "IX_Events_EventType")>]
[<Index([| "EntityType" |], Name = "IX_Events_EntityType")>]
[<Index([| "OccurredAt" |], Name = "IX_Events_OccurredAt")>]
// CLIMutable for EntityFramework
[<CLIMutable>]
type EventData = {
    [<Key>]
    [<Column("Id")>]
    Id: Guid

    [<Required>]
    [<Column("EventId")>]
    EventId: string

    [<Required>]
    [<Column("EventType")>]
    EventType: EventType

    [<Required>]
    [<Column("EntityType")>]
    EntityType: EntityType

    [<Required>]
    [<Column("EntityId")>]
    EntityId: string

    [<Required>]
    [<Column("EventData")>]
    EventData: string

    [<Required>]
    [<Column("OccurredAt")>]
    OccurredAt: DateTime

    [<Required>]
    [<Column("CreatedAt")>]
    CreatedAt: DateTime

    [<Required>]
    [<Column("UserId")>]
    UserId: UserId
}