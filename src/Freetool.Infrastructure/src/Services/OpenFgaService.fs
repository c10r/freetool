namespace Freetool.Infrastructure.Services

open System.Threading.Tasks
open OpenFga.Sdk.Client
open OpenFga.Sdk.Client.Model
open OpenFga.Sdk.Model
open Freetool.Application.Interfaces

/// OpenFGA service implementation for fine-grained authorization
type OpenFgaService(apiUrl: string, ?storeId: string) =

    /// Creates a new OpenFGA client instance without store ID (for store creation)
    let createClientWithoutStore () =
        let configuration = ClientConfiguration(ApiUrl = apiUrl)
        new OpenFgaClient(configuration)

    /// Creates a new OpenFGA client instance with store ID (for store operations)
    let createClient () =
        let configuration = ClientConfiguration(ApiUrl = apiUrl)

        // Set store ID if provided
        match storeId with
        | Some id -> configuration.StoreId <- id
        | None -> ()

        new OpenFgaClient(configuration)

    interface IAuthorizationService with
        /// Creates a new OpenFGA store
        member _.CreateStoreAsync(request: CreateStoreRequest) : Task<StoreResponse> =
            task {
                use client = createClientWithoutStore ()
                let createRequest = ClientCreateStoreRequest(Name = request.Name)
                let! response = client.CreateStore(createRequest)

                return
                    { Id = response.Id
                      Name = response.Name }
            }

        /// Writes the authorization model to the store
        member _.WriteAuthorizationModelAsync() : Task<AuthorizationModelResponse> =
            task {
                use client = createClient ()

                // Define the authorization model
                let typeDefinitions = ResizeArray<TypeDefinition>()

                // Type: user (base type, no relations needed)
                typeDefinitions.Add(TypeDefinition(Type = "user"))

                // Type: organization (for global admins)
                let orgRelations = System.Collections.Generic.Dictionary<string, Userset>()
                orgRelations.["admin"] <- Userset(varThis = obj ())

                let orgMetadata = System.Collections.Generic.Dictionary<string, RelationMetadata>()

                orgMetadata.["admin"] <-
                    RelationMetadata(DirectlyRelatedUserTypes = ResizeArray([ RelationReference(Type = "user") ]))

                typeDefinitions.Add(
                    TypeDefinition(
                        Type = "organization",
                        Relations = orgRelations,
                        Metadata = Metadata(relations = orgMetadata)
                    )
                )

                // Type: team (with members and admins, rename/delete restricted to org admins)
                let teamRelations = System.Collections.Generic.Dictionary<string, Userset>()
                teamRelations.["member"] <- Userset(varThis = obj ())
                teamRelations.["admin"] <- Userset(varThis = obj ())
                teamRelations.["organization"] <- Userset(varThis = obj ())

                // Only organization admins can rename or delete teams
                let orgAdminComputedUserset = ObjectRelation()
                orgAdminComputedUserset.Relation <- "admin"

                let orgTupleToUserset = TupleToUserset()
                orgTupleToUserset.Tupleset <- ObjectRelation(Object = "", Relation = "organization")
                orgTupleToUserset.ComputedUserset <- orgAdminComputedUserset

                teamRelations.["rename"] <- Userset(tupleToUserset = orgTupleToUserset)
                teamRelations.["delete"] <- Userset(tupleToUserset = orgTupleToUserset)

                let teamMetadata = System.Collections.Generic.Dictionary<string, RelationMetadata>()

                teamMetadata.["member"] <-
                    RelationMetadata(DirectlyRelatedUserTypes = ResizeArray([ RelationReference(Type = "user") ]))

                teamMetadata.["admin"] <-
                    RelationMetadata(
                        DirectlyRelatedUserTypes =
                            ResizeArray(
                                [ RelationReference(Type = "user")
                                  RelationReference(Type = "organization", Relation = "admin") ]
                            )
                    )

                teamMetadata.["organization"] <-
                    RelationMetadata(
                        DirectlyRelatedUserTypes = ResizeArray([ RelationReference(Type = "organization") ])
                    )

                typeDefinitions.Add(
                    TypeDefinition(
                        Type = "team",
                        Relations = teamRelations,
                        Metadata = Metadata(relations = teamMetadata)
                    )
                )

                // Type: workspace (with team ownership and 7 permissions)
                let workspaceRelations = System.Collections.Generic.Dictionary<string, Userset>()
                workspaceRelations.["team"] <- Userset(varThis = obj ())
                workspaceRelations.["organization"] <- Userset(varThis = obj ())

                // Only organization admins can create workspaces
                let orgAdminCreateUserset = ObjectRelation()
                orgAdminCreateUserset.Relation <- "admin"

                let orgCreateTupleToUserset = TupleToUserset()
                orgCreateTupleToUserset.Tupleset <- ObjectRelation(Object = "", Relation = "organization")
                orgCreateTupleToUserset.ComputedUserset <- orgAdminCreateUserset

                workspaceRelations.["create_workspace"] <- Userset(tupleToUserset = orgCreateTupleToUserset)

                // Helper to create permission definition: [user, organization#admin] or admin from team
                let createPermissionUserset () =
                    let computedUserset = ObjectRelation()
                    computedUserset.Relation <- "admin"

                    let tupleToUserset = TupleToUserset()
                    tupleToUserset.Tupleset <- ObjectRelation(Object = "", Relation = "team")
                    tupleToUserset.ComputedUserset <- computedUserset

                    let unionUsersets = Usersets()

                    unionUsersets.Child <-
                        ResizeArray(
                            [ Userset(varThis = obj ()) // Direct assignment
                              Userset(tupleToUserset = tupleToUserset) ] // admin from team
                        )

                    Userset(union = unionUsersets)

                // Add all 10 permissions with the same pattern
                for permission in
                    [ "create_resource"
                      "edit_resource"
                      "delete_resource"
                      "create_app"
                      "edit_app"
                      "delete_app"
                      "run_app"
                      "create_folder"
                      "edit_folder"
                      "delete_folder" ] do
                    workspaceRelations.[permission] <- createPermissionUserset ()

                let workspaceMetadata =
                    System.Collections.Generic.Dictionary<string, RelationMetadata>()

                workspaceMetadata.["team"] <-
                    RelationMetadata(DirectlyRelatedUserTypes = ResizeArray([ RelationReference(Type = "team") ]))

                workspaceMetadata.["organization"] <-
                    RelationMetadata(
                        DirectlyRelatedUserTypes = ResizeArray([ RelationReference(Type = "organization") ])
                    )

                for permission in
                    [ "create_resource"
                      "edit_resource"
                      "delete_resource"
                      "create_app"
                      "edit_app"
                      "delete_app"
                      "run_app"
                      "create_folder"
                      "edit_folder"
                      "delete_folder" ] do
                    workspaceMetadata.[permission] <-
                        RelationMetadata(
                            DirectlyRelatedUserTypes =
                                ResizeArray(
                                    [ RelationReference(Type = "user")
                                      RelationReference(Type = "organization", Relation = "admin") ]
                                )
                        )

                typeDefinitions.Add(
                    TypeDefinition(
                        Type = "workspace",
                        Relations = workspaceRelations,
                        Metadata = Metadata(relations = workspaceMetadata)
                    )
                )

                let body =
                    ClientWriteAuthorizationModelRequest(SchemaVersion = "1.1", TypeDefinitions = typeDefinitions)

                let! response = client.WriteAuthorizationModel(body)

                return { AuthorizationModelId = response.AuthorizationModelId }
            }

        /// Creates new relationship tuple(s)
        member _.CreateRelationshipsAsync(tuples: RelationshipTuple list) : Task<unit> =
            task {
                use client = createClient ()

                let writes =
                    tuples
                    |> List.map (fun t -> ClientTupleKey(User = t.User, Relation = t.Relation, Object = t.Object))
                    |> ResizeArray

                let body = ClientWriteRequest(Writes = writes)
                let! _ = client.Write(body)
                return ()
            }

        /// Updates relationships by adding and/or removing tuples in a single transaction
        member _.UpdateRelationshipsAsync(request: UpdateRelationshipsRequest) : Task<unit> =
            task {
                use client = createClient ()

                let writes =
                    request.TuplesToAdd
                    |> List.map (fun t -> ClientTupleKey(User = t.User, Relation = t.Relation, Object = t.Object))
                    |> ResizeArray

                let deletes =
                    request.TuplesToRemove
                    |> List.map (fun t ->
                        ClientTupleKeyWithoutCondition(User = t.User, Relation = t.Relation, Object = t.Object))
                    |> ResizeArray

                let body = ClientWriteRequest(Writes = writes, Deletes = deletes)

                let! _ = client.Write(body)
                return ()
            }

        /// Deletes relationship tuple(s)
        member _.DeleteRelationshipsAsync(tuples: RelationshipTuple list) : Task<unit> =
            task {
                use client = createClient ()

                let deletes =
                    tuples
                    |> List.map (fun t ->
                        ClientTupleKeyWithoutCondition(User = t.User, Relation = t.Relation, Object = t.Object))
                    |> ResizeArray

                let body = ClientWriteRequest(Deletes = deletes)
                let! _ = client.Write(body)
                return ()
            }

        /// Checks if a user has a specific permission on an object
        member _.CheckPermissionAsync (user: string) (relation: string) (object: string) : Task<bool> =
            task {
                use client = createClient ()

                let body = ClientCheckRequest(User = user, Relation = relation, Object = object)

                let! response = client.Check(body)
                return response.Allowed.GetValueOrDefault(false)
            }
