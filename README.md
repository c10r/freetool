# Freetool

A free, open-source alternative to Retool for building internal tools and dashboards. Freetool helps companies create CRUD interfaces around their internal APIs with authentication, authorization, and audit logging - all without requiring developers to build custom admin interfaces.

# Contributing

## 🏗️ Architecture

This project follows **Onion Architecture** principles to maintain clean separation of concerns and enable comprehensive testing:

```
┌─────────────────────────────────────────┐
│              API Layer                  │  ← Controllers, Middleware, Models
├─────────────────────────────────────────┤
│         Infrastructure Layer            │  ← Repositories, External Services
├─────────────────────────────────────────┤
│         Application Layer               │  ← Use Cases, DTOs, Interfaces
├─────────────────────────────────────────┤
│            Domain Layer                 │  ← Entities, Value Objects, Events
└─────────────────────────────────────────┘
```

### Core Principles

- **Dependency Inversion**: All dependencies point inward toward the domain
- **Pure Business Logic**: Domain and Application layers contain no infrastructure concerns
- **Testability**: Business logic is easily unit tested without external dependencies
- **Flexibility**: Infrastructure can be swapped without affecting core functionality
- **Functional Design**: Uses F# discriminated unions and pattern matching for command handling instead of object-oriented use cases
- **Event Store Integration**: Guarantees 1:1 consistency between business operations and audit trail using transactional event sourcing

## 📊 OpenTelemetry & Distributed Tracing

Freetool includes comprehensive **OpenTelemetry (OTEL)** instrumentation with automatic business logic tracing across all layers. The system provides complete observability from HTTP requests down to database operations without requiring manual span creation.

### 🔍 Tracing Coverage

The application instruments three layers automatically:

1. **HTTP Layer**: ASP.NET Core instrumentation captures all incoming requests, response times, and HTTP status codes
2. **Business Logic Layer**: Custom AutoTracing system automatically generates spans for all command operations with detailed attributes
3. **Database Layer**: Entity Framework Core instrumentation tracks all SQL queries, connection times, and database operations

### ⚡ AutoTracing System

The AutoTracing system uses **pure reflection** and **naming conventions** to automatically generate OTEL spans and attributes for all business operations without requiring manual configuration.

#### Key Features

- **Zero Configuration**: Works automatically for all controllers - just register one line in DI
- **Naming Convention Based**: Converts `CreateUser` command to `"user.create"` span name
- **Automatic Attributes**: Extracts all command parameters and result data as OTEL attributes
- **Security First**: Automatically skips sensitive fields (password, token, secret, key, credential)
- **Architecture Compliant**: Uses pure reflection to maintain clean onion architecture separation

#### How It Works

```fsharp
// 1. Command Analysis - Automatic span name generation
CreateUser → "user.create"
GetUserById → "user.get_user_by_id"
UpdateUserEmail → "user.update_user_email"
DeleteUser → "user.delete"

// 2. Attribute Extraction - Automatic parameter capture
CreateUser(ValidatedUser { Name = "John"; Email = "john@example.com" })
→ Attributes: user.name = "John", user.email = "john@example.com"

// 3. Result Tracking - Automatic response capture
UserResult(UserDto { Id = "123"; Name = "John" })
→ Attributes: result.id = "123", result.name = "John"
```

#### Adding Tracing to New Controllers

Adding OTEL tracing to a new controller requires just **one line** in dependency injection:

```fsharp
// In Program.fs
builder.Services.AddScoped<IGenericCommandHandler<INewRepository, NewCommand, NewCommandResult>>
    (fun serviceProvider ->
        let newHandler = serviceProvider.GetRequiredService<NewHandler>()
        let activitySource = serviceProvider.GetRequiredService<ActivitySource>()
        AutoTracing.createTracingDecorator "new_entity" newHandler activitySource)
```

### 📊 Current Tracing Coverage

| Layer          | Automatic Spans     | What Gets Traced                       |
|----------------|---------------------|----------------------------------------|
| HTTP           | ✅ ASP.NET Core     | Request/response, status codes, routes |
| Business Logic | ✅ AutoTracing      | Commands, DTOs, domain operations      |
| Database       | ✅ Entity Framework | SQL queries, connection times          |
| Repository     | ❌ Not yet          | Individual repository method calls     |
| Domain Models  | ❌ Not yet          | Domain entity operations               |

### 🔧 Configuration

#### Basic Setup

