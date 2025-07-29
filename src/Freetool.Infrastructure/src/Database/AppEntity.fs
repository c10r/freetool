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
    member val Inputs = "" with get, set // JSON serialized list of inputs

    [<Required>]
    member val CreatedAt = DateTime.UtcNow with get, set

    [<Required>]
    member val UpdatedAt = DateTime.UtcNow with get, set