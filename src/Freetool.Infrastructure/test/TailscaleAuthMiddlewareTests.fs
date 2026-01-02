module Freetool.Infrastructure.Tests.TailscaleAuthMiddlewareTests

open System
open System.IO
open System.Threading.Tasks
open Xunit
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Primitives
open Freetool.Domain
open Freetool.Domain.Entities
open Freetool.Domain.ValueObjects
open Freetool.Application.Interfaces
open Freetool.Api.Middleware

// ============================================================================
// Mock Types
// ============================================================================

type MockUserRepository
    (
        existingUsers: Map<string, ValidatedUser>,
        addResult: Result<unit, DomainError>,
        updateResult: Result<unit, DomainError>
    ) =
    let mutable users = existingUsers
    let mutable addedUsers: ValidatedUser list = []
    let mutable updatedUsers: ValidatedUser list = []

    member _.GetAddedUsers() = addedUsers
    member _.GetUpdatedUsers() = updatedUsers

    interface IUserRepository with
        member _.GetByIdAsync(userId: UserId) =
            let userOption =
                users
                |> Map.tryPick (fun _ user -> if user.State.Id = userId then Some user else None)

            Task.FromResult(userOption)

        member _.GetByEmailAsync(email: Email) =
            let userOption = users |> Map.tryFind email.Value

            Task.FromResult(userOption)

        member _.GetAllAsync _ _ = Task.FromResult([])

        member _.AddAsync(user: ValidatedUser) =
            addedUsers <- addedUsers @ [ user ]
            users <- users.Add(user.State.Email, user)
            Task.FromResult(addResult)

        member _.UpdateAsync(user: ValidatedUser) =
            updatedUsers <- updatedUsers @ [ user ]
            users <- users.Add(user.State.Email, user)
            Task.FromResult(updateResult)

        member _.DeleteAsync _ = Task.FromResult(Ok())
        member _.ExistsAsync _ = Task.FromResult(false)
        member _.ExistsByEmailAsync _ = Task.FromResult(false)
        member _.GetCountAsync() = Task.FromResult(0)

type MockAuthorizationService(initOrgResult: Result<unit, exn>) =
    let mutable initOrgCalls: (string * string) list = []

    member _.GetInitOrgCalls() = initOrgCalls

    interface IAuthorizationService with
        member _.CreateStoreAsync _ =
            Task.FromResult({ Id = "store-1"; Name = "test-store" })

        member _.WriteAuthorizationModelAsync() =
            Task.FromResult({ AuthorizationModelId = "model-1" })

        member _.InitializeOrganizationAsync orgId userId =
            task {
                initOrgCalls <- initOrgCalls @ [ (orgId, userId) ]

                match initOrgResult with
                | Ok() -> return ()
                | Error ex -> return raise ex
            }

        member _.CreateRelationshipsAsync _ = Task.FromResult(())
        member _.UpdateRelationshipsAsync _ = Task.FromResult(())
        member _.DeleteRelationshipsAsync _ = Task.FromResult(())

        member _.CheckPermissionAsync _ _ _ = Task.FromResult(false)
        member _.StoreExistsAsync _ = Task.FromResult(true)
        member _.BatchCheckPermissionsAsync _ _ _ = Task.FromResult(Map.empty)

// ============================================================================
// Helper Functions
// ============================================================================

let createTestHttpContext () =
    let context = DefaultHttpContext()
    context.Response.Body <- new MemoryStream()
    context

let addHeader (context: HttpContext) (key: string) (value: string) =
    context.Request.Headers.[key] <- StringValues(value)
    context

