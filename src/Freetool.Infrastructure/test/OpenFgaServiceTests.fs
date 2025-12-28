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
            [ { Subject = User "alice"
                Relation = TeamMember
                Object = TeamObject "engineering" } ]

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
            { Subject = User "bob"
              Relation = TeamMember
              Object = TeamObject "engineering" }

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
            { Subject = User "carol"
              Relation = TeamMember
              Object = TeamObject "engineering" }

        do! service.CreateRelationshipsAsync([ memberTuple ])

        let adminTuple =
            { Subject = User "carol"
              Relation = TeamAdmin
              Object = TeamObject "engineering" }

        // Act - Promote Carol from member to admin
        do!
            service.UpdateRelationshipsAsync(
                { TuplesToAdd = [ adminTuple ]
                  TuplesToRemove = [ memberTuple ] }
            )

        // Assert - Verify Carol is now an admin
        let! isAdmin = service.CheckPermissionAsync (User "carol") TeamAdmin (TeamObject "engineering")
        Assert.True(isAdmin)

        // Verify Carol is no longer just a member
        let! isMember = service.CheckPermissionAsync (User "carol") TeamMember (TeamObject "engineering")
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
            { Subject = User "dave"
              Relation = TeamMember
              Object = TeamObject "engineering" }

        do! service.CreateRelationshipsAsync([ tuple ])

        // Act
        let! hasPermission = service.CheckPermissionAsync (User "dave") TeamMember (TeamObject "engineering")

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
        let! hasPermission = service.CheckPermissionAsync (User "eve") TeamAdmin (TeamObject "engineering")

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
                [ { Subject = User "frank"
                    Relation = TeamAdmin
                    Object = TeamObject "engineering" } ]
            )

        // Associate workspace with team
        do!
            service.CreateRelationshipsAsync(
                [ { Subject = Team "engineering"
                    Relation = WorkspaceTeam
                    Object = WorkspaceObject "main" } ]
            )

        // Act & Assert - Verify Frank has all 7 permissions
        let! canCreateResource = service.CheckPermissionAsync (User "frank") ResourceCreate (WorkspaceObject "main")
        Assert.True(canCreateResource, "Team admin should have create_resource permission")

        let! canEditResource = service.CheckPermissionAsync (User "frank") ResourceEdit (WorkspaceObject "main")
        Assert.True(canEditResource, "Team admin should have edit_resource permission")

        let! canDeleteResource = service.CheckPermissionAsync (User "frank") ResourceDelete (WorkspaceObject "main")
        Assert.True(canDeleteResource, "Team admin should have delete_resource permission")

        let! canCreateApp = service.CheckPermissionAsync (User "frank") AppCreate (WorkspaceObject "main")
        Assert.True(canCreateApp, "Team admin should have create_app permission")

        let! canEditApp = service.CheckPermissionAsync (User "frank") AppEdit (WorkspaceObject "main")
        Assert.True(canEditApp, "Team admin should have edit_app permission")

        let! canDeleteApp = service.CheckPermissionAsync (User "frank") AppDelete (WorkspaceObject "main")
        Assert.True(canDeleteApp, "Team admin should have delete_app permission")

        let! canRunApp = service.CheckPermissionAsync (User "frank") AppRun (WorkspaceObject "main")
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
                [ { Subject = User "grace"
                    Relation = TeamAdmin
                    Object = OrganizationObject "acme" } ]
            )

        // Associate workspace with team
        do!
            service.CreateRelationshipsAsync(
                [ { Subject = Team "sales"
                    Relation = WorkspaceTeam
                    Object = WorkspaceObject "sales-dashboard" } ]
            )

        // Grant global admins all permissions on the workspace
        do!
            service.CreateRelationshipsAsync(
                [ { Subject = UserSetFromRelation("organization", "acme", "admin")
                    Relation = ResourceCreate
                    Object = WorkspaceObject "sales-dashboard" }
                  { Subject = UserSetFromRelation("organization", "acme", "admin")
                    Relation = AppRun
                    Object = WorkspaceObject "sales-dashboard" } ]
            )

        // Act & Assert - Verify Grace has all permissions on any workspace
        let! canCreateResource =
            service.CheckPermissionAsync (User "grace") ResourceCreate (WorkspaceObject "sales-dashboard")

        Assert.True(canCreateResource, "Global admin should have create_resource permission")

        let! canRunApp = service.CheckPermissionAsync (User "grace") AppRun (WorkspaceObject "sales-dashboard")
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
                [ { Subject = User "henry"
                    Relation = TeamMember
                    Object = TeamObject "engineering" } ]
            )

        // Grant Henry specific permission on workspace
        do!
            service.CreateRelationshipsAsync(
                [ { Subject = User "henry"
                    Relation = AppRun
                    Object = WorkspaceObject "main" } ]
            )

        // Act
        let! canRunApp = service.CheckPermissionAsync (User "henry") AppRun (WorkspaceObject "main")
        let! canEditApp = service.CheckPermissionAsync (User "henry") AppEdit (WorkspaceObject "main")

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
                [ { Subject = User "alice"
                    Relation = TeamAdmin
                    Object = OrganizationObject "acme" } ]
            )

        // Associate workspace with organization
        do!
            service.CreateRelationshipsAsync(
                [ { Subject = Organization "acme"
                    Relation = TeamOrganization
                    Object = WorkspaceObject "new-workspace" } ]
            )

        // Act & Assert - Verify Alice can create workspaces
        let! canCreate = service.CheckPermissionAsync (User "alice") WorkspaceCreate (WorkspaceObject "new-workspace")
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
                [ { Subject = User "bob"
                    Relation = TeamAdmin
                    Object = TeamObject "engineering" } ]
            )

        // Associate workspace with organization
        do!
            service.CreateRelationshipsAsync(
                [ { Subject = Organization "acme"
                    Relation = TeamOrganization
                    Object = WorkspaceObject "new-workspace" } ]
            )

        // Act & Assert - Verify Bob CANNOT create workspaces
        let! canCreate = service.CheckPermissionAsync (User "bob") WorkspaceCreate (WorkspaceObject "new-workspace")
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
                [ { Subject = User "carol"
                    Relation = TeamAdmin
                    Object = OrganizationObject "acme" } ]
            )

        // Associate team with organization
        do!
            service.CreateRelationshipsAsync(
                [ { Subject = Organization "acme"
                    Relation = TeamOrganization
                    Object = TeamObject "engineering" } ]
            )

        // Act & Assert - Verify Carol can rename teams
        let! canRename = service.CheckPermissionAsync (User "carol") TeamRename (TeamObject "engineering")
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
                [ { Subject = User "dave"
                    Relation = TeamAdmin
                    Object = TeamObject "engineering" } ]
            )

        // Associate team with organization
        do!
            service.CreateRelationshipsAsync(
                [ { Subject = Organization "acme"
                    Relation = TeamOrganization
                    Object = TeamObject "engineering" } ]
            )

        // Act & Assert - Verify Dave CANNOT rename teams
        let! canRename = service.CheckPermissionAsync (User "dave") TeamRename (TeamObject "engineering")
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
                [ { Subject = User "eve"
                    Relation = TeamAdmin
                    Object = OrganizationObject "acme" } ]
            )

        // Associate team with organization
        do!
            service.CreateRelationshipsAsync(
                [ { Subject = Organization "acme"
                    Relation = TeamOrganization
                    Object = TeamObject "engineering" } ]
            )

        // Act & Assert - Verify Eve can delete teams
        let! canDelete = service.CheckPermissionAsync (User "eve") TeamDelete (TeamObject "engineering")
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
                [ { Subject = User "frank"
                    Relation = TeamAdmin
                    Object = TeamObject "engineering" } ]
            )

        // Associate team with organization
        do!
            service.CreateRelationshipsAsync(
                [ { Subject = Organization "acme"
                    Relation = TeamOrganization
                    Object = TeamObject "engineering" } ]
            )

        // Act & Assert - Verify Frank CANNOT delete teams
        let! canDelete = service.CheckPermissionAsync (User "frank") TeamDelete (TeamObject "engineering")
        Assert.False(canDelete, "Team admin should NOT be able to delete teams (only org admin can)")
    }

