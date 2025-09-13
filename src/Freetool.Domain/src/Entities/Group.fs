namespace Freetool.Domain.Entities

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open System.Text.Json.Serialization
open Microsoft.EntityFrameworkCore
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Events

[<Table("Groups")>]
[<Index([| "Name" |], IsUnique = true, Name = "IX_Groups_Name")>]
// CLIMutable for EntityFramework
[<CLIMutable>]
type GroupData =
    { [<Key>]
      Id: GroupId

      [<Required>]
      [<MaxLength(100)>]
      Name: string

      [<Required>]
      [<JsonIgnore>]
      CreatedAt: DateTime

      [<Required>]
      [<JsonIgnore>]
      UpdatedAt: DateTime

      [<JsonIgnore>]
      IsDeleted: bool

      [<NotMapped>]
      [<JsonPropertyName("userIds")>]
      mutable UserIds: UserId list }

// Junction entity for many-to-many relationship
[<Table("UserGroups")>]
[<Index([| "UserId"; "GroupId" |], IsUnique = true, Name = "IX_UserGroups_UserId_GroupId")>]
// CLIMutable for EntityFramework
[<CLIMutable>]
type UserGroupData =
    { [<Key>]
      Id: Guid

      [<Required>]
      UserId: UserId

      [<Required>]
      GroupId: GroupId

      [<Required>]
      [<JsonIgnore>]
      CreatedAt: DateTime }

type Group = EventSourcingAggregate<GroupData>

module GroupAggregateHelpers =
    let getEntityId (group: Group) : GroupId = group.State.Id

    let implementsIEntity (group: Group) =
        { new IEntity<GroupId> with
            member _.Id = group.State.Id }

// Type aliases for clarity
type UnvalidatedGroup = Group // From DTOs - potentially unsafe
type ValidatedGroup = Group // Validated domain model and database data

module Group =
    let create
        (actorUserId: UserId)
        (name: string)
        (userIds: UserId list option)
        : Result<ValidatedGroup, DomainError> =
        match name with
        | "" -> Error(ValidationError "Group name cannot be empty")
        | nameValue when nameValue.Length > 100 -> Error(ValidationError "Group name cannot exceed 100 characters")
        | nameValue ->
            // Handle userIds - remove duplicates and default to empty list if None
            let validatedUserIds = userIds |> Option.defaultValue [] |> List.distinct

            let groupData =
                { Id = GroupId.NewId()
                  Name = nameValue.Trim()
                  CreatedAt = DateTime.UtcNow
                  UpdatedAt = DateTime.UtcNow
                  IsDeleted = false
                  UserIds = validatedUserIds }

            let groupCreatedEvent =
                GroupEvents.groupCreated actorUserId groupData.Id groupData.Name validatedUserIds

            Ok
                { State = groupData
                  UncommittedEvents = [ groupCreatedEvent :> IDomainEvent ] }

    let fromData (groupData: GroupData) : ValidatedGroup =
        { State = groupData
          UncommittedEvents = [] }

    let validate (group: UnvalidatedGroup) : Result<ValidatedGroup, DomainError> =
        let groupData = group.State

        match groupData.Name with
        | "" -> Error(ValidationError "Group name cannot be empty")
        | nameValue when nameValue.Length > 100 -> Error(ValidationError "Group name cannot exceed 100 characters")
        | nameValue ->
            // Remove duplicates from UserIds
            let distinctUserIds = groupData.UserIds |> List.distinct

            let updatedGroupData =
                { groupData with
                    Name = nameValue.Trim()
                    UserIds = distinctUserIds }

            Ok
                { State = updatedGroupData
                  UncommittedEvents = group.UncommittedEvents }

    let updateName
        (actorUserId: UserId)
        (newName: string)
        (group: ValidatedGroup)
        : Result<ValidatedGroup, DomainError> =
        match newName with
        | "" -> Error(ValidationError "Group name cannot be empty")
        | nameValue when nameValue.Length > 100 -> Error(ValidationError "Group name cannot exceed 100 characters")
        | nameValue ->
            let oldName = group.State.Name
            let trimmedName = nameValue.Trim()

            if oldName = trimmedName then
                Ok group // No change needed
            else
                let updatedGroupData =
                    { group.State with
                        Name = trimmedName
                        UpdatedAt = DateTime.UtcNow }

                let nameChangedEvent =
                    GroupEvents.groupUpdated
                        actorUserId
                        group.State.Id
                        [ GroupChange.NameChanged(oldName, trimmedName) ]

                Ok
                    { State = updatedGroupData
                      UncommittedEvents = group.UncommittedEvents @ [ nameChangedEvent :> IDomainEvent ] }

    let addUser (actorUserId: UserId) (userId: UserId) (group: ValidatedGroup) : Result<ValidatedGroup, DomainError> =
        if List.contains userId group.State.UserIds then
            Error(Conflict "User is already a member of this group")
        else
            let updatedGroupData =
                { group.State with
                    UpdatedAt = DateTime.UtcNow
                    UserIds = userId :: group.State.UserIds }

            let userAddedEvent =
                GroupEvents.groupUpdated actorUserId group.State.Id [ GroupChange.UserAdded(userId) ]

            Ok
                { State = updatedGroupData
                  UncommittedEvents = group.UncommittedEvents @ [ userAddedEvent :> IDomainEvent ] }

    let removeUser
        (actorUserId: UserId)
        (userId: UserId)
        (group: ValidatedGroup)
        : Result<ValidatedGroup, DomainError> =
        if not (List.contains userId group.State.UserIds) then
            Error(NotFound "User is not a member of this group")
        else
            let updatedGroupData =
                { group.State with
                    UpdatedAt = DateTime.UtcNow
                    UserIds = List.filter (fun id -> id <> userId) group.State.UserIds }

            let userRemovedEvent =
                GroupEvents.groupUpdated actorUserId group.State.Id [ GroupChange.UserRemoved(userId) ]

            Ok
                { State = updatedGroupData
                  UncommittedEvents = group.UncommittedEvents @ [ userRemovedEvent :> IDomainEvent ] }

    let markForDeletion (actorUserId: UserId) (group: ValidatedGroup) : ValidatedGroup =
        let groupDeletedEvent = GroupEvents.groupDeleted actorUserId group.State.Id

        { group with
            UncommittedEvents = group.UncommittedEvents @ [ groupDeletedEvent :> IDomainEvent ] }

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