let setupServices
    (context: HttpContext)
    (userRepo: IUserRepository)
    (authService: IAuthorizationService)
    (orgAdminEmail: string option)
    =
    let services = ServiceCollection()
    services.AddSingleton<IUserRepository>(userRepo) |> ignore
    services.AddSingleton<IAuthorizationService>(authService) |> ignore

    // Create configuration with optional OrgAdminEmail
    let configValues =
        match orgAdminEmail with
        | Some email -> dict [ "OpenFGA:OrgAdminEmail", email ]
        | None -> dict []

    let config = ConfigurationBuilder().AddInMemoryCollection(configValues).Build()

    services.AddSingleton<IConfiguration>(config) |> ignore

    let serviceProvider = services.BuildServiceProvider()
    context.RequestServices <- serviceProvider
    context

let getResponseBody (context: HttpContext) =
    context.Response.Body.Seek(0L, SeekOrigin.Begin) |> ignore
    use reader = new StreamReader(context.Response.Body)
    reader.ReadToEnd()

let createMiddleware () =
    let mutable nextCalled = false

    let nextDelegate =
        RequestDelegate(fun _ ->
            nextCalled <- true
            Task.CompletedTask)

    let middleware = TailscaleAuthMiddleware(nextDelegate)
    (middleware, fun () -> nextCalled)

let createValidUser (email: string) (name: string) : ValidatedUser =
    let emailObj =
        Email.Create(Some email)
        |> Result.defaultWith (fun _ -> failwith "Invalid email")

    User.create name emailObj None

let createInvitedPlaceholderUser (email: string) : ValidatedUser =
    let emailObj =
        Email.Create(Some email)
        |> Result.defaultWith (fun _ -> failwith "Invalid email")

    User.invite (UserId.NewId()) emailObj

// ============================================================================
// Test Cases
// ============================================================================

[<Fact>]
let ``Returns 401 when Tailscale-User-Login header missing`` () : Task =
    task {
        // Arrange
        let context = createTestHttpContext ()
        let userRepo = MockUserRepository(Map.empty, Ok(), Ok())
        let authService = MockAuthorizationService(Ok())
        setupServices context userRepo authService None |> ignore

        let middleware, wasNextCalled = createMiddleware ()

        // Act
        do! middleware.InvokeAsync(context)

        // Assert
        Assert.Equal(401, context.Response.StatusCode)
        Assert.False(wasNextCalled ())

        let body = getResponseBody context
        Assert.Contains("Tailscale-User-Login", body)
    }

[<Fact>]
let ``Returns 401 when Tailscale-User-Login header empty`` () : Task =
    task {
        // Arrange
        let context =
            createTestHttpContext () |> fun c -> addHeader c "Tailscale-User-Login" ""

        let userRepo = MockUserRepository(Map.empty, Ok(), Ok())
        let authService = MockAuthorizationService(Ok())
        setupServices context userRepo authService None |> ignore

        let middleware, wasNextCalled = createMiddleware ()

        // Act
        do! middleware.InvokeAsync(context)

        // Assert
        Assert.Equal(401, context.Response.StatusCode)
        Assert.False(wasNextCalled ())
    }

[<Fact>]
let ``Returns 401 when email format invalid`` () : Task =
    task {
        // Arrange
        let context =
            createTestHttpContext ()
            |> fun c -> addHeader c "Tailscale-User-Login" "not-a-valid-email"

        let userRepo = MockUserRepository(Map.empty, Ok(), Ok())
        let authService = MockAuthorizationService(Ok())
        setupServices context userRepo authService None |> ignore

        let middleware, wasNextCalled = createMiddleware ()

        // Act
        do! middleware.InvokeAsync(context)

        // Assert
        Assert.Equal(401, context.Response.StatusCode)
        Assert.False(wasNextCalled ())

        let body = getResponseBody context
        Assert.Contains("Invalid", body)
    }

[<Fact>]
let ``Creates new user when email not in database`` () : Task =
    task {
        // Arrange
        let email = "newuser@example.com"

        let context =
            createTestHttpContext () |> fun c -> addHeader c "Tailscale-User-Login" email

        let userRepo = MockUserRepository(Map.empty, Ok(), Ok())
        let authService = MockAuthorizationService(Ok())
        setupServices context userRepo authService None |> ignore

        let middleware, wasNextCalled = createMiddleware ()

        // Act
        do! middleware.InvokeAsync(context)

        // Assert
        Assert.Equal(200, context.Response.StatusCode)
        Assert.True(wasNextCalled ())

        let addedUsers = userRepo.GetAddedUsers()
        Assert.Single(addedUsers) |> ignore
        Assert.Equal(email, addedUsers.[0].State.Email)

        // Verify UserId is set in context
        Assert.True(context.Items.ContainsKey("UserId"))
    }

