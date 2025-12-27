namespace Freetool.Domain.ValueObjects

open System

[<Struct>]
type WorkspaceId =
    | WorkspaceId of Guid

    static member NewId() = WorkspaceId(Guid.NewGuid())

    static member FromGuid(id: Guid) = WorkspaceId(id)

    member this.Value =
        let (WorkspaceId id) = this
        id

    override this.ToString() = this.Value.ToString()