```fsharp
// Configure OTEL in Program.fs
builder.Services
    .AddOpenTelemetry()
    .WithTracing(fun tracing ->
        tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("freetool-api", "1.0.0"))
            .AddSource("Freetool.Api")                    // Custom business logic spans
            .AddAspNetCoreInstrumentation()               // HTTP request/response spans
            .AddEntityFrameworkCoreInstrumentation()      // Database query spans
            .AddOtlpExporter(fun options ->
                options.Endpoint <- System.Uri("http://localhost:4317") // Jaeger/OTEL collector
                options.Protocol <- OtlpExportProtocol.Grpc))
```

#### Environment Variables

```bash
# OTEL Collector/Jaeger endpoint
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317

# Service identification
OTEL_SERVICE_NAME=freetool-api
OTEL_SERVICE_VERSION=1.0.0
```

### 📈 Observability Examples

#### Sample Trace Hierarchy
```
🌐 HTTP GET /user/123                                   [200ms]
├── 🔧 user.get_user_by_id                              [180ms]
│   ├── 🗄️ SELECT * FROM Users WHERE Id = @p0           [50ms]
│   └── 🔄 Domain Mapping & Validation                  [5ms]
└── 🌐 HTTP Response Serialization                      [15ms]
```

#### Automatic Span Attributes
```
Span: user.create
├── operation.type = "create"
├── user.name = "John Doe"
├── user.email = "john@example.com"
├── user.profile_pic_url = "https://example.com/pic.jpg"
├── result.id = "user-123"
├── result.created_at = "2024-01-15T10:30:00Z"
└── span.status = "ok"
```

### 🔐 Security & Privacy

The AutoTracing system automatically protects sensitive data:

- **Field Filtering**: Automatically skips any field containing: `password`, `token`, `secret`, `key`, `credential`
- **Configurable**: Additional sensitive patterns can be added to the `shouldSkipField` function
- **Secure by Default**: Unknown field types are safely converted to strings with null checks

## 🗃️ Event Store Integration & Audit Trail

Freetool implements a **transactional event sourcing pattern** that guarantees perfect **1:1 consistency** between business operations and audit trail. This ensures that every database change is atomically recorded as domain events, providing complete auditability and compliance.

### 🎯 Key Benefits

- **Atomic Consistency**: Events and business data are saved in the same database transaction
- **Complete Audit Trail**: Every business operation automatically generates audit events
- **Architecture Compliance**: Maintains clean onion architecture with domain events in the core
- **Type Safety**: F# discriminated unions ensure correct event structure at compile time
- **Performance**: Events stored in same database - no distributed transaction overhead

### 🔧 How It Works

#### 1. Domain Events Collection
Domain aggregates collect uncommitted events as business operations are performed:

```fsharp
// Domain Layer - User.fs
type User = {
    State: UserData                    // Business data
    UncommittedEvents: IDomainEvent list  // Events to be persisted
}

let updateName (newName: string) (user: User) : Result<User, DomainError> =
    // Business logic validation
    if String.IsNullOrWhiteSpace newName then
        Error(ValidationError "User name cannot be empty")
    else
        // Update business data
        let updatedData = { user.State with Name = newName.Trim() }

        // Collect domain event
        let nameChangedEvent = UserEvents.userUpdated user.State.Id [NameChanged(oldName, newName)]

        // Return updated aggregate with new event
        Ok {
            State = updatedData
            UncommittedEvents = user.UncommittedEvents @ [nameChangedEvent]
        }
```

#### 2. Transactional Persistence
The Infrastructure layer saves both business data and events atomically:

```fsharp
// Infrastructure Layer - UserRepository.fs
member _.UpdateAsync(user: ValidatedUser) : Task<Result<unit, DomainError>> = task {
    use transaction = context.Database.BeginTransaction()

    try
        // 1. Save business data to Users table
        let! _ = context.SaveChangesAsync()

        // 2. Save events to Events table (SAME transaction)
        let events = User.getUncommittedEvents user
        for event in events do
            do! eventRepository.SaveEventAsync event

        // 3. Commit everything atomically
        transaction.Commit()
        return Ok()
    with
    | ex ->
        transaction.Rollback()
        return Error(InvalidOperation "Transaction failed")
}
```

#### 3. Application Layer Orchestration
Command handlers use domain methods that automatically generate events:

```fsharp
// Application Layer - UserHandler.fs
| UpdateUserName(userId, dto) ->
    let! userOption = userRepository.GetByIdAsync userIdObj
    match userOption with
    | Some user ->
        // Domain method automatically creates events
        match User.updateName dto.Name user with
        | Ok updatedUser ->
            // Repository saves both data and events atomically
            match! userRepository.UpdateAsync updatedUser with
            | Ok() -> return Ok(UserResult(mapUserToDto updatedUser))
```

### 4. Event Store Schema

