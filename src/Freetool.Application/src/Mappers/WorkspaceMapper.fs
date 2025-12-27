namespace Freetool.Application.Mappers

open System
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Application.DTOs

module WorkspaceMapper =
    let fromCreateDto (dto: CreateWorkspaceDto) : UnvalidatedWorkspace =
        // Convert string GroupId to GroupId value object
        let groupId =
            match Guid.TryParse dto.GroupId with
            | true, guid -> GroupId.FromGuid guid
            | false, _ -> GroupId.NewId() // This will fail validation later

        { State =
            { Id = WorkspaceId.NewId()
              GroupId = groupId
              CreatedAt = DateTime.UtcNow
              UpdatedAt = DateTime.UtcNow
              IsDeleted = false }
          UncommittedEvents = [] }

    let fromUpdateGroupDto (dto: UpdateWorkspaceGroupDto) (workspace: ValidatedWorkspace) : UnvalidatedWorkspace =
        let newGroupId =
            match Guid.TryParse dto.GroupId with
            | true, guid -> GroupId.FromGuid guid
            | false, _ -> workspace.State.GroupId // Keep existing if invalid

        { State =
            { workspace.State with
                GroupId = newGroupId
                UpdatedAt = DateTime.UtcNow }
          UncommittedEvents = [] }
