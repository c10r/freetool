module Freetool.Api.Tests.WorkspaceControllerAuthTests

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
let ``CreateWorkspace - Organization admin can create workspaces`` () : Task =
    task {
        // Arrange - Create store and write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let storeGuid = Guid.NewGuid().ToString("N")

        let! storeResponse =
            serviceWithoutStore.CreateStoreAsync({ Name = $"test-create-workspace-allowed-{storeGuid}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let orgId = "default"

        // Make user an organization admin
        do!
            authService.CreateRelationshipsAsync(
                [ { Subject = User userId
                    Relation = TeamAdmin
                    Object = OrganizationObject orgId } ]
            )

        // Act - Check if user can create workspaces
        let! isAdmin = authService.CheckPermissionAsync (User userId) (TeamAdmin) (OrganizationObject orgId)

        // Assert
        Assert.True(isAdmin, "Organization admin should be able to create workspaces")
    }

[<Fact>]
let ``CreateWorkspace - Non-admin cannot create workspaces`` () : Task =
    task {
        // Arrange - Create store and write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let storeGuid = Guid.NewGuid().ToString("N")

        let! storeResponse =
            serviceWithoutStore.CreateStoreAsync({ Name = $"test-create-workspace-denied-{storeGuid}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let orgId = "default"

        // User is NOT an organization admin

        // Act - Check if user can create workspaces
        let! isAdmin = authService.CheckPermissionAsync (User userId) (TeamAdmin) (OrganizationObject orgId)

        // Assert
        Assert.False(isAdmin, "Non-admin should NOT be able to create workspaces")
    }

[<Fact>]
let ``CreateWorkspace - Team admin cannot create workspaces`` () : Task =
    task {
        // Arrange - Create store and write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let storeGuid = Guid.NewGuid().ToString("N")

        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-team-admin-no-create-{storeGuid}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let teamId = Guid.NewGuid().ToString()
        let orgId = "default"

        // Make user a team admin (NOT an org admin)
        do!
            authService.CreateRelationshipsAsync(
                [ { Subject = User userId
                    Relation = TeamAdmin
                    Object = TeamObject teamId } ]
            )

        // Act - Check if team admin can create workspaces
        let! isOrgAdmin = authService.CheckPermissionAsync (User userId) (TeamAdmin) (OrganizationObject orgId)

        // Assert
        Assert.False(isOrgAdmin, "Team admin should NOT be able to create workspaces (only org admin can)")
    }

[<Fact>]
let ``DeleteWorkspace - Organization admin can delete workspaces`` () : Task =
    task {
        // Arrange - Create store and write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let storeGuid = Guid.NewGuid().ToString("N")

        let! storeResponse =
            serviceWithoutStore.CreateStoreAsync({ Name = $"test-delete-workspace-allowed-{storeGuid}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let orgId = "default"
        let workspaceId = Guid.NewGuid().ToString()

        // Make user an organization admin
        do!
            authService.CreateRelationshipsAsync(
                [ { Subject = User userId
                    Relation = TeamAdmin
                    Object = OrganizationObject orgId } ]
            )

        // Act - Check if user can delete workspaces
        let! isAdmin = authService.CheckPermissionAsync (User userId) (TeamAdmin) (OrganizationObject orgId)

        // Assert
        Assert.True(isAdmin, "Organization admin should be able to delete workspaces")
    }

[<Fact>]
let ``DeleteWorkspace - Non-admin cannot delete workspaces`` () : Task =
    task {
        // Arrange - Create store and write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let storeGuid = Guid.NewGuid().ToString("N")

        let! storeResponse =
            serviceWithoutStore.CreateStoreAsync({ Name = $"test-delete-workspace-denied-{storeGuid}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let orgId = "default"

        // User is NOT an organization admin

        // Act - Check if user can delete workspaces
        let! isAdmin = authService.CheckPermissionAsync (User userId) (TeamAdmin) (OrganizationObject orgId)

        // Assert
        Assert.False(isAdmin, "Non-admin should NOT be able to delete workspaces")
    }

[<Fact>]
let ``DeleteWorkspace - Team admin cannot delete workspaces`` () : Task =
    task {
        // Arrange - Create store and write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let storeGuid = Guid.NewGuid().ToString("N")

        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-team-admin-no-delete-{storeGuid}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let teamId = Guid.NewGuid().ToString()
        let orgId = "default"

        // Make user a team admin (NOT an org admin)
        do!
            authService.CreateRelationshipsAsync(
                [ { Subject = User userId
                    Relation = TeamAdmin
                    Object = TeamObject teamId } ]
            )

        // Act - Check if team admin can delete workspaces
        let! isOrgAdmin = authService.CheckPermissionAsync (User userId) (TeamAdmin) (OrganizationObject orgId)

        // Assert
        Assert.False(isOrgAdmin, "Team admin should NOT be able to delete workspaces (only org admin can)")
    }

[<Fact>]
let ``UpdateWorkspaceGroup - Organization admin can update workspace group`` () : Task =
    task {
        // Arrange - Create store and write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let storeGuid = Guid.NewGuid().ToString("N")

        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-update-group-allowed-{storeGuid}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let orgId = "default"

        // Make user an organization admin
        do!
            authService.CreateRelationshipsAsync(
                [ { Subject = User userId
                    Relation = TeamAdmin
                    Object = OrganizationObject orgId } ]
            )

        // Act - Check if user can update workspace groups
        let! isAdmin = authService.CheckPermissionAsync (User userId) (TeamAdmin) (OrganizationObject orgId)

        // Assert
        Assert.True(isAdmin, "Organization admin should be able to update workspace groups")
    }

[<Fact>]
let ``UpdateWorkspaceGroup - Non-admin cannot update workspace group`` () : Task =
    task {
        // Arrange - Create store and write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let storeGuid = Guid.NewGuid().ToString("N")

        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-update-group-denied-{storeGuid}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let orgId = "default"

        // User is NOT an organization admin

        // Act - Check if user can update workspace groups
        let! isAdmin = authService.CheckPermissionAsync (User userId) (TeamAdmin) (OrganizationObject orgId)

        // Assert
        Assert.False(isAdmin, "Non-admin should NOT be able to update workspace groups")
    }

[<Fact>]
let ``UpdateWorkspaceGroup - Team admin cannot update workspace group`` () : Task =
    task {
        // Arrange - Create store and write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let storeGuid = Guid.NewGuid().ToString("N")

        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-team-admin-no-update-{storeGuid}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let teamId = Guid.NewGuid().ToString()
        let orgId = "default"

        // Make user a team admin (NOT an org admin)
        do!
            authService.CreateRelationshipsAsync(
                [ { Subject = User userId
                    Relation = TeamAdmin
                    Object = TeamObject teamId } ]
            )

        // Act - Check if team admin can update workspace groups
        let! isOrgAdmin = authService.CheckPermissionAsync (User userId) (TeamAdmin) (OrganizationObject orgId)

        // Assert
        Assert.False(isOrgAdmin, "Team admin should NOT be able to update workspace groups (only org admin can)")
    }

[<Fact>]
let ``Workspace permissions hierarchy - Global admin has precedence over team admin`` () : Task =
    task {
        // Arrange - Create store and write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let storeGuid = Guid.NewGuid().ToString("N")

        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-hierarchy-{storeGuid}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let teamId = Guid.NewGuid().ToString()
        let orgId = "default"
        let workspaceId = Guid.NewGuid().ToString()

        // Make user BOTH a team admin AND an org admin
        do!
            authService.CreateRelationshipsAsync(
                [ { Subject = User userId
                    Relation = TeamAdmin
                    Object = TeamObject teamId }
                  { Subject = User userId
                    Relation = TeamAdmin
                    Object = OrganizationObject orgId } ]
            )

        // Associate workspace with team
        do!
            authService.CreateRelationshipsAsync(
                [ { Subject = Team teamId
                    Relation = WorkspaceTeam
                    Object = WorkspaceObject workspaceId } ]
            )

        // Act - Verify user has all permissions
        let! isOrgAdmin = authService.CheckPermissionAsync (User userId) (TeamAdmin) (OrganizationObject orgId)
        let! isTeamAdmin = authService.CheckPermissionAsync (User userId) (TeamAdmin) (TeamObject teamId)

        // Verify workspace permissions
        let! canCreateResource =
            authService.CheckPermissionAsync (User userId) (ResourceCreate) (WorkspaceObject workspaceId)

        let! canDeleteFolder =
            authService.CheckPermissionAsync (User userId) (FolderDelete) (WorkspaceObject workspaceId)

        // Assert
        Assert.True(isOrgAdmin, "User should be an organization admin")
        Assert.True(isTeamAdmin, "User should be a team admin")
        Assert.True(canCreateResource, "User should have create_resource permission via team admin")
        Assert.True(canDeleteFolder, "User should have delete_folder permission via team admin")
    }

[<Fact>]
let ``Team member without explicit permissions cannot perform workspace operations`` () : Task =
    task {
        // Arrange - Create store and write model
        let serviceWithoutStore = createServiceWithoutStore ()

        let storeGuid = Guid.NewGuid().ToString("N")
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-member-no-perms-{storeGuid}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let teamId = Guid.NewGuid().ToString()
        let orgId = "default"
        let workspaceId = Guid.NewGuid().ToString()

        // Make user a team member (NOT admin)
        do!
            authService.CreateRelationshipsAsync(
                [ { Subject = User userId
                    Relation = TeamMember
                    Object = TeamObject teamId } ]
            )

        // Associate workspace with team
        do!
            authService.CreateRelationshipsAsync(
                [ { Subject = Team teamId
                    Relation = WorkspaceTeam
                    Object = WorkspaceObject workspaceId } ]
            )

        // Act - Check various permissions
        let! isOrgAdmin = authService.CheckPermissionAsync (User userId) (TeamAdmin) (OrganizationObject orgId)
        let! isTeamAdmin = authService.CheckPermissionAsync (User userId) (TeamAdmin) (TeamObject teamId)

        let! canCreateResource =
            authService.CheckPermissionAsync (User userId) (ResourceCreate) (WorkspaceObject workspaceId)

        let! canDeleteApp = authService.CheckPermissionAsync (User userId) (AppDelete) (WorkspaceObject workspaceId)

        // Assert
        Assert.False(isOrgAdmin, "Team member should NOT be an organization admin")
        Assert.False(isTeamAdmin, "Team member should NOT be a team admin")

        Assert.False(
            canCreateResource,
            "Team member without explicit permission should NOT have create_resource permission"
        )

        Assert.False(canDeleteApp, "Team member without explicit permission should NOT have delete_app permission")
    }
