namespace Freetool.Infrastructure.Database

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema

[<Table("Resources")>]
type ResourceEntity() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    [<Required>]
    [<MaxLength(100)>]
    member val Name = "" with get, set

    [<Required>]
    [<MaxLength(500)>]
    member val Description = "" with get, set

    [<Required>]
    [<MaxLength(1000)>]
    member val BaseUrl = "" with get, set

    [<Required>]
    member val UrlParameters = "[]" with get, set // JSON string

    [<Required>]
    member val Headers = "[]" with get, set // JSON string

    [<Required>]
    member val Body = "[]" with get, set // JSON string

    [<Required>]
    member val CreatedAt = DateTime.UtcNow with get, set

    [<Required>]
    member val UpdatedAt = DateTime.UtcNow with get, set