[<Fact>]
let ``Sets UserId in HttpContext Items for existing user`` () : Task =
    task {
        // Arrange
        let email = "existing@example.com"
        let existingUser = createValidUser email "Existing User"

        let context =
            createTestHttpContext () |> fun c -> addHeader c "Tailscale-User-Login" email

        let userRepo = MockUserRepository(Map.ofList [ (email, existingUser) ], Ok(), Ok())
        let authService = MockAuthorizationService(Ok())
        setupServices context userRepo authService None |> ignore

        let middleware, wasNextCalled = createMiddleware ()

        // Act
        do! middleware.InvokeAsync(context)

        // Assert
        Assert.Equal(200, context.Response.StatusCode)
        Assert.True(wasNextCalled ())

        // Verify no new users were added
        Assert.Empty(userRepo.GetAddedUsers())

        // Verify UserId is set correctly
        Assert.True(context.Items.ContainsKey("UserId"))
        let userId = context.Items.["UserId"] :?> UserId
        Assert.Equal(existingUser.State.Id, userId)
    }

[<Fact>]
let ``Activates invited placeholder user on first login`` () : Task =
    task {
        // Arrange
        let email = "invited@example.com"
        let invitedUser = createInvitedPlaceholderUser email
        let userName = "Invited User Name"

        let context =
            createTestHttpContext ()
            |> fun c -> addHeader c "Tailscale-User-Login" email
            |> fun c -> addHeader c "Tailscale-User-Name" userName

        let userRepo = MockUserRepository(Map.ofList [ (email, invitedUser) ], Ok(), Ok())
        let authService = MockAuthorizationService(Ok())
        setupServices context userRepo authService None |> ignore

        let middleware, wasNextCalled = createMiddleware ()

        // Act
        do! middleware.InvokeAsync(context)

        // Assert
        Assert.Equal(200, context.Response.StatusCode)
        Assert.True(wasNextCalled ())

        // Verify user was updated (activated), not added
        Assert.Empty(userRepo.GetAddedUsers())
        let updatedUsers = userRepo.GetUpdatedUsers()
        Assert.Single(updatedUsers) |> ignore

        // Verify the activated user has the correct name
        let activatedUser = updatedUsers.[0]
        Assert.Equal(userName, activatedUser.State.Name)
        Assert.True(activatedUser.State.InvitedAt.IsNone) // InvitedAt should be cleared

        // Verify UserId is set
        Assert.True(context.Items.ContainsKey("UserId"))
    }

[<Fact>]
let ``Initializes org admin when email matches config`` () : Task =
    task {
        // Arrange
        let email = "admin@example.com"

        let context =
            createTestHttpContext () |> fun c -> addHeader c "Tailscale-User-Login" email

        let userRepo = MockUserRepository(Map.empty, Ok(), Ok())
        let authService = MockAuthorizationService(Ok())
        setupServices context userRepo authService (Some email) |> ignore

        let middleware, wasNextCalled = createMiddleware ()

        // Act
        do! middleware.InvokeAsync(context)

        // Assert
        Assert.Equal(200, context.Response.StatusCode)
        Assert.True(wasNextCalled ())

        // Verify org admin was initialized
        let initOrgCalls = authService.GetInitOrgCalls()
        Assert.Single(initOrgCalls) |> ignore
        Assert.Equal("default", fst initOrgCalls.[0])
    }