Events are stored in a dedicated table with full metadata:

```sql
CREATE TABLE Events (
    Id TEXT NOT NULL PRIMARY KEY,
    EventId TEXT NOT NULL,           -- Domain event ID
    EventType TEXT NOT NULL,         -- UserCreatedEvent, UserUpdatedEvent, etc.
    EntityType TEXT NOT NULL,        -- User, Tool, etc.
    EntityId TEXT NOT NULL,          -- ID of the affected entity
    EventData TEXT NOT NULL,         -- JSON serialized event data
    OccurredAt TEXT NOT NULL,        -- When the business operation happened
    CreatedAt TEXT NOT NULL          -- When the event was persisted
);
```

### 🔍 Event Types

The system generates comprehensive events for all business operations:

```fsharp
// Domain Events - UserEvents.fs
type UserCreatedEvent = {
    UserId: UserId
    Name: string
    Email: Email
    ProfilePicUrl: Url option
    OccurredAt: DateTime
    EventId: Guid
}

type UserUpdatedEvent = {
    UserId: UserId
    Changes: UserChange list          // What specifically changed
    OccurredAt: DateTime
    EventId: Guid
}

type UserChange =
    | NameChanged of oldValue: string * newValue: string
    | EmailChanged of oldValue: Email * newValue: Email
    | ProfilePicChanged of oldValue: Url option * newValue: Url option
```

### ⚡ Operational Flow

1. **User Action**: HTTP request to update user name
2. **Domain Logic**: `User.updateName` validates and creates `UserUpdatedEvent`
3. **Atomic Save**: Repository saves user data + event in single transaction
4. **Audit Trail**: Event is immediately queryable via `/audit` endpoints
5. **Tracing**: OpenTelemetry captures the entire operation flow

### 🧪 Testing Benefits

Domain events enable comprehensive testing without infrastructure dependencies:

```fsharp
[<Fact>]
let ``User name update should generate correct event`` () =
    // Arrange
    let user = User.create "John Doe" email None

    // Act
    let result = User.updateName "Jane Doe" user

    // Assert
    match result with
    | Ok updatedUser ->
        let events = User.getUncommittedEvents updatedUser
        Assert.Single(events)

        match events.[0] with
        | :? UserUpdatedEvent as event ->
            Assert.Equal("John Doe", event.Changes.[0].OldValue)
            Assert.Equal("Jane Doe", event.Changes.[0].NewValue)
```

This event store integration pattern ensures that Freetool maintains perfect audit compliance while preserving clean architecture principles and providing comprehensive observability.

## 📁 Project Structure

