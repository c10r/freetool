namespace Freetool.Domain.Events

open System
open Freetool.Domain
open Freetool.Domain.ValueObjects

type Input = {
    Title: string
    Type: InputType
    Required: bool
}

type AppChange =
    | NameChanged of oldValue: AppName * newValue: AppName
    | InputsChanged of oldInputs: Input list * newInputs: Input list
    | FolderChanged of oldFolderId: FolderId option * newFolderId: FolderId option

type AppCreatedEvent = {
    AppId: AppId
    Name: AppName
    FolderId: FolderId option
    Inputs: Input list
    OccurredAt: DateTime
    EventId: Guid
} with

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId

type AppUpdatedEvent = {
    AppId: AppId
    Changes: AppChange list
    OccurredAt: DateTime
    EventId: Guid
} with

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId

type AppDeletedEvent = {
    AppId: AppId
    OccurredAt: DateTime
    EventId: Guid
} with

    interface IDomainEvent with
        member this.OccurredAt = this.OccurredAt
        member this.EventId = this.EventId

module AppEvents =
    let appCreated (appId: AppId) (name: AppName) (folderId: FolderId option) (inputs: Input list) =
        {
            AppId = appId
            Name = name
            FolderId = folderId
            Inputs = inputs
            OccurredAt = DateTime.UtcNow
            EventId = Guid.NewGuid()
        }
        : AppCreatedEvent

    let appUpdated (appId: AppId) (changes: AppChange list) =
        {
            AppId = appId
            Changes = changes
            OccurredAt = DateTime.UtcNow
            EventId = Guid.NewGuid()
        }
        : AppUpdatedEvent

    let appDeleted (appId: AppId) =
        {
            AppId = appId
            OccurredAt = DateTime.UtcNow
            EventId = Guid.NewGuid()
        }
        : AppDeletedEvent