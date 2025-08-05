namespace Freetool.Domain.Entities

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events

[<Table("Groups")>]
[<Index([| "Name" |], IsUnique = true, Name = "IX_Groups_Name")>]
type GroupData = {
    [<Key>]
    Id: GroupId

    [<Required>]
    [<MaxLength(100)>]
    Name: string

    [<NotMapped>] // UserIds will be handled via separate UserGroups table
    UserIds: UserId list

    [<Required>]
    CreatedAt: DateTime

    [<Required>]
    UpdatedAt: DateTime

    IsDeleted: bool
}

// Junction entity for many-to-many relationship
[<Table("UserGroups")>]
[<Index([| "UserId"; "GroupId" |], IsUnique = true, Name = "IX_UserGroups_UserId_GroupId")>]
type UserGroupData = {
    [<Key>]
    Id: System.Guid

    [<Required>]
    UserId: System.Guid

    [<Required>]
    GroupId: System.Guid

    [<Required>]
    CreatedAt: DateTime
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
    let create (name: string) (userIds: UserId list option) : Result<ValidatedGroup, DomainError> =
        match name with
        | "" -> Error(ValidationError "Group name cannot be empty")
        | nameValue when nameValue.Length > 100 -> Error(ValidationError "Group name cannot exceed 100 characters")
        | nameValue ->
            // Handle userIds - remove duplicates and default to empty list if None
            let validatedUserIds = userIds |> Option.defaultValue [] |> List.distinct

            let groupData = {
                Id = GroupId.NewId()
                Name = nameValue.Trim()
                UserIds = validatedUserIds
                CreatedAt = DateTime.UtcNow
                UpdatedAt = DateTime.UtcNow
                IsDeleted = false
            }

            let groupCreatedEvent =
                GroupEvents.groupCreated groupData.Id groupData.Name validatedUserIds

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
            // Remove duplicates from UserIds
            let distinctUserIds = groupData.UserIds |> List.distinct

            Ok {
                State = {
                    groupData with
                        Name = nameValue.Trim()
                        UserIds = distinctUserIds
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

    // Helper function to validate UserIds - to be used by application layer
    let validateUserIds (userIds: UserId list) (userExistsFunc: UserId -> bool) : Result<UserId list, DomainError> =
        let distinctUserIds = userIds |> List.distinct

        let invalidUserIds =
            distinctUserIds |> List.filter (fun userId -> not (userExistsFunc userId))

        match invalidUserIds with
        | [] -> Ok distinctUserIds
        | invalidIds ->
            let invalidIdStrings = invalidIds |> List.map (fun id -> id.Value.ToString())

            let message =
                sprintf "The following user IDs do not exist or are deleted: %s" (String.concat ", " invalidIdStrings)

            Error(ValidationError message)