```
Freetool.sln
├── src/
│   ├── Freetool.Domain/              # 🎯 Pure business logic (innermost)
│   │   ├── src/
│   │   │   ├── Types.fs              # Core domain types and common definitions
│   │   │   ├── Entities/             # Domain entities (aggregates)
│   │   │   │   ├── User.fs           # User aggregate with business rules
│   │   │   │   ├── Tool.fs           # Tool/endpoint configuration
│   │   │   │   ├── Dashboard.fs      # Dashboard layout and components
│   │   │   │   └── AuditLog.fs       # Audit trail entity
│   │   │   ├── ValueObjects/         # Immutable value objects
│   │   │   │   ├── UserId.fs         # Strongly-typed user identifier
│   │   │   │   ├── ToolId.fs         # Strongly-typed tool identifier
│   │   │   │   ├── Email.fs          # Email with validation rules
│   │   │   │   └── Permissions.fs    # Permission and role definitions
│   │   │   ├── Services/             # Domain services (pure functions)
│   │   │   │   ├── ToolValidation.fs # Tool configuration validation logic
│   │   │   │   ├── PermissionService.fs # Permission calculation and checking
│   │   │   │   └── AuditService.fs   # Audit event creation logic
│   │   │   └── Events/               # Domain events for integration
│   │   │       ├── UserEvents.fs     # User-related domain events
│   │   │       ├── ToolEvents.fs     # Tool-related domain events
│   │   │       └── AuditEvents.fs    # Audit-related domain events
│   │   └── test/                     # 🧪 Pure unit tests (fast)
│   │
│   ├── Freetool.Application/         # 🔧 Application orchestration
│   │   ├── src/
│   │   │   ├── DTOs/                 # Data transfer objects for boundaries
│   │   │   │   └── UserDtos.fs       # User-related DTOs
│   │   │   ├── Interfaces/           # Repository and service contracts
│   │   │   │   ├── IUserRepository.fs # User data access interface
│   │   │   │   ├── IToolRepository.fs # Tool data access interface
│   │   │   │   ├── IDashboardRepository.fs
│   │   │   │   ├── IAuditRepository.fs
│   │   │   │   └── IEmailService.fs  # Email service interface
│   │   │   ├── Commands/             # Command definitions using discriminated unions
│   │   │   │   └── UserCommands.fs   # User commands and result types
│   │   │   ├── Handlers/             # Command handlers using pattern matching
│   │   │   │   └── UserHandler.fs    # User command handler module
│   │   │   └── Common/               # Shared application utilities
│   │   │       ├── Result.fs         # Result type for error handling
│   │   │       ├── Validation.fs     # Cross-cutting validation logic
│   │   │       └── Mapping.fs        # Domain ↔ DTO conversions
│   │   └── test/                     # 🧪 Application logic tests (fast)
│   │
│   ├── Freetool.Infrastructure/      # 🔌 External system integrations
│   │   ├── src/
│   │   │   ├── Database/             # Data persistence layer
│   │   │   │   ├── FreetoolDbContext.fs # Entity Framework context
│   │   │   │   ├── UserEntity.fs     # Database entity mappings
│   │   │   │   ├── Persistence.fs    # Database migration utilities
│   │   │   │   ├── Repositories/     # Repository implementations
│   │   │   │   │   └── UserRepository.fs # User repository implementation
│   │   │   │   └── Migrations/       # Database schema migrations
│   │   │   │       └── DatabaseUpgradeScripts.DBUP.001_CreateUsersTable.sql
│   │   │   ├── ExternalServices/     # Third-party service integrations
│   │   │   │   ├── EmailService.fs   # SMTP email implementation
│   │   │   │   ├── HttpClientService.fs # HTTP client for external APIs
│   │   │   │   └── CacheService.fs   # Redis/in-memory caching
│   │   │   ├── Security/             # Authentication and authorization
│   │   │   │   ├── JwtTokenService.fs # JWT token creation/validation
│   │   │   │   ├── PasswordService.fs # Password hashing (bcrypt)
│   │   │   │   └── AuthorizationService.fs
│   │   │   └── Configuration/        # Infrastructure configuration
│   │   └── test/                     # 🧪 Infrastructure tests
│   │
│   └── Freetool.Api/                 # 🌐 HTTP API (outermost layer)
│       ├── src/
│       │   ├── Controllers/          # ASP.NET Core controllers
│       │   │   ├── UserController.fs # User management endpoints
│       │   │   ├── ToolController.fs # Tool CRUD endpoints
│       │   │   ├── DashboardController.fs
│       │   │   └── AuditController.fs # Audit log retrieval
│       │   ├── Middleware/           # HTTP middleware pipeline
│       │   │   ├── AuthenticationMiddleware.fs
│       │   │   ├── AuthorizationMiddleware.fs
│       │   │   ├── AuditMiddleware.fs # Request/response audit logging
│       │   │   └── ErrorHandlingMiddleware.fs
│       │   ├── Models/               # HTTP request/response models
│       │   ├── Program.fs            # Application entry point & DI configuration
│       │   ├── appsettings.json      # Production configuration
│       │   ├── appsettings.Development.json # Development configuration
│       │   └── Properties/
│       │       └── launchSettings.json # Launch profiles
│       └── test/                     # 🔗 End-to-end integration tests
│
└── docs/                             # 📚 Additional documentation
```

## 🚀 Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Git](https://git-scm.com/)

### Database Setup

This project uses **SQLite** with **DBUp** for database migrations. SQLite is a lightweight, file-based database that works perfectly on Windows, macOS, and Linux with zero configuration required.

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/freetool.git
   cd freetool
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure database (optional)**
   The default configuration uses SQLite with a local file. The database file `freetool.db` will be created automatically in the API project directory. If needed, edit `src/Freetool.Api/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Data Source=freetool.db"
     }
   }
   ```

4. **Start the application**
   ```bash
   docker-compose up --build
   ```

   **The database will be created automatically on first run!** The application uses DBUp to:
   - Create the database if it doesn't exist
   - Run all migration scripts automatically
   - Display migration progress in the console

5. **Access the API**
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5001/swagger
   - OTEL traces: http://localhost:18888/

### Quick Test

Once the application is running, you can test the User API:

1. **Open Swagger UI** at http://localhost:5001/swagger
2. **Create a user** using the `POST /user` endpoint:
   ```json
   {
     "name": "John Doe",
     "email": "john.doe@example.com",
     "profilePicUrl": "https://example.com/profile.jpg"
   }
   ```
3. **Get users** using the `GET /user` endpoint
4. **Get user by ID** using the `GET /user/{id}` endpoint with the returned ID

