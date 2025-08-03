namespace Freetool.Infrastructure.Database

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open Microsoft.EntityFrameworkCore

[<Table("Folders")>]
[<Index([| "Name"; "ParentId" |], IsUnique = true, Name = "IX_Folders_Name_ParentId")>]
type FolderEntity() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    [<Required>]
    [<MaxLength(100)>]
    member val Name = "" with get, set

    member val ParentId: Nullable<Guid> = Nullable<Guid>() with get, set

    [<Required>]
    member val CreatedAt = DateTime.UtcNow with get, set

    [<Required>]
    member val UpdatedAt = DateTime.UtcNow with get, set

    member val IsDeleted = false with get, set