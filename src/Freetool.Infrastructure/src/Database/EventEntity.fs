namespace Freetool.Infrastructure.Database

open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open Microsoft.EntityFrameworkCore

[<Table("Events")>]
[<Index([| "EventId" |], IsUnique = true, Name = "IX_Events_EventId")>]
type EventEntity() =
    [<Key>]
    [<Column("Id")>]
    member val Id = "" with get, set

    [<Required>]
    [<Column("EventId")>]
    member val EventId = "" with get, set

    [<Required>]
    [<Column("EventType")>]
    member val EventType = "" with get, set

    [<Required>]
    [<Column("EntityType")>]
    member val EntityType = "" with get, set

    [<Required>]
    [<Column("EntityId")>]
    member val EntityId = "" with get, set

    [<Required>]
    [<Column("EventData")>]
    member val EventData = "" with get, set

    [<Required>]
    [<Column("OccurredAt")>]
    member val OccurredAt = "" with get, set

    [<Required>]
    [<Column("CreatedAt")>]
    member val CreatedAt = "" with get, set