The API supports full CRUD operations:
- `POST /user` - Create a new user
- `GET /user/{id}` - Get user by ID
- `GET /user/email/{email}` - Get user by email
- `GET /user?skip=0&take=10` - Get paginated list of users
- `PUT /user/{id}/name` - Update user name
- `PUT /user/{id}/email` - Update user email
- `PUT /user/{id}/profile-picture` - Set user profile picture
- `DELETE /user/{id}/profile-picture` - Remove user profile picture
- `DELETE /user/{id}` - Delete user

### Database Migrations

This project uses [DBUp](https://dbup.readthedocs.io/) for database migrations instead of Entity Framework migrations. This gives you full control over your SQL scripts.

#### Adding New Migrations

1. **Create a new SQL script** in `src/Freetool.Infrastructure/src/Database/Migrations/`
   - File naming convention: `DatabaseUpgradeScripts.DBUP.{number}_{description}.sql`
   - Example: `DatabaseUpgradeScripts.DBUP.002_AddUserPreferencesTable.sql`

2. **Add the script to the project file** as an embedded resource:
   ```xml
   <EmbeddedResource Include="src/Database/Migrations/DatabaseUpgradeScripts.DBUP.002_AddUserPreferencesTable.sql" />
   ```

3. **Restart the application** - DBUp will automatically detect and run new scripts

#### Example Migration Script
```sql
-- DatabaseUpgradeScripts.DBUP.002_AddUserPreferencesTable.sql
CREATE TABLE UserPreferences (
    Id TEXT NOT NULL PRIMARY KEY,
    UserId TEXT NOT NULL,
    Theme TEXT NOT NULL DEFAULT 'Light',
    Language TEXT NOT NULL DEFAULT 'en',
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);
```

#### Migration Benefits
- **Version control friendly**: SQL scripts are checked into source control
- **Database agnostic**: Easy to switch between SQL Server, SQLite, PostgreSQL, etc.
- **Full SQL control**: Write optimized SQL for complex migrations
- **Rollback support**: Create explicit down migration scripts when needed
- **Team collaboration**: No merge conflicts with migration files

## 🧪 Testing Strategy

### Unit Tests (Fast & Isolated)
- **Domain Layer**: Test business rules, entity behavior, and domain services
- **Application Layer**: Test use case orchestration and validation logic
- Run with: `dotnet test src/Freetool.Domain/test src/Freetool.Application/test`

### Integration Tests (Realistic & Comprehensive)
- **Infrastructure Layer**: Test database repositories with real SQL Server
- **API Layer**: Test HTTP endpoints with full middleware pipeline
- Run with: `dotnet test src/Freetool.Infrastructure/test src/Freetool.Api/test`

### Test Organization Principles
- Tests are colocated with source code in each project's `test/` folder
- Domain and Application tests should be **fast** (milliseconds) and require **no external dependencies**
- Integration tests may be **slower** but provide **high confidence** in real-world scenarios
- Use test databases and mock external services in integration tests

## 🛠️ Development Workflow

### Adding New Features

1. **Start with Domain**: Define entities, value objects, and business rules
2. **Add Application Logic**: Create use cases and define interfaces
3. **Implement Infrastructure**: Build repository implementations and external service integrations
4. **Expose via API**: Create controllers and HTTP models
5. **Test Each Layer**: Unit tests for inner layers, integration tests for outer layers

### File Ordering in F# Projects

F# requires dependencies to be ordered correctly in `.fsproj` files. Always ensure:
- Value objects come before entities that use them
- Domain services come after the entities they operate on
- Interfaces are defined before their implementations

### Code Standards

- Follow F# naming conventions (PascalCase for types, camelCase for values)
- Write comprehensive unit tests for Domain and Application layers
- Add integration tests for Infrastructure and API changes
- Keep business logic pure and free of infrastructure concerns
- Use meaningful commit messages following [Conventional Commits](https://www.conventionalcommits.org/)

# 💻 Deploying

Freetool is meant to be deployed behind a [Tailscale](https://tailscale.com) network **under the /freetool** path.

When deployed via Tailscale Serve, Tailscale will automatically set [identity headers](https://tailscale.com/kb/1312/serve#identity-headers), obviating the need for this app to handle auth at all.

```bash
tailscale serve --bg --set-path=/freetool 5001
```

# 📄 License & 🙏 Acknowledgements

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

- Inspired by [Retool](https://retool.com/) and the need for open-source internal tooling
- Built with [F#](https://fsharp.org/) and [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)
- Architecture influenced by [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) and [Onion Architecture](https://jeffreypalermo.com/2008/07/the-onion-architecture-part-1/) principles