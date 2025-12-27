module Freetool.Infrastructure.Tests.OpenFgaServiceTests

open System
open System.Threading.Tasks
open Xunit
open Freetool.Application.Interfaces
open Freetool.Infrastructure.Services

// Test configuration - assumes OpenFGA is running on localhost:8090
let openFgaApiUrl = "http://localhost:8090"

// Helper to create a test service without a store ID (for store creation)
let createServiceWithoutStore () =
    OpenFgaService(openFgaApiUrl) :> IAuthorizationService

// Helper to create a test service with a store ID
let createServiceWithStore storeId =
    OpenFgaService(openFgaApiUrl, storeId) :> IAuthorizationService

[<Fact>]
let ``CreateStoreAsync creates a new store successfully`` () : Task =
    task {
        // Arrange
        let service = createServiceWithoutStore ()
        let storeName = $"test-store-{Guid.NewGuid()}"

        // Act
        let! result = service.CreateStoreAsync({ Name = storeName })

        // Assert
        Assert.NotNull(result)
        Assert.NotEmpty(result.Id)
        Assert.Equal(storeName, result.Name)
    }

[<Fact>]
let ``WriteAuthorizationModelAsync writes the model successfully`` () : Task =
    task {
        // Arrange - Create a store first
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-model-store-{Guid.NewGuid()}" })

        let service = createServiceWithStore storeResponse.Id

        // Act
        let! modelResponse = service.WriteAuthorizationModelAsync()

        // Assert
        Assert.NotNull(modelResponse)
        Assert.NotEmpty(modelResponse.AuthorizationModelId)
    }

[<Fact>]
let ``CreateRelationshipsAsync creates tuples successfully`` () : Task =
    task {
        // Arrange - Create store and write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-tuples-store-{Guid.NewGuid()}" })

        let service = createServiceWithStore storeResponse.Id
        let! _ = service.WriteAuthorizationModelAsync()

        let tuples =
            [ { User = "user:alice"
                Relation = "member"
                Object = "team:engineering" } ]

        // Act
        do! service.CreateRelationshipsAsync(tuples)

        // Assert - If no exception is thrown, the operation succeeded
        Assert.True(true)
    }

[<Fact>]
let ``DeleteRelationshipsAsync removes tuples successfully`` () : Task =
    task {
        // Arrange - Create store, write model, and add a tuple
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-delete-store-{Guid.NewGuid()}" })

        let service = createServiceWithStore storeResponse.Id
        let! _ = service.WriteAuthorizationModelAsync()

        let tuple =
            { User = "user:bob"
              Relation = "member"
              Object = "team:engineering" }

        do! service.CreateRelationshipsAsync([ tuple ])

        // Act
        do! service.DeleteRelationshipsAsync([ tuple ])

        // Assert - If no exception is thrown, the operation succeeded
        Assert.True(true)
    }

[<Fact>]
let ``UpdateRelationshipsAsync adds and removes tuples atomically`` () : Task =
    task {
        // Arrange - Create store, write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-update-store-{Guid.NewGuid()}" })

        let service = createServiceWithStore storeResponse.Id
        let! _ = service.WriteAuthorizationModelAsync()

        // First add Carol as a member
        let memberTuple =
            { User = "user:carol"
              Relation = "member"
              Object = "team:engineering" }

        do! service.CreateRelationshipsAsync([ memberTuple ])

        let adminTuple =
            { User = "user:carol"
              Relation = "admin"
              Object = "team:engineering" }

        // Act - Promote Carol from member to admin
        do!
            service.UpdateRelationshipsAsync(
                { TuplesToAdd = [ adminTuple ]
                  TuplesToRemove = [ memberTuple ] }
            )

        // Assert - Verify Carol is now an admin
        let! isAdmin = service.CheckPermissionAsync "user:carol" "admin" "team:engineering"
        Assert.True(isAdmin)

        // Verify Carol is no longer just a member
        let! isMember = service.CheckPermissionAsync "user:carol" "member" "team:engineering"
        Assert.False(isMember)
    }

[<Fact>]
let ``CheckPermissionAsync returns true for granted permission`` () : Task =
    task {
        // Arrange - Create store, write model, and add permission
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-check-granted-{Guid.NewGuid()}" })

        let service = createServiceWithStore storeResponse.Id
        let! _ = service.WriteAuthorizationModelAsync()

        let tuple =
            { User = "user:dave"
              Relation = "member"
              Object = "team:engineering" }

        do! service.CreateRelationshipsAsync([ tuple ])

        // Act
        let! hasPermission = service.CheckPermissionAsync "user:dave" "member" "team:engineering"

        // Assert
        Assert.True(hasPermission)
    }

[<Fact>]
let ``CheckPermissionAsync returns false for denied permission`` () : Task =
    task {
        // Arrange - Create store and write model (no tuples)
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-check-denied-{Guid.NewGuid()}" })

        let service = createServiceWithStore storeResponse.Id
        let! _ = service.WriteAuthorizationModelAsync()

        // Act
        let! hasPermission = service.CheckPermissionAsync "user:eve" "admin" "team:engineering"

        // Assert
        Assert.False(hasPermission)
    }

