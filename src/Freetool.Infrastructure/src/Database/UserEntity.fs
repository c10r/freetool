namespace Freetool.Infrastructure.Database

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema

[<Table("Users")>]
type UserEntity() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    [<Required>]
    [<MaxLength(100)>]
    member val Name = "" with get, set

    [<Required>]
    [<MaxLength(254)>]
    member val Email = "" with get, set

    [<MaxLength(2_000)>]
    member val ProfilePicUrl: string option = None with get, set

    [<Required>]
    member val CreatedAt = DateTime.UtcNow with get, set

    [<Required>]
    member val UpdatedAt = DateTime.UtcNow with get, set

    member val IsDeleted = false with get, set