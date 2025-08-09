namespace Freetool.Domain.Entities

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open Freetool.Domain.ValueObjects
open Microsoft.EntityFrameworkCore

[<Table("Events")>]
[<Index([| "EventId" |], IsUnique = true, Name = "IX_Events_EventId")>]
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
    EventType: string

    [<Required>]
    [<Column("EntityType")>]
    EntityType: string

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