[<Fact>]
let ``Calls next middleware on success`` () : Task =
    task {
        // Arrange
        let email = "test@example.com"

        let context =
            createTestHttpContext () |> fun c -> addHeader c "Tailscale-User-Login" email

        let userRepo = MockUserRepository(Map.empty, Ok(), Ok())
        let authService = MockAuthorizationService(Ok())
        setupServices context userRepo authService None |> ignore

        let middleware, wasNextCalled = createMiddleware ()

        // Act
        do! middleware.InvokeAsync(context)

        // Assert
        Assert.True(wasNextCalled ())
    }

[<Fact>]
let ``Returns 500 when user creation fails`` () : Task =
    task {
        // Arrange
        let email = "newuser@example.com"

        let context =
            createTestHttpContext () |> fun c -> addHeader c "Tailscale-User-Login" email

        let userRepo =
            MockUserRepository(Map.empty, Error(ValidationError "Database error"), Ok())

        let authService = MockAuthorizationService(Ok())
        setupServices context userRepo authService None |> ignore

        let middleware, wasNextCalled = createMiddleware ()

        // Act
        do! middleware.InvokeAsync(context)

        // Assert
        Assert.Equal(500, context.Response.StatusCode)
        Assert.False(wasNextCalled ())

        let body = getResponseBody context
        Assert.Contains("Failed to create user", body)
    }

[<Fact>]
let ``Returns 500 when user activation fails`` () : Task =
    task {
        // Arrange
        let email = "invited@example.com"
        let invitedUser = createInvitedPlaceholderUser email

        let context =
            createTestHttpContext ()
            |> fun c -> addHeader c "Tailscale-User-Login" email
            |> fun c -> addHeader c "Tailscale-User-Name" "User Name"

        let userRepo =
            MockUserRepository(Map.ofList [ (email, invitedUser) ], Ok(), Error(ValidationError "Update failed"))

        let authService = MockAuthorizationService(Ok())
        setupServices context userRepo authService None |> ignore

        let middleware, wasNextCalled = createMiddleware ()

        // Act
        do! middleware.InvokeAsync(context)

        // Assert
        Assert.Equal(500, context.Response.StatusCode)
        Assert.False(wasNextCalled ())

        let body = getResponseBody context
        Assert.Contains("Failed to save activated user", body)
    }

[<Fact>]
let ``Uses Tailscale-User-Name header for display name`` () : Task =
    task {
        // Arrange
        let email = "newuser@example.com"
        let displayName = "John Doe"

        let context =
            createTestHttpContext ()
            |> fun c -> addHeader c "Tailscale-User-Login" email
            |> fun c -> addHeader c "Tailscale-User-Name" displayName

        let userRepo = MockUserRepository(Map.empty, Ok(), Ok())
        let authService = MockAuthorizationService(Ok())
        setupServices context userRepo authService None |> ignore

        let middleware, _ = createMiddleware ()

        // Act
        do! middleware.InvokeAsync(context)

        // Assert
        let addedUsers = userRepo.GetAddedUsers()
        Assert.Single(addedUsers) |> ignore
        Assert.Equal(displayName, addedUsers.[0].State.Name)
    }

[<Fact>]
let ``Uses Tailscale-User-Profile-Pic header for avatar`` () : Task =
    task {
        // Arrange
        let email = "newuser@example.com"
        let profilePicUrl = "https://example.com/avatar.jpg"

        let context =
            createTestHttpContext ()
            |> fun c -> addHeader c "Tailscale-User-Login" email
            |> fun c -> addHeader c "Tailscale-User-Profile-Pic" profilePicUrl

        let userRepo = MockUserRepository(Map.empty, Ok(), Ok())
        let authService = MockAuthorizationService(Ok())
        setupServices context userRepo authService None |> ignore

        let middleware, _ = createMiddleware ()

        // Act
        do! middleware.InvokeAsync(context)

        // Assert
        let addedUsers = userRepo.GetAddedUsers()
        Assert.Single(addedUsers) |> ignore
        Assert.Equal(Some profilePicUrl, addedUsers.[0].State.ProfilePicUrl)
    }
