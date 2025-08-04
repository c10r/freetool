namespace Freetool.Infrastructure.Database

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open Microsoft.EntityFrameworkCore

[<Table("Runs")>]
type RunEntity() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    [<Required>]
    member val AppId = Guid.Empty with get, set

    [<Required>]
    [<MaxLength(50)>]
    member val Status = "" with get, set

    [<Required>]
    member val InputValues = "" with get, set // JSON serialized list of RunInputValue

    member val ExecutableRequest = "" with get, set // JSON serialized ExecutableHttpRequest (null until composed)

    member val Response = "" with get, set // HTTP response body (null until completed successfully)

    member val ErrorMessage = "" with get, set // Error message (null unless failed)

    member val StartedAt = Nullable<DateTime>() with get, set // When the run was started (null if not started)

    member val CompletedAt = Nullable<DateTime>() with get, set // When the run was completed (null if not completed)

    [<Required>]
    member val CreatedAt = DateTime.UtcNow with get, set