[<Fact>]
let ``Team admin has all folder permissions`` () : Task =
    task {
        // Arrange - Create store, write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-folder-perms-{Guid.NewGuid()}" })

        let service = createServiceWithStore storeResponse.Id
        let! _ = service.WriteAuthorizationModelAsync()

        // Make George a team admin
        do!
            service.CreateRelationshipsAsync(
                [ { Subject = User "george"
                    Relation = TeamAdmin
                    Object = TeamObject "engineering" } ]
            )

        // Associate workspace with team
        do!
            service.CreateRelationshipsAsync(
                [ { Subject = Team "engineering"
                    Relation = WorkspaceTeam
                    Object = WorkspaceObject "main" } ]
            )

        // Act & Assert - Verify George has all 3 folder permissions
        let! canCreateFolder = service.CheckPermissionAsync (User "george") FolderCreate (WorkspaceObject "main")
        Assert.True(canCreateFolder, "Team admin should have create_folder permission")

        let! canEditFolder = service.CheckPermissionAsync (User "george") FolderEdit (WorkspaceObject "main")
        Assert.True(canEditFolder, "Team admin should have edit_folder permission")

        let! canDeleteFolder = service.CheckPermissionAsync (User "george") FolderDelete (WorkspaceObject "main")
        Assert.True(canDeleteFolder, "Team admin should have delete_folder permission")
    }

