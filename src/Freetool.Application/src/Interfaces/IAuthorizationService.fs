namespace Freetool.Application.Interfaces

open System.Threading.Tasks

/// Request to create an OpenFGA store
type CreateStoreRequest = { Name: string }

/// Response from creating an OpenFGA store
type StoreResponse = {
    Id: string
    Name: string
}

/// Response from writing an authorization model
type AuthorizationModelResponse = {
    AuthorizationModelId: string
}

/// Represents a relationship tuple (user, relation, object)
type RelationshipTuple = {
    User: string        // e.g., "user:alice" or "team:engineering#admin"
    Relation: string    // e.g., "member", "admin", "create_resource"
    Object: string      // e.g., "team:engineering", "workspace:default"
}

/// Request to update relationships (add and/or remove tuples)
type UpdateRelationshipsRequest = {
    TuplesToAdd: RelationshipTuple list
    TuplesToRemove: RelationshipTuple list
}

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
    abstract member CheckPermissionAsync: user:string -> relation:string -> object:string -> Task<bool>
