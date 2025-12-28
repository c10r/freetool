namespace Freetool.Application.Interfaces

open System.Threading.Tasks

/// Request to create an OpenFGA store
type CreateStoreRequest = { Name: string }

/// Response from creating an OpenFGA store
type StoreResponse = { Id: string; Name: string }

/// Response from writing an authorization model
type AuthorizationModelResponse = { AuthorizationModelId: string }

/// Represents the subject (user field) in a relationship tuple
type AuthSubject =
    | User of userId: string
    | Team of teamId: string
    | Organization of organizationId: string
    | UserSetFromRelation of objectType: string * objectId: string * relation: string

/// Represents a relation/permission in the authorization model
type AuthRelation =
    // Team relations
    | TeamMember
    | TeamAdmin
    | TeamOrganization
    | TeamRename
    | TeamDelete
    // Workspace relations
    | WorkspaceTeam
    | WorkspaceOrganization
    | WorkspaceCreate
    // Resource permissions
    | ResourceCreate
    | ResourceEdit
    | ResourceDelete
    // App permissions
    | AppCreate
    | AppEdit
    | AppDelete
    | AppRun
    // Folder permissions
    | FolderCreate
    | FolderEdit
    | FolderDelete

/// Represents the object (resource) in a relationship tuple
type AuthObject =
    | UserObject of userId: string
    | TeamObject of teamId: string
    | OrganizationObject of organizationId: string
    | WorkspaceObject of workspaceId: string

/// Helper module for converting between strongly-typed and string representations
module AuthTypes =
    /// Converts an AuthSubject to OpenFGA string format
    let subjectToString (subject: AuthSubject) : string =
        match subject with
        | User userId -> $"user:{userId}"
        | Team teamId -> $"team:{teamId}"
        | Organization orgId -> $"organization:{orgId}"
        | UserSetFromRelation(objectType, objectId, relation) -> $"{objectType}:{objectId}#{relation}"

    /// Converts an AuthRelation to OpenFGA string format
    let relationToString (relation: AuthRelation) : string =
        match relation with
        | TeamMember -> "member"
        | TeamAdmin -> "admin"
        | TeamOrganization -> "organization"
        | TeamRename -> "rename"
        | TeamDelete -> "delete"
        | WorkspaceTeam -> "team"
        | WorkspaceOrganization -> "organization"
        | WorkspaceCreate -> "create_workspace"
        | ResourceCreate -> "create_resource"
        | ResourceEdit -> "edit_resource"
        | ResourceDelete -> "delete_resource"
        | AppCreate -> "create_app"
        | AppEdit -> "edit_app"
        | AppDelete -> "delete_app"
        | AppRun -> "run_app"
        | FolderCreate -> "create_folder"
        | FolderEdit -> "edit_folder"
        | FolderDelete -> "delete_folder"

    /// Converts an AuthObject to OpenFGA string format
    let objectToString (obj: AuthObject) : string =
        match obj with
        | UserObject userId -> $"user:{userId}"
        | TeamObject teamId -> $"team:{teamId}"
        | OrganizationObject orgId -> $"organization:{orgId}"
        | WorkspaceObject workspaceId -> $"workspace:{workspaceId}"

/// Represents a strongly-typed relationship tuple
type RelationshipTuple =
    { Subject: AuthSubject
      Relation: AuthRelation
      Object: AuthObject }

/// Helper module for RelationshipTuple operations
module RelationshipTuple =
    /// Converts a RelationshipTuple to the legacy string-based format for OpenFGA SDK
    let toStrings (tuple: RelationshipTuple) : string * string * string =
        (AuthTypes.subjectToString tuple.Subject,
         AuthTypes.relationToString tuple.Relation,
         AuthTypes.objectToString tuple.Object)

/// Request to update relationships (add and/or remove tuples)
type UpdateRelationshipsRequest =
    { TuplesToAdd: RelationshipTuple list
      TuplesToRemove: RelationshipTuple list }

/// Interface for OpenFGA authorization operations
type IAuthorizationService =
    /// Creates a new OpenFGA store for authorization data
    abstract member CreateStoreAsync: CreateStoreRequest -> Task<StoreResponse>

    /// Writes the authorization model to the store
    abstract member WriteAuthorizationModelAsync: unit -> Task<AuthorizationModelResponse>

    /// Creates new relationship tuple(s)
    abstract member CreateRelationshipsAsync: RelationshipTuple list -> Task<unit>

    /// Updates relationships by adding and/or removing tuples in a single transaction
    abstract member UpdateRelationshipsAsync: UpdateRelationshipsRequest -> Task<unit>

    /// Deletes relationship tuple(s)
    abstract member DeleteRelationshipsAsync: RelationshipTuple list -> Task<unit>

    /// Checks if a user has a specific permission on an object
    abstract member CheckPermissionAsync:
        subject: AuthSubject -> relation: AuthRelation -> object: AuthObject -> Task<bool>