[<Fact>]
let ``Team admin has all workspace permissions`` () : Task =
    task {
        // Arrange - Create store, write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-admin-perms-{Guid.NewGuid()}" })

        let service = createServiceWithStore storeResponse.Id
        let! _ = service.WriteAuthorizationModelAsync()

        // Make Frank a team admin
        do!
            service.CreateRelationshipsAsync(
                [ { User = "user:frank"
                    Relation = "admin"
                    Object = "team:engineering" } ]
            )

        // Associate workspace with team
        do!
            service.CreateRelationshipsAsync(
                [ { User = "team:engineering"
                    Relation = "team"
                    Object = "workspace:main" } ]
            )

        // Act & Assert - Verify Frank has all 7 permissions
        let! canCreateResource = service.CheckPermissionAsync "user:frank" "create_resource" "workspace:main"
        Assert.True(canCreateResource, "Team admin should have create_resource permission")

        let! canEditResource = service.CheckPermissionAsync "user:frank" "edit_resource" "workspace:main"
        Assert.True(canEditResource, "Team admin should have edit_resource permission")

        let! canDeleteResource = service.CheckPermissionAsync "user:frank" "delete_resource" "workspace:main"
        Assert.True(canDeleteResource, "Team admin should have delete_resource permission")

        let! canCreateApp = service.CheckPermissionAsync "user:frank" "create_app" "workspace:main"
        Assert.True(canCreateApp, "Team admin should have create_app permission")

        let! canEditApp = service.CheckPermissionAsync "user:frank" "edit_app" "workspace:main"
        Assert.True(canEditApp, "Team admin should have edit_app permission")

        let! canDeleteApp = service.CheckPermissionAsync "user:frank" "delete_app" "workspace:main"
        Assert.True(canDeleteApp, "Team admin should have delete_app permission")

        let! canRunApp = service.CheckPermissionAsync "user:frank" "run_app" "workspace:main"
        Assert.True(canRunApp, "Team admin should have run_app permission")
    }

[<Fact>]
let ``Global admin has all workspace permissions`` () : Task =
    task {
        // Arrange - Create store, write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-global-admin-{Guid.NewGuid()}" })

        let service = createServiceWithStore storeResponse.Id
        let! _ = service.WriteAuthorizationModelAsync()

        // Make Grace a global admin
        do!
            service.CreateRelationshipsAsync(
                [ { User = "user:grace"
                    Relation = "admin"
                    Object = "organization:acme" } ]
            )

        // Associate workspace with team
        do!
            service.CreateRelationshipsAsync(
                [ { User = "team:sales"
                    Relation = "team"
                    Object = "workspace:sales-dashboard" } ]
            )

        // Grant global admins all permissions on the workspace
        do!
            service.CreateRelationshipsAsync(
                [ { User = "organization:acme#admin"
                    Relation = "create_resource"
                    Object = "workspace:sales-dashboard" }
                  { User = "organization:acme#admin"
                    Relation = "run_app"
                    Object = "workspace:sales-dashboard" } ]
            )

        // Act & Assert - Verify Grace has all permissions on any workspace
        let! canCreateResource = service.CheckPermissionAsync "user:grace" "create_resource" "workspace:sales-dashboard"
        Assert.True(canCreateResource, "Global admin should have create_resource permission")

        let! canRunApp = service.CheckPermissionAsync "user:grace" "run_app" "workspace:sales-dashboard"
        Assert.True(canRunApp, "Global admin should have run_app permission")
    }

[<Fact>]
let ``Team member with granted permission can access resource`` () : Task =
    task {
        // Arrange - Create store, write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-member-perm-{Guid.NewGuid()}" })

        let service = createServiceWithStore storeResponse.Id
        let! _ = service.WriteAuthorizationModelAsync()

        // Make Henry a team member
        do!
            service.CreateRelationshipsAsync(
                [ { User = "user:henry"
                    Relation = "member"
                    Object = "team:engineering" } ]
            )

        // Grant Henry specific permission on workspace
        do!
            service.CreateRelationshipsAsync(
                [ { User = "user:henry"
                    Relation = "run_app"
                    Object = "workspace:main" } ]
            )

        // Act
        let! canRunApp = service.CheckPermissionAsync "user:henry" "run_app" "workspace:main"
        let! canEditApp = service.CheckPermissionAsync "user:henry" "edit_app" "workspace:main"

        // Assert
        Assert.True(canRunApp, "User should have explicitly granted run_app permission")
        Assert.False(canEditApp, "User should NOT have edit_app permission (not granted)")
    }

[<Fact>]
let ``Only organization admin can create workspaces`` () : Task =
    task {
        // Arrange - Create store, write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-workspace-create-{Guid.NewGuid()}" })

        let service = createServiceWithStore storeResponse.Id
        let! _ = service.WriteAuthorizationModelAsync()

        // Make Alice an organization admin
        do!
            service.CreateRelationshipsAsync(
                [ { User = "user:alice"
                    Relation = "admin"
                    Object = "organization:acme" } ]
            )

        // Associate workspace with organization
        do!
            service.CreateRelationshipsAsync(
                [ { User = "organization:acme"
                    Relation = "organization"
                    Object = "workspace:new-workspace" } ]
            )

        // Act & Assert - Verify Alice can create workspaces
        let! canCreate = service.CheckPermissionAsync "user:alice" "create_workspace" "workspace:new-workspace"
        Assert.True(canCreate, "Organization admin should be able to create workspaces")
    }

