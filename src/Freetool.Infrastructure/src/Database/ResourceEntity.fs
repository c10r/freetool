namespace Freetool.Infrastructure.Database

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open Microsoft.EntityFrameworkCore

[<Table("Resources")>]
[<Index([| "Name" |], IsUnique = true, Name = "IX_Resources_Name")>]
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
    [<MaxLength(1_000)>]
    member val BaseUrl = "" with get, set

    [<Required>]
    [<MaxLength(10)>]
    member val HttpMethod = "GET" with get, set

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

    member val IsDeleted = false with get, set