# Freetool

A free, open-source alternative to Retool for building internal tools and dashboards. Freetool helps companies create CRUD interfaces around their internal APIs with authentication, authorization, and audit logging - all without requiring developers to build custom admin interfaces.

## 🎯 Project Vision

Freetool enables non-technical team members to safely interact with internal APIs through a user-friendly interface. Instead of asking developers to create one-off admin tools or exposing raw API endpoints, teams can use Freetool to:

- **Wrap APIs with UI**: Transform REST endpoints into intuitive forms and dashboards
- **Control Access**: Fine-grained permissions and role-based access control
- **Track Everything**: Comprehensive audit logging for compliance and debugging
- **Self-Service**: Empower business users to perform operations independently

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
│   │   │   ├── Interfaces/           # Repository and service contracts
│   │   │   │   ├── IUserRepository.fs # User data access interface
│   │   │   │   ├── IToolRepository.fs # Tool data access interface
│   │   │   │   ├── IDashboardRepository.fs
│   │   │   │   ├── IAuditRepository.fs
│   │   │   │   └── IEmailService.fs  # Email service interface
│   │   │   ├── UseCases/             # Application use cases (commands/queries)
│   │   │   │   ├── UserManagement/   # User registration, authentication
│   │   │   │   ├── ToolManagement/   # CRUD operations for tools
│   │   │   │   └── Dashboard/        # Dashboard creation and management
│   │   │   ├── DTOs/                 # Data transfer objects for boundaries
│   │   │   └── Common/               # Shared application utilities
│   │   │       ├── Result.fs         # Result type for error handling
│   │   │       ├── Validation.fs     # Cross-cutting validation logic
│   │   │       └── Mapping.fs        # Domain ↔ DTO conversions
│   │   └── test/                     # 🧪 Application logic tests (fast)
│   │
│   ├── Freetool.Infrastructure/      # 🔌 External system integrations
│   │   ├── src/
│   │   │   ├── Database/             # Data persistence layer
│   │   │   │   ├── Repositories/     # Repository implementations
│   │   │   │   ├── Migrations/       # Database schema migrations
│   │   │   │   └── DbContext.fs      # Entity Framework context
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
│       │   ├── Startup.fs            # Dependency injection configuration
│       │   ├── Program.fs            # Application entry point
│       │   └── appsettings.json
│       └── test/                     # 🔗 End-to-end integration tests
│
└── docs/                             # 📚 Additional documentation
```

## 🚀 Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server) (or SQL Server Express/LocalDB for development)
- [Git](https://git-scm.com/)

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

3. **Update database connection string**
   Edit `src/Freetool.Api/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FreetoolDev;Trusted_Connection=true;"
     }
   }
   ```

4. **Run database migrations**
   ```bash
   dotnet ef database update --project src/Freetool.Infrastructure --startup-project src/Freetool.Api
   ```

5. **Start the application**
   ```bash
   dotnet run --project src/Freetool.Api
   ```

6. **Access the API**
   - API: https://localhost:5001
   - Swagger UI: https://localhost:5001/swagger

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

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Write tests for your changes
4. Ensure all tests pass (`dotnet test`)
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Code Standards

- Follow F# naming conventions (PascalCase for types, camelCase for values)
- Write comprehensive unit tests for Domain and Application layers
- Add integration tests for Infrastructure and API changes
- Keep business logic pure and free of infrastructure concerns
- Use meaningful commit messages following [Conventional Commits](https://www.conventionalcommits.org/)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by [Retool](https://retool.com/) and the need for open-source internal tooling
- Built with [F#](https://fsharp.org/) and [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)
- Architecture influenced by [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) and [Onion Architecture](https://jeffreypalermo.com/2008/07/the-onion-architecture-part-1/) principles