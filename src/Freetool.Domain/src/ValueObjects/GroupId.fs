namespace Freetool.Domain.ValueObjects

open System

[<Struct>]
type GroupId =
    | GroupId of Guid

    static member NewId() = GroupId(Guid.NewGuid())

    static member FromGuid(id: Guid) = GroupId(id)

    member this.Value =
        let (GroupId id) = this
        id

    override this.ToString() = this.Value.ToString()