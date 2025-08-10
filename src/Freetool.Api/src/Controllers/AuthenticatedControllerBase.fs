namespace Freetool.Api.Controllers

open Microsoft.AspNetCore.Mvc
open Freetool.Domain.ValueObjects

[<ApiController>]
type AuthenticatedControllerBase() =
    inherit ControllerBase()

    member this.CurrentUserId: UserId =
        match this.HttpContext.Items.TryGetValue "UserId" with
        | true, userIdObj when userIdObj <> null -> userIdObj :?> UserId
        | _ -> failwith "User not authenticated"

    [<NonAction>]
    member this.TryGetCurrentUserId() : UserId option =
        match this.HttpContext.Items.TryGetValue "UserId" with
        | true, userIdObj when userIdObj <> null -> Some(userIdObj :?> UserId)
        | _ -> None