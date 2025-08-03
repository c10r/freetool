namespace Freetool.Infrastructure.Database

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema

[<Table("Apps")>]
type AppEntity() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    [<Required>]
    [<MaxLength(100)>]
    member val Name = "" with get, set

    [<Required>]
    member val FolderId = Guid.Empty with get, set

    [<Required>]
    member val ResourceId = Guid.Empty with get, set

    [<Required>]
    member val Inputs = "" with get, set // JSON serialized list of inputs

    [<MaxLength(500)>]
    member val UrlPath = "" with get, set

    [<Required>]
    member val UrlParameters = "" with get, set // JSON serialized key-value pairs

    [<Required>]
    member val Headers = "" with get, set // JSON serialized key-value pairs

    [<Required>]
    member val Body = "" with get, set // JSON serialized key-value pairs

    [<Required>]
    member val CreatedAt = DateTime.UtcNow with get, set

    [<Required>]
    member val UpdatedAt = DateTime.UtcNow with get, set

    member val IsDeleted = false with get, set