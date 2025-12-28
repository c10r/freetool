# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Freetool is an open-source internal tools platform (Retool alternative) built with F# and ASP.NET Core. The backend uses **Onion Architecture** with functional design patterns, while the frontend (in `www/`) is a React/TypeScript SPA using Vite, React Router, and shadcn/ui components.

## Essential Commands

### Backend (F#/.NET)
```bash
# Restore dependencies
dotnet restore Freetool.sln

# Build all projects
dotnet build Freetool.sln -c Release

# Run API locally (serves from www/ for static files)
dotnet run --project src/Freetool.Api/Freetool.Api.fsproj

# Run all tests
dotnet test Freetool.sln

# Run tests for specific project
dotnet test src/Freetool.Domain/test/Freetool.Domain.Tests.fsproj

# Run specific test by name filter
dotnet test --filter "FullyQualifiedName~NameOfTest"

# Format code
dotnet format Freetool.sln
```

### Frontend (React/TypeScript)
```bash
cd www

# Install dependencies
npm install

# Development server
npm run dev

# Build for production
npm run build

# Build for development (with source maps)
npm run build:dev

# Lint
npm run lint

# Format
npm run format
```

### Docker
```bash
# Start all services (API, OpenFGA, Aspire Dashboard)
docker-compose up --build

# Start specific service
docker-compose up --build freetool-api

# View logs
docker-compose logs -f freetool-api
```

**Service URLs:**
- API: http://localhost:5001
- Swagger UI: http://localhost:5001/swagger
- OTEL/Aspire Dashboard: http://localhost:18888
- OpenFGA: http://localhost:8090
- OpenFGA Playground: http://localhost:3030

## Architecture Overview

### Layered Onion Architecture

The F# backend follows strict dependency inversion - all dependencies point inward toward the domain:

