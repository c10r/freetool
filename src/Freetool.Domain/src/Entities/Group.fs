namespace Freetool.Domain.Entities

open System
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events

type GroupData = {
    Id: GroupId
    Name: string
    UserIds: UserId list
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

type Group = EventSourcingAggregate<GroupData>

module GroupAggregateHelpers =
    let getEntityId (group: Group) : GroupId = group.State.Id

    let implementsIEntity (group: Group) =
        { new IEntity<GroupId> with
            member _.Id = group.State.Id
        }

// Type aliases for clarity
type UnvalidatedGroup = Group // From DTOs - potentially unsafe
type ValidatedGroup = Group // Validated domain model and database data

module Group =
    let create (name: string) : Result<ValidatedGroup, DomainError> =
        match name with
        | "" -> Error(ValidationError "Group name cannot be empty")
        | nameValue when nameValue.Length > 100 -> Error(ValidationError "Group name cannot exceed 100 characters")
        | nameValue ->
            let groupData = {
                Id = GroupId.NewId()
                Name = nameValue.Trim()
                UserIds = []
                CreatedAt = DateTime.UtcNow
                UpdatedAt = DateTime.UtcNow
            }

            let groupCreatedEvent = GroupEvents.groupCreated groupData.Id groupData.Name

            Ok {
                State = groupData
                UncommittedEvents = [ groupCreatedEvent :> IDomainEvent ]
            }

    let fromData (groupData: GroupData) : ValidatedGroup = {
        State = groupData
        UncommittedEvents = []
    }

    let validate (group: UnvalidatedGroup) : Result<ValidatedGroup, DomainError> =
        let groupData = group.State

        match groupData.Name with
        | "" -> Error(ValidationError "Group name cannot be empty")
        | nameValue when nameValue.Length > 100 -> Error(ValidationError "Group name cannot exceed 100 characters")
        | nameValue ->
            Ok {
                State = {
                    groupData with
                        Name = nameValue.Trim()
                }
                UncommittedEvents = group.UncommittedEvents
            }

    let updateName (newName: string) (group: ValidatedGroup) : Result<ValidatedGroup, DomainError> =
        match newName with
        | "" -> Error(ValidationError "Group name cannot be empty")
        | nameValue when nameValue.Length > 100 -> Error(ValidationError "Group name cannot exceed 100 characters")
        | nameValue ->
            let oldName = group.State.Name
            let trimmedName = nameValue.Trim()

            if oldName = trimmedName then
                Ok group // No change needed
            else
                let updatedGroupData = {
                    group.State with
                        Name = trimmedName
                        UpdatedAt = DateTime.UtcNow
                }

                let nameChangedEvent =
                    GroupEvents.groupUpdated group.State.Id [ GroupChange.NameChanged(oldName, trimmedName) ]

                Ok {
                    State = updatedGroupData
                    UncommittedEvents = group.UncommittedEvents @ [ nameChangedEvent :> IDomainEvent ]
                }

    let addUser (userId: UserId) (group: ValidatedGroup) : Result<ValidatedGroup, DomainError> =
        if List.contains userId group.State.UserIds then
            Error(Conflict "User is already a member of this group")
        else
            let updatedGroupData = {
                group.State with
                    UserIds = userId :: group.State.UserIds
                    UpdatedAt = DateTime.UtcNow
            }

            let userAddedEvent =
                GroupEvents.groupUpdated group.State.Id [ GroupChange.UserAdded(userId) ]

            Ok {
                State = updatedGroupData
                UncommittedEvents = group.UncommittedEvents @ [ userAddedEvent :> IDomainEvent ]
            }

    let removeUser (userId: UserId) (group: ValidatedGroup) : Result<ValidatedGroup, DomainError> =
        if not (List.contains userId group.State.UserIds) then
            Error(NotFound "User is not a member of this group")
        else
            let updatedGroupData = {
                group.State with
                    UserIds = List.filter (fun id -> id <> userId) group.State.UserIds
                    UpdatedAt = DateTime.UtcNow
            }

            let userRemovedEvent =
                GroupEvents.groupUpdated group.State.Id [ GroupChange.UserRemoved(userId) ]

            Ok {
                State = updatedGroupData
                UncommittedEvents = group.UncommittedEvents @ [ userRemovedEvent :> IDomainEvent ]
            }

    let markForDeletion (group: ValidatedGroup) : ValidatedGroup =
        let groupDeletedEvent = GroupEvents.groupDeleted group.State.Id

        {
            group with
                UncommittedEvents = group.UncommittedEvents @ [ groupDeletedEvent :> IDomainEvent ]
        }

    let getUncommittedEvents (group: ValidatedGroup) : IDomainEvent list = group.UncommittedEvents

    let markEventsAsCommitted (group: ValidatedGroup) : ValidatedGroup = { group with UncommittedEvents = [] }

    let getId (group: Group) : GroupId = group.State.Id

    let getName (group: Group) : string = group.State.Name

    let getUserIds (group: Group) : UserId list = group.State.UserIds

    let getCreatedAt (group: Group) : DateTime = group.State.CreatedAt

    let getUpdatedAt (group: Group) : DateTime = group.State.UpdatedAt

    let hasUser (userId: UserId) (group: Group) : bool =
        List.contains userId group.State.UserIds