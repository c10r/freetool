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

        /// Initializes the organization with an admin user
        member this.InitializeOrganizationAsync (organizationId: string) (adminUserId: string) : Task<unit> =
            task {
                eprintfn
                    "[DEBUG OpenFGA] InitializeOrganizationAsync called with orgId=%s, userId=%s"
                    organizationId
                    adminUserId

                // Create the organization admin relationship tuple
                let tuple =
                    { Subject = User adminUserId
                      Relation = OrganizationAdmin
                      Object = OrganizationObject organizationId }

                let userStr = AuthTypes.subjectToString tuple.Subject
                let relationStr = AuthTypes.relationToString tuple.Relation
                let objectStr = AuthTypes.objectToString tuple.Object

                eprintfn "[DEBUG OpenFGA] Creating relationship: %s#%s@%s" objectStr relationStr userStr

                do! (this :> IAuthorizationService).CreateRelationshipsAsync([ tuple ])

                eprintfn "[DEBUG OpenFGA] Relationship created successfully"
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

                // Helper to create permission definition: [user, admin from team, admin from organization]
                let createPermissionUserset () =
                    // Team admin check: admin from team relation
                    let teamAdminComputedUserset = ObjectRelation()
                    teamAdminComputedUserset.Relation <- "admin"

                    let teamTupleToUserset = TupleToUserset()
                    teamTupleToUserset.Tupleset <- ObjectRelation(Object = "", Relation = "team")
                    teamTupleToUserset.ComputedUserset <- teamAdminComputedUserset

                    // Organization admin check: admin from organization relation
                    let orgAdminComputedUserset = ObjectRelation()
                    orgAdminComputedUserset.Relation <- "admin"

                    let orgTupleToUserset = TupleToUserset()
                    orgTupleToUserset.Tupleset <- ObjectRelation(Object = "", Relation = "organization")
                    orgTupleToUserset.ComputedUserset <- orgAdminComputedUserset

                    let unionUsersets = Usersets()

                    unionUsersets.Child <-
                        ResizeArray(
                            [ Userset(varThis = obj ()) // Direct assignment
                              Userset(tupleToUserset = teamTupleToUserset) // admin from team
                              Userset(tupleToUserset = orgTupleToUserset) ] // admin from organization
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
                    |> List.map (fun t ->
                        let (user, relation, object) = RelationshipTuple.toStrings t
                        ClientTupleKey(User = user, Relation = relation, Object = object))
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
                    |> List.map (fun t ->
                        let (user, relation, object) = RelationshipTuple.toStrings t
                        ClientTupleKey(User = user, Relation = relation, Object = object))
                    |> ResizeArray

                let deletes =
                    request.TuplesToRemove
                    |> List.map (fun t ->
                        let (user, relation, object) = RelationshipTuple.toStrings t
                        ClientTupleKeyWithoutCondition(User = user, Relation = relation, Object = object))
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
                        let (user, relation, object) = RelationshipTuple.toStrings t
                        ClientTupleKeyWithoutCondition(User = user, Relation = relation, Object = object))
                    |> ResizeArray

                let body = ClientWriteRequest(Deletes = deletes)
                let! _ = client.Write(body)
                return ()
            }

        /// Checks if a user has a specific permission on an object
        member _.CheckPermissionAsync
            (subject: AuthSubject)
            (relation: AuthRelation)
            (object: AuthObject)
            : Task<bool> =
            task {
                use client = createClient ()

                let user = AuthTypes.subjectToString subject
                let relationStr = AuthTypes.relationToString relation
                let objectStr = AuthTypes.objectToString object

                eprintfn "[DEBUG OpenFGA] Checking permission: %s#%s@%s" objectStr relationStr user

                let body =
                    ClientCheckRequest(User = user, Relation = relationStr, Object = objectStr)

                let! response = client.Check(body)
                let allowed = response.Allowed.GetValueOrDefault(false)

                eprintfn "[DEBUG OpenFGA] Permission check response: Allowed=%b" allowed
                return allowed
            }

        /// Checks if a store with the given ID exists
        member _.StoreExistsAsync(storeId: string) : Task<bool> =
            task {
                try
                    use client = createClientWithoutStore ()
                    let request = ClientListStoresRequest()
                    let! response = client.ListStores(request)
                    return response.Stores |> Seq.exists (fun s -> s.Id = storeId)
                with _ ->
                    return false
            }
