namespace Freetool.Infrastructure.Database

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open Microsoft.EntityFrameworkCore

[<Table("Groups")>]
[<Index([| "Name" |], IsUnique = true, Name = "IX_Groups_Name")>]
type GroupEntity() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    [<Required>]
    [<MaxLength(100)>]
    member val Name = "" with get, set

    [<Required>]
    member val CreatedAt = DateTime.UtcNow with get, set

    [<Required>]
    member val UpdatedAt = DateTime.UtcNow with get, set

    member val IsDeleted = false with get, set

// Junction table for many-to-many relationship between Users and Groups
[<Table("UserGroups")>]
[<Index([| "UserId"; "GroupId" |], IsUnique = true, Name = "IX_UserGroups_UserId_GroupId")>]
type UserGroupEntity() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    [<Required>]
    member val UserId = Guid.Empty with get, set

    [<Required>]
    member val GroupId = Guid.Empty with get, set

    [<Required>]
    member val CreatedAt = DateTime.UtcNow with get, set