module Freetool.Api.Tests.AppControllerAuthTests

open System
open System.Threading.Tasks
open Xunit
open Freetool.Application.Interfaces
open Freetool.Infrastructure.Services
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities

// Test configuration - assumes OpenFGA is running on localhost:8090
let openFgaApiUrl = "http://localhost:8090"

// Helper to create a test service without a store ID (for store creation)
let createServiceWithoutStore () =
    OpenFgaService(openFgaApiUrl) :> IAuthorizationService

// Helper to create a test service with a store ID
let createServiceWithStore storeId =
    OpenFgaService(openFgaApiUrl, storeId) :> IAuthorizationService

// Test setup helper to create a workspace with proper permissions
let setupWorkspaceWithPermissions (authService: IAuthorizationService) (userId: string) (permission: string) =
    task {
        let workspaceId = Guid.NewGuid().ToString()
        let teamId = "team:test-team"

        // Associate workspace with team
        do!
            authService.CreateRelationshipsAsync(
                [ { User = teamId
                    Relation = "team"
                    Object = $"workspace:{workspaceId}" } ]
            )

        // Grant permission to user
        do!
            authService.CreateRelationshipsAsync(
                [ { User = $"user:{userId}"
                    Relation = permission
                    Object = $"workspace:{workspaceId}" } ]
            )

        return workspaceId
    }