**1. Domain Layer** (`src/Freetool.Domain/`) - Innermost layer, zero dependencies
- **Entities/**: Aggregates with business rules (User, App, Resource, Folder, Workspace, Group, Run)
  - Each entity is an F# record with a `State` field (business data) and `UncommittedEvents` list
  - Domain methods are pure functions that validate, update state, and collect events
  - Example: `App.updateName` validates the name, updates the aggregate, and adds an `AppUpdatedEvent`
- **ValueObjects/**: Immutable value types with validation (UserId, Email, AppName, etc.)
- **Events/**: Domain events representing business facts (UserCreatedEvent, AppUpdatedEvent, etc.)
- **Services/**: Pure domain services for complex business logic
- **Types.fs**: Core domain types (`DomainError` discriminated union, etc.)
- **EventSourcingAggregate.fs**: Base aggregate pattern for event collection

**2. Application Layer** (`src/Freetool.Application/`) - Orchestration and use cases
- **Commands/**: Command discriminated unions define all operations
  - Pattern: `type UserCommand = CreateUser of ... | UpdateUser of ... | DeleteUser of ...`
  - Result types: `type UserCommandResult = UserResult of UserDto | UsersResult of PagedResult<UserDto> | ...`
- **Handlers/**: Handler modules implement command pattern matching
  - Pattern: Module-based handlers (e.g., `UserHandler`) with `handleCommand` function
  - Each handler matches command cases and orchestrates domain + repository calls
  - Handlers implement `IGenericCommandHandler<TRepository, TCommand, TResult>` interface
- **DTOs/**: Data transfer objects for API boundary (no business logic)
- **Interfaces/**: Repository and service contracts (e.g., `IUserRepository`, `IAuthorizationService`)
- **Mappers/**: Domain ↔ DTO conversions
- **Services/**: Application services (e.g., `EventEnhancementService` for enriching audit events)

**3. Infrastructure Layer** (`src/Freetool.Infrastructure/`) - External integrations
- **Database/**: Entity Framework Core, repositories, migrations
  - **FreetoolDbContext.fs**: EF Core DbContext
  - **Repositories/**: Repository implementations (UserRepository, AppRepository, etc.)
  - **Migrations/**: DBUp SQL scripts named `DatabaseUpgradeScripts.DBUP.{number}_{description}.sql`
  - Migration pattern: Embedded resources, auto-run on startup
- **Services/**: External service integrations
  - **OpenFgaService.fs**: OpenFGA client for authorization
  - **EventPublisher.fs**: Event publishing infrastructure

**4. API Layer** (`src/Freetool.Api/`) - HTTP endpoints
- **Controllers/**: ASP.NET Core controllers (UserController, AppController, etc.)
  - Controllers call handlers via DI-injected `IGenericCommandHandler` instances
- **Middleware/**: HTTP middleware (AuthzMiddleware for OpenFGA checks)
- **Tracing/**: AutoTracing system for OpenTelemetry instrumentation
- **Program.fs**: Application entry point and DI configuration

### Command/Handler Pattern

This codebase uses a **functional command pattern** instead of OOP use cases:

1. **Define Command DU**: Create discriminated union in `Commands/` (e.g., `AppCommand`)
2. **Define Result DU**: Create result type for all possible outcomes (e.g., `AppCommandResult`)
3. **Implement Handler**: Create handler module in `Handlers/` with `handleCommand` function
   - Use pattern matching on command cases
   - Call domain methods (which create events)
   - Call repository to persist (events saved atomically with data)
4. **Wire in DI**: Register handler in `Program.fs` with AutoTracing decorator
5. **Call from Controller**: Inject `IGenericCommandHandler` and invoke `HandleCommand`

**Example Flow:**
```fsharp
// 1. Command definition
type AppCommand = UpdateAppName of actorUserId: UserId * appId: string * UpdateAppNameDto

// 2. Handler pattern match
match command with
| UpdateAppName(actorUserId, appId, dto) ->
    let! app = repository.GetByIdAsync appId
    // Domain method validates and creates event
    match App.updateName actorUserId dto.Name app with
    | Ok updatedApp ->
        // Repository saves data + events atomically
        repository.UpdateAsync updatedApp
```

### Event Sourcing Pattern

**Transactional Event Sourcing** ensures 1:1 consistency between business operations and audit trail:

1. **Domain Layer**: Entities collect uncommitted events as methods execute
   - Pattern: Each domain method returns updated entity with new events appended
   - Events stored in `UncommittedEvents: IDomainEvent list` field
2. **Application Layer**: Handlers orchestrate domain + repository
3. **Infrastructure Layer**: Repositories save business data + events in **same transaction**
   - Pattern: Begin transaction → Save entity → Save events → Commit
   - `EventRepository.SaveEventAsync` persists to `Events` table
4. **Audit API**: Events queryable via audit endpoints

**Event Schema:**
- `Events` table stores all domain events with metadata (EntityId, EventType, EventData JSON, OccurredAt)
- Events are immutable - never updated or deleted

## OpenTelemetry AutoTracing System

The codebase has a **zero-configuration tracing system** that automatically instruments all command handlers:

### How It Works
1. **Naming Convention**: Command names auto-generate span names
   - `CreateApp` → `"app.create"`
   - `UpdateAppName` → `"app.update_app_name"`
2. **Automatic Attributes**: Command parameters and results extracted as OTEL attributes
3. **Security**: Sensitive fields (password, token, secret, key, credential) automatically filtered
4. **Pure Reflection**: Uses F# reflection to maintain architecture separation

### Adding Tracing to New Handlers

In `Program.fs`, wrap your handler with `AutoTracing.createTracingDecorator`:

```fsharp
builder.Services.AddScoped<IGenericCommandHandler<IAppRepository, AppCommand, AppCommandResult>>
    (fun serviceProvider ->
        let handler = serviceProvider.GetRequiredService<AppHandler>()
        let activitySource = serviceProvider.GetRequiredService<ActivitySource>()
        AutoTracing.createTracingDecorator "app" handler activitySource)
```

**Parameters:**
- First arg: Entity name prefix for spans (e.g., `"app"`, `"user"`, `"folder"`)
- Second arg: Handler instance
- Third arg: ActivitySource for OTEL

## OpenFGA Authorization

Fine-grained, relationship-based authorization via OpenFGA (Google Zanzibar):

### Model Hierarchy
- **Organization**: Global admins with all permissions on all workspaces
- **Team**: Team admins + members, scoped to specific workspaces
- **Workspace**: Top-level container with 10 permissions (create/edit/delete for resource/app/folder, plus run_app)
- **Permissions**: Relationship tuples like `(user:alice, create_app, workspace:main)`

### Authorization Checks

Controllers use `AuthzMiddleware` which:
1. Extracts user from Tailscale identity headers
2. Checks permission via `IAuthorizationService.CheckPermissionAsync`
3. Returns 403 if unauthorized

**Manual Check Example:**
```fsharp
let! canCreate = authService.CheckPermissionAsync("user:alice", "create_resource", "workspace:main")
if not canCreate then
    return Error(PermissionDenied "Insufficient permissions")
```

### Managing Relationships

```fsharp
// Create relationship (grant permission)
authService.CreateRelationshipsAsync([
    { User = "user:bob"; Relation = "create_app"; Object = "workspace:eng" }
])

// Update relationships atomically (e.g., promote member to admin)
authService.UpdateRelationshipsAsync({
    TuplesToAdd = [{ User = "user:carol"; Relation = "admin"; Object = "team:eng" }]
    TuplesToRemove = [{ User = "user:carol"; Relation = "member"; Object = "team:eng" }]
})

// Delete relationship (revoke permission)
authService.DeleteRelationshipsAsync([
    { User = "user:dave"; Relation = "run_app"; Object = "workspace:eng" }
])
```

## Database Migrations with DBUp

The project uses **DBUp** (not EF migrations) for full SQL control:

### Adding a Migration
1. **Create SQL file** in `src/Freetool.Infrastructure/src/Database/Migrations/`
   - Naming: `DatabaseUpgradeScripts.DBUP.{number}_{description}.sql`
   - Example: `DatabaseUpgradeScripts.DBUP.005_AddColumnToApps.sql`
2. **Add to .fsproj** as embedded resource:
   ```xml
   <EmbeddedResource Include="src/Database/Migrations/DatabaseUpgradeScripts.DBUP.005_AddColumnToApps.sql" />
   ```
3. **Restart app** - DBUp auto-detects and runs new scripts

**Benefits:**
- Version controlled SQL scripts
- Database agnostic (SQLite, Postgres, SQL Server)
- No merge conflicts with migration files
- Explicit rollback scripts when needed

## F# Project File Ordering

F# requires strict file ordering in `.fsproj` - dependencies must appear before dependents:

**Correct Order:**
1. Value objects (e.g., `UserId.fs`, `Email.fs`)
2. Core types (e.g., `Types.fs`)
3. Entities that use value objects (e.g., `User.fs`, `App.fs`)
4. Services that use entities
5. Handlers that use everything

**If you add a new file**, ensure it's ordered correctly or you'll get compilation errors.

## Testing Strategy

Tests colocated in `test/` subdirectories within each project:

### Domain Tests (`src/Freetool.Domain/test/`)
- **Fast, pure, isolated** - no external dependencies
- Test business rules, entity behavior, event generation
- Use xUnit: `[<Fact>]` for single scenarios, `[<Theory>]` for data-driven
- Example: Test that `App.updateName` generates correct `AppUpdatedEvent`

### Application Tests (`src/Freetool.Application/test/`)
- Test handler orchestration, DTO mapping, validation
- Mock repositories using interfaces
- Verify command handling logic

### Infrastructure Tests (`src/Freetool.Infrastructure/test/`)
- Test repositories with in-memory database
- Test OpenFGA integration
- Test event persistence

### API Tests (`src/Freetool.Api/test/`)
- End-to-end integration tests
- Test HTTP endpoints with full middleware pipeline
- Test authorization checks

**Running Specific Tests:**
```bash
# All tests
dotnet test

# Single project
dotnet test src/Freetool.Domain/test/

# By name filter
dotnet test --filter "FullyQualifiedName~UpdateApp"
```

## Frontend Architecture

The `www/` directory contains a React/TypeScript SPA:

### Key Technologies
- **Vite**: Build tool and dev server
- **React Router**: Client-side routing
- **shadcn/ui**: UI component library (Radix UI + Tailwind)
- **TanStack Query**: Server state management
- **React Hook Form + Zod**: Form handling and validation
- **openapi-fetch**: Type-safe API client generated from OpenAPI spec

### Structure
- `src/components/`: Reusable UI components
- `src/pages/`: Page components (routes)
- `src/lib/`: Utilities, API client, hooks
- `src/hooks/`: Custom React hooks

### API Integration
The frontend uses `openapi-fetch` to generate a type-safe client from `openapi.spec.json` at the repo root. After backend API changes:
1. Regenerate OpenAPI spec (if using Swagger gen)
2. Regenerate TypeScript types: `npm run generate-api-types` (if configured)
3. Frontend automatically gets type checking for API calls

## Deployment

Freetool is designed to run behind **Tailscale Serve** at the `/freetool` path:

```bash
tailscale serve --bg --set-path=/freetool 5001
```

**Authentication:** Tailscale automatically sets identity headers, so the app doesn't handle auth directly - it trusts Tailscale's headers for user identity.

## Configuration

### Environment Variables
- `ASPNETCORE_ENVIRONMENT`: `Development` or `Production`
- `OTEL_EXPORTER_OTLP_ENDPOINT`: OpenTelemetry collector endpoint (default: `http://localhost:4317`)
- `OTEL_SERVICE_NAME`: Service name for traces (default: `freetool-api`)
- `OpenFGA:ApiUrl`: OpenFGA server URL (default: `http://openfga:8090`)
- `OpenFGA:StoreId`: OpenFGA store ID (empty until store created)

### appsettings Files
- `appsettings.json`: Base configuration
- `appsettings.Development.json`: Development overrides (SQLite connection string, etc.)
- `appsettings.Production.json`: Production settings (not committed)

**Secret Management:** Use user secrets or environment variables for sensitive config - never commit secrets.

## Common Patterns

### Adding a New Entity

1. **Domain Layer:**
   - Create value object (e.g., `NewEntityId.fs`) in `ValueObjects/`
   - Create entity (e.g., `NewEntity.fs`) in `Entities/` with business logic
   - Create events (e.g., `NewEntityEvents.fs`) in `Events/`
   - Update `.fsproj` file ordering

2. **Application Layer:**
   - Create DTOs in `DTOs/NewEntityDtos.fs`
   - Create commands in `Commands/NewEntityCommands.fs`
   - Create handler in `Handlers/NewEntityHandler.fs`
   - Create repository interface in `Interfaces/INewEntityRepository.fs`
   - Create mapper in `Mappers/NewEntityMapper.fs`

3. **Infrastructure Layer:**
   - Create EF entity mapping if needed
   - Create repository in `Database/Repositories/NewEntityRepository.fs`
   - Add migration script
   - Update `.fsproj` file ordering

4. **API Layer:**
   - Create controller in `Controllers/NewEntityController.fs`
   - Register dependencies in `Program.fs` (repository, handler with tracing)
   - Update `.fsproj` file ordering

5. **Test Each Layer:**
   - Domain tests for business rules
   - Application tests for handler logic
   - Infrastructure tests for repository
   - API tests for endpoints

### Adding a New Command to Existing Entity

1. Add command case to command DU in `Commands/`
2. Add handler case in `Handlers/` (pattern match new command)
3. Create domain method if needed (or reuse existing)
4. Add controller endpoint in `Controllers/`
5. Add tests

**Note:** AutoTracing automatically picks up new commands - no extra configuration needed!

## Common Gotchas

1. **F# File Ordering**: Compilation errors about "not defined" usually mean wrong file order in `.fsproj`
2. **Event Not Saved**: Ensure repository calls `eventRepository.SaveEventAsync` in transaction
3. **Tracing Missing**: Check that handler is wrapped with `AutoTracing.createTracingDecorator` in DI
4. **Authorization Failing**: Verify OpenFGA store has correct relationships (`CreateRelationshipsAsync`)
5. **Migration Not Running**: Ensure SQL file is marked as `<EmbeddedResource>` in `.fsproj`
6. **Frontend Type Errors**: Regenerate types from OpenAPI spec after backend changes

## Code Style

- **Indentation**: 4 spaces (F# standard)
- **Naming**: PascalCase for types/modules/DU cases, camelCase for functions/values
- **Immutability**: Prefer immutable records, pure functions
- **Pattern Matching**: Exhaustive pattern matching on DUs (compiler enforces)
- **Error Handling**: Use `Result<'T, DomainError>` for domain operations
- **Comments**: Avoid obvious comments; code should be self-documenting
- **Format Before Commit**: Run `dotnet format Freetool.sln`