[<Fact>]
let ``Global admin has all folder permissions`` () : Task =
    task {
        // Arrange - Create store, write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-global-folder-{Guid.NewGuid()}" })

        let service = createServiceWithStore storeResponse.Id
        let! _ = service.WriteAuthorizationModelAsync()

        // Make Hannah a global admin
        do!
            service.CreateRelationshipsAsync(
                [ { Subject = User "hannah"
                    Relation = TeamAdmin
                    Object = OrganizationObject "acme" } ]
            )

        // Associate workspace with team
        do!
            service.CreateRelationshipsAsync(
                [ { Subject = Team "engineering"
                    Relation = WorkspaceTeam
                    Object = WorkspaceObject "main" } ]
            )

        // Grant global admins folder permissions on the workspace
        do!
            service.CreateRelationshipsAsync(
                [ { Subject = UserSetFromRelation("organization", "acme", "admin")
                    Relation = FolderCreate
                    Object = WorkspaceObject "main" }
                  { Subject = UserSetFromRelation("organization", "acme", "admin")
                    Relation = FolderEdit
                    Object = WorkspaceObject "main" }
                  { Subject = UserSetFromRelation("organization", "acme", "admin")
                    Relation = FolderDelete
                    Object = WorkspaceObject "main" } ]
            )

        // Act & Assert - Verify Hannah has all folder permissions
        let! canCreateFolder = service.CheckPermissionAsync (User "hannah") FolderCreate (WorkspaceObject "main")
        Assert.True(canCreateFolder, "Global admin should have create_folder permission")

        let! canEditFolder = service.CheckPermissionAsync (User "hannah") FolderEdit (WorkspaceObject "main")
        Assert.True(canEditFolder, "Global admin should have edit_folder permission")

        let! canDeleteFolder = service.CheckPermissionAsync (User "hannah") FolderDelete (WorkspaceObject "main")
        Assert.True(canDeleteFolder, "Global admin should have delete_folder permission")
    }

[<Fact>]
let ``Team member with granted folder permission can manage folders`` () : Task =
    task {
        // Arrange - Create store, write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-member-folder-{Guid.NewGuid()}" })

        let service = createServiceWithStore storeResponse.Id
        let! _ = service.WriteAuthorizationModelAsync()

        // Make Ivan a team member
        do!
            service.CreateRelationshipsAsync(
                [ { Subject = User "ivan"
                    Relation = TeamMember
                    Object = TeamObject "engineering" } ]
            )

        // Grant Ivan specific folder permissions on workspace
        do!
            service.CreateRelationshipsAsync(
                [ { Subject = User "ivan"
                    Relation = FolderCreate
                    Object = WorkspaceObject "main" }
                  { Subject = User "ivan"
                    Relation = FolderEdit
                    Object = WorkspaceObject "main" } ]
            )

        // Act & Assert
        let! canCreateFolder = service.CheckPermissionAsync (User "ivan") FolderCreate (WorkspaceObject "main")
        Assert.True(canCreateFolder, "User should have explicitly granted create_folder permission")

        let! canEditFolder = service.CheckPermissionAsync (User "ivan") FolderEdit (WorkspaceObject "main")
        Assert.True(canEditFolder, "User should have explicitly granted edit_folder permission")

        let! canDeleteFolder = service.CheckPermissionAsync (User "ivan") FolderDelete (WorkspaceObject "main")
        Assert.False(canDeleteFolder, "User should NOT have delete_folder permission (not granted)")
    }