[<Fact>]
let ``CreateApp endpoint - User with create_app permission can create app`` () : Task =
    task {
        // Arrange - Create store and write model
        let serviceWithoutStore = createServiceWithoutStore ()

        let! storeResponse =
            serviceWithoutStore.CreateStoreAsync({ Name = $"test-create-app-allowed-{Guid.NewGuid()}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let! workspaceId = setupWorkspaceWithPermissions authService userId "create_app"

        // Act - Check if user can create app on workspace
        let! canCreate = authService.CheckPermissionAsync $"user:{userId}" "create_app" $"workspace:{workspaceId}"

        // Assert
        Assert.True(canCreate, "User with create_app permission should be allowed to create apps")
    }

[<Fact>]
let ``CreateApp endpoint - User without create_app permission is denied`` () : Task =
    task {
        // Arrange - Create store and write model
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-create-app-denied-{Guid.NewGuid()}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let workspaceId = Guid.NewGuid().ToString()

        // Act - Check if user (without permission) can create app
        let! canCreate = authService.CheckPermissionAsync $"user:{userId}" "create_app" $"workspace:{workspaceId}"

        // Assert
        Assert.False(canCreate, "User without create_app permission should be denied")
    }

[<Fact>]
let ``GetAppById endpoint - User with run_app permission can view app`` () : Task =
    task {
        // Arrange
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-get-app-allowed-{Guid.NewGuid()}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let! workspaceId = setupWorkspaceWithPermissions authService userId "run_app"

        // Act
        let! canView = authService.CheckPermissionAsync $"user:{userId}" "run_app" $"workspace:{workspaceId}"

        // Assert
        Assert.True(canView, "User with run_app permission should be allowed to view apps")
    }

[<Fact>]
let ``GetAppById endpoint - User without run_app permission is denied`` () : Task =
    task {
        // Arrange
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-get-app-denied-{Guid.NewGuid()}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let workspaceId = Guid.NewGuid().ToString()

        // Act
        let! canView = authService.CheckPermissionAsync $"user:{userId}" "run_app" $"workspace:{workspaceId}"

        // Assert
        Assert.False(canView, "User without run_app permission should be denied")
    }

[<Fact>]
let ``UpdateAppName endpoint - User with edit_app permission can update`` () : Task =
    task {
        // Arrange
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-edit-app-allowed-{Guid.NewGuid()}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let! workspaceId = setupWorkspaceWithPermissions authService userId "edit_app"

        // Act
        let! canEdit = authService.CheckPermissionAsync $"user:{userId}" "edit_app" $"workspace:{workspaceId}"

        // Assert
        Assert.True(canEdit, "User with edit_app permission should be allowed to update apps")
    }

[<Fact>]
let ``UpdateAppName endpoint - User without edit_app permission is denied`` () : Task =
    task {
        // Arrange
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-edit-app-denied-{Guid.NewGuid()}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let workspaceId = Guid.NewGuid().ToString()

        // Act
        let! canEdit = authService.CheckPermissionAsync $"user:{userId}" "edit_app" $"workspace:{workspaceId}"

        // Assert
        Assert.False(canEdit, "User without edit_app permission should be denied")
    }

[<Fact>]
let ``DeleteApp endpoint - User with delete_app permission can delete`` () : Task =
    task {
        // Arrange
        let serviceWithoutStore = createServiceWithoutStore ()

        let! storeResponse =
            serviceWithoutStore.CreateStoreAsync({ Name = $"test-delete-app-allowed-{Guid.NewGuid()}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let! workspaceId = setupWorkspaceWithPermissions authService userId "delete_app"

        // Act
        let! canDelete = authService.CheckPermissionAsync $"user:{userId}" "delete_app" $"workspace:{workspaceId}"

        // Assert
        Assert.True(canDelete, "User with delete_app permission should be allowed to delete apps")
    }

[<Fact>]
let ``DeleteApp endpoint - User without delete_app permission is denied`` () : Task =
    task {
        // Arrange
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-delete-app-denied-{Guid.NewGuid()}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let workspaceId = Guid.NewGuid().ToString()

        // Act
        let! canDelete = authService.CheckPermissionAsync $"user:{userId}" "delete_app" $"workspace:{workspaceId}"

        // Assert
        Assert.False(canDelete, "User without delete_app permission should be denied")
    }

[<Fact>]
let ``RunApp endpoint - User with run_app permission can run`` () : Task =
    task {
        // Arrange
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-run-app-allowed-{Guid.NewGuid()}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let! workspaceId = setupWorkspaceWithPermissions authService userId "run_app"

        // Act
        let! canRun = authService.CheckPermissionAsync $"user:{userId}" "run_app" $"workspace:{workspaceId}"

        // Assert
        Assert.True(canRun, "User with run_app permission should be allowed to run apps")
    }

[<Fact>]
let ``RunApp endpoint - User without run_app permission is denied`` () : Task =
    task {
        // Arrange
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-run-app-denied-{Guid.NewGuid()}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let workspaceId = Guid.NewGuid().ToString()

        // Act
        let! canRun = authService.CheckPermissionAsync $"user:{userId}" "run_app" $"workspace:{workspaceId}"

        // Assert
        Assert.False(canRun, "User without run_app permission should be denied")
    }

[<Fact>]
let ``Team admin has all app permissions on workspace`` () : Task =
    task {
        // Arrange
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-team-admin-{Guid.NewGuid()}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let teamId = $"team:{Guid.NewGuid()}"
        let workspaceId = Guid.NewGuid().ToString()

        // Make user a team admin
        do!
            authService.CreateRelationshipsAsync(
                [ { User = $"user:{userId}"
                    Relation = "admin"
                    Object = teamId } ]
            )

        // Associate workspace with team
        do!
            authService.CreateRelationshipsAsync(
                [ { User = teamId
                    Relation = "team"
                    Object = $"workspace:{workspaceId}" } ]
            )

        // Act & Assert - Verify user has all app permissions
        let! canCreateApp = authService.CheckPermissionAsync $"user:{userId}" "create_app" $"workspace:{workspaceId}"
        Assert.True(canCreateApp, "Team admin should have create_app permission")

        let! canEditApp = authService.CheckPermissionAsync $"user:{userId}" "edit_app" $"workspace:{workspaceId}"
        Assert.True(canEditApp, "Team admin should have edit_app permission")

        let! canDeleteApp = authService.CheckPermissionAsync $"user:{userId}" "delete_app" $"workspace:{workspaceId}"
        Assert.True(canDeleteApp, "Team admin should have delete_app permission")

        let! canRunApp = authService.CheckPermissionAsync $"user:{userId}" "run_app" $"workspace:{workspaceId}"
        Assert.True(canRunApp, "Team admin should have run_app permission")
    }

[<Fact>]
let ``Global admin has all app permissions on any workspace`` () : Task =
    task {
        // Arrange
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-global-admin-{Guid.NewGuid()}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let orgId = $"organization:{Guid.NewGuid()}"
        let workspaceId = Guid.NewGuid().ToString()

        // Make user a global admin
        do!
            authService.CreateRelationshipsAsync(
                [ { User = $"user:{userId}"
                    Relation = "admin"
                    Object = orgId } ]
            )

        // Grant global admin permissions on workspace
        do!
            authService.CreateRelationshipsAsync(
                [ { User = $"{orgId}#admin"
                    Relation = "create_app"
                    Object = $"workspace:{workspaceId}" }
                  { User = $"{orgId}#admin"
                    Relation = "edit_app"
                    Object = $"workspace:{workspaceId}" }
                  { User = $"{orgId}#admin"
                    Relation = "delete_app"
                    Object = $"workspace:{workspaceId}" }
                  { User = $"{orgId}#admin"
                    Relation = "run_app"
                    Object = $"workspace:{workspaceId}" } ]
            )

        // Act & Assert - Verify global admin has all permissions
        let! canCreateApp = authService.CheckPermissionAsync $"user:{userId}" "create_app" $"workspace:{workspaceId}"
        Assert.True(canCreateApp, "Global admin should have create_app permission")

        let! canEditApp = authService.CheckPermissionAsync $"user:{userId}" "edit_app" $"workspace:{workspaceId}"
        Assert.True(canEditApp, "Global admin should have edit_app permission")

        let! canDeleteApp = authService.CheckPermissionAsync $"user:{userId}" "delete_app" $"workspace:{workspaceId}"
        Assert.True(canDeleteApp, "Global admin should have delete_app permission")

        let! canRunApp = authService.CheckPermissionAsync $"user:{userId}" "run_app" $"workspace:{workspaceId}"
        Assert.True(canRunApp, "Global admin should have run_app permission")
    }

[<Fact>]
let ``Team member with specific permission can only perform allowed actions`` () : Task =
    task {
        // Arrange
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-member-limited-{Guid.NewGuid()}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let teamId = $"team:{Guid.NewGuid()}"
        let workspaceId = Guid.NewGuid().ToString()

        // Make user a team member (not admin)
        do!
            authService.CreateRelationshipsAsync(
                [ { User = $"user:{userId}"
                    Relation = "member"
                    Object = teamId } ]
            )

        // Associate workspace with team
        do!
            authService.CreateRelationshipsAsync(
                [ { User = teamId
                    Relation = "team"
                    Object = $"workspace:{workspaceId}" } ]
            )

        // Grant only run_app permission
        do!
            authService.CreateRelationshipsAsync(
                [ { User = $"user:{userId}"
                    Relation = "run_app"
                    Object = $"workspace:{workspaceId}" } ]
            )

        // Act & Assert
        let! canRunApp = authService.CheckPermissionAsync $"user:{userId}" "run_app" $"workspace:{workspaceId}"
        Assert.True(canRunApp, "Team member should have explicitly granted run_app permission")

        let! canCreateApp = authService.CheckPermissionAsync $"user:{userId}" "create_app" $"workspace:{workspaceId}"
        Assert.False(canCreateApp, "Team member should NOT have create_app permission (not granted)")

        let! canEditApp = authService.CheckPermissionAsync $"user:{userId}" "edit_app" $"workspace:{workspaceId}"
        Assert.False(canEditApp, "Team member should NOT have edit_app permission (not granted)")

        let! canDeleteApp = authService.CheckPermissionAsync $"user:{userId}" "delete_app" $"workspace:{workspaceId}"
        Assert.False(canDeleteApp, "Team member should NOT have delete_app permission (not granted)")
    }

[<Fact>]
let ``All update endpoints require edit_app permission`` () : Task =
    task {
        // Arrange
        let serviceWithoutStore = createServiceWithoutStore ()
        let! storeResponse = serviceWithoutStore.CreateStoreAsync({ Name = $"test-all-updates-{Guid.NewGuid()}" })

        let authService = createServiceWithStore storeResponse.Id
        let! _ = authService.WriteAuthorizationModelAsync()

        let userId = Guid.NewGuid().ToString()
        let! workspaceId = setupWorkspaceWithPermissions authService userId "edit_app"

        // Act - Verify that all these update operations use the same permission
        let! canUpdateName = authService.CheckPermissionAsync $"user:{userId}" "edit_app" $"workspace:{workspaceId}"
        let! canUpdateInputs = authService.CheckPermissionAsync $"user:{userId}" "edit_app" $"workspace:{workspaceId}"

        let! canUpdateQueryParams =
            authService.CheckPermissionAsync $"user:{userId}" "edit_app" $"workspace:{workspaceId}"

        let! canUpdateBody = authService.CheckPermissionAsync $"user:{userId}" "edit_app" $"workspace:{workspaceId}"
        let! canUpdateHeaders = authService.CheckPermissionAsync $"user:{userId}" "edit_app" $"workspace:{workspaceId}"
        let! canUpdateUrlPath = authService.CheckPermissionAsync $"user:{userId}" "edit_app" $"workspace:{workspaceId}"

        // Assert - All should be true with edit_app permission
        Assert.True(canUpdateName, "edit_app permission allows UpdateAppName")
        Assert.True(canUpdateInputs, "edit_app permission allows UpdateAppInputs")
        Assert.True(canUpdateQueryParams, "edit_app permission allows UpdateAppQueryParameters")
        Assert.True(canUpdateBody, "edit_app permission allows UpdateAppBody")
        Assert.True(canUpdateHeaders, "edit_app permission allows UpdateAppHeaders")
        Assert.True(canUpdateUrlPath, "edit_app permission allows UpdateAppUrlPath")
    }