[<Fact>]
let ``Team admin cannot create workspaces`` () : Task =
    task {
        // Arrange - Create store, write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-team-no-create-{Guid.NewGuid()}" })

        let service = createServiceWithStore storeResponse.Id
        let! _ = service.WriteAuthorizationModelAsync()

        // Make Bob a team admin (NOT an org admin)
        do!
            service.CreateRelationshipsAsync(
                [ { User = "user:bob"
                    Relation = "admin"
                    Object = "team:engineering" } ]
            )

        // Associate workspace with organization
        do!
            service.CreateRelationshipsAsync(
                [ { User = "organization:acme"
                    Relation = "organization"
                    Object = "workspace:new-workspace" } ]
            )

        // Act & Assert - Verify Bob CANNOT create workspaces
        let! canCreate = service.CheckPermissionAsync "user:bob" "create_workspace" "workspace:new-workspace"
        Assert.False(canCreate, "Team admin should NOT be able to create workspaces (only org admin can)")
    }

[<Fact>]
let ``Only organization admin can rename teams`` () : Task =
    task {
        // Arrange - Create store, write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-team-rename-{Guid.NewGuid()}" })

        let service = createServiceWithStore storeResponse.Id
        let! _ = service.WriteAuthorizationModelAsync()

        // Make Carol an organization admin
        do!
            service.CreateRelationshipsAsync(
                [ { User = "user:carol"
                    Relation = "admin"
                    Object = "organization:acme" } ]
            )

        // Associate team with organization
        do!
            service.CreateRelationshipsAsync(
                [ { User = "organization:acme"
                    Relation = "organization"
                    Object = "team:engineering" } ]
            )

        // Act & Assert - Verify Carol can rename teams
        let! canRename = service.CheckPermissionAsync "user:carol" "rename" "team:engineering"
        Assert.True(canRename, "Organization admin should be able to rename teams")
    }

[<Fact>]
let ``Team admin cannot rename teams`` () : Task =
    task {
        // Arrange - Create store, write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-team-no-rename-{Guid.NewGuid()}" })

        let service = createServiceWithStore storeResponse.Id
        let! _ = service.WriteAuthorizationModelAsync()

        // Make Dave a team admin (NOT an org admin)
        do!
            service.CreateRelationshipsAsync(
                [ { User = "user:dave"
                    Relation = "admin"
                    Object = "team:engineering" } ]
            )

        // Associate team with organization
        do!
            service.CreateRelationshipsAsync(
                [ { User = "organization:acme"
                    Relation = "organization"
                    Object = "team:engineering" } ]
            )

        // Act & Assert - Verify Dave CANNOT rename teams
        let! canRename = service.CheckPermissionAsync "user:dave" "rename" "team:engineering"
        Assert.False(canRename, "Team admin should NOT be able to rename teams (only org admin can)")
    }

[<Fact>]
let ``Only organization admin can delete teams`` () : Task =
    task {
        // Arrange - Create store, write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-team-delete-{Guid.NewGuid()}" })

        let service = createServiceWithStore storeResponse.Id
        let! _ = service.WriteAuthorizationModelAsync()

        // Make Eve an organization admin
        do!
            service.CreateRelationshipsAsync(
                [ { User = "user:eve"
                    Relation = "admin"
                    Object = "organization:acme" } ]
            )

        // Associate team with organization
        do!
            service.CreateRelationshipsAsync(
                [ { User = "organization:acme"
                    Relation = "organization"
                    Object = "team:engineering" } ]
            )

        // Act & Assert - Verify Eve can delete teams
        let! canDelete = service.CheckPermissionAsync "user:eve" "delete" "team:engineering"
        Assert.True(canDelete, "Organization admin should be able to delete teams")
    }

[<Fact>]
let ``Team admin cannot delete teams`` () : Task =
    task {
        // Arrange - Create store, write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-team-no-delete-{Guid.NewGuid()}" })

        let service = createServiceWithStore storeResponse.Id
        let! _ = service.WriteAuthorizationModelAsync()

        // Make Frank a team admin (NOT an org admin)
        do!
            service.CreateRelationshipsAsync(
                [ { User = "user:frank"
                    Relation = "admin"
                    Object = "team:engineering" } ]
            )

        // Associate team with organization
        do!
            service.CreateRelationshipsAsync(
                [ { User = "organization:acme"
                    Relation = "organization"
                    Object = "team:engineering" } ]
            )

        // Act & Assert - Verify Frank CANNOT delete teams
        let! canDelete = service.CheckPermissionAsync "user:frank" "delete" "team:engineering"
        Assert.False(canDelete, "Team admin should NOT be able to delete teams (only org admin can)")
    }
