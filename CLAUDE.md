# Claude Code Guidelines

Guidelines and instructions for Claude Code when working in this project.

## Table of Contents

1. [About AESIR](#about-aesir)
2. [Project Configuration](#project-configuration)
3. [Code Generation](#code-generation)
4. [Data Access Guidelines](#data-access-guidelines)
5. [API Documentation](#api-documentation)
6. [Code Style Guidelines](#code-style-guidelines)
7. [Error Handling](#error-handling)
8. [Logging Guidelines](#logging-guidelines)
9. [Docker & Kubernetes](#docker--kubernetes)
10. [Blazor WebAssembly Client](#blazor-webassembly-client)
11. [Testing Guidelines](#testing-guidelines)
12. [Development Environment](#development-environment)
13. [Planning & Workflow](#planning--workflow)

---

## About AESIR

AESIR is an AI-powered chat orchestration platform with:
- **Server**: .NET 10 modular API with PostgreSQL
- **Client**: Blazor WebAssembly with Tauri desktop support
- **Features**: Multi-model inference (OpenAI, Ollama), document handling, speech-to-text, MCP tools

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Client Applications                       │
├───────────────────┬───────────────────┬─────────────────────┤
│   Blazor WASM     │   Tauri Desktop   │   (Future: Mobile)  │
└─────────┬─────────┴─────────┬─────────┴──────────┬──────────┘
          │                   │                    │
          └───────────────────┼────────────────────┘
                              │ HTTPS (Traefik)
          ┌───────────────────▼───────────────────┐
          │           Aesir.Api.Server            │
          │        https://aesir.localhost        │
          └───────────────────┬───────────────────┘
                              │
   ┌──────────────────────────┼──────────────────────────┐
   │           │              │              │           │
┌──▼──┐   ┌────▼────┐   ┌─────▼─────┐   ┌────▼────┐   ┌──▼──┐
│Chat │   │ Config  │   │ Inference │   │ Storage │   │ ... │
└─────┘   └─────────┘   └───────────┘   └─────────┘   └─────┘
                              │
          ┌───────────────────▼───────────────────┐
          │              PostgreSQL               │
          └───────────────────────────────────────┘
```

### Quick Start

#### Development Scripts

| Script | Purpose | Options |
|--------|---------|---------|
| `./run-server.sh` | Start API server (Docker) | `--rebuild` `--prune` |
| `./run-blazor.sh` | Start Blazor dev server | `--clean-build` |
| `./run-tauri.sh` | Start Tauri desktop app | Requires Blazor running |

#### Getting Started

1. **Start API**: `./run-server.sh`
   - Builds Docker image, starts PostgreSQL/Traefik/Qdrant
   - Auto-tails API logs (Ctrl+C to stop following)

2. **Start Client**: `./run-blazor.sh`
   - Auto-kills existing processes on port 5173
   - Starts dev server (no hot reload - rebuild required for changes)

3. **Open**: http://localhost:5173/ or https://aesir.localhost

#### Script Options

- **run-server.sh**
  - `--rebuild`: Force full rebuild (no Docker cache)
  - `--prune`: Clean up Docker resources before building

- **run-blazor.sh**
  - `--clean-build`: Clean and rebuild before starting

- **run-tauri.sh**
  - Requires Blazor dev server running first in separate terminal

---

## Project Configuration

- **Target Framework**: .NET 10.0
- **C# Version**: C# 13
- All projects must target `net10.0` framework

---

## Code Generation

### Context7 Documentation

When generating code involving external libraries, you MUST fetch up-to-date documentation first:

```
User Request → Identify Libraries → Resolve Library ID → Fetch Docs → Generate Code
```

**Tools:**
- `mcp__context7__resolve-library-id` - Find library ID
- `mcp__context7__get-library-docs` - Retrieve documentation

**When to use Context7:**
- Adding FluentMigrator migrations → Fetch FluentMigrator docs
- Creating MudBlazor components → Fetch MudBlazor docs
- Implementing SignalR hubs → Fetch SignalR docs
- Writing Dapper queries → Fetch Dapper docs
- Configuring NLog → Fetch NLog docs

**Query parameters:**
- `topic`: Focus on relevant sections (e.g., "hooks", "routing")
- `tokens`: Default 5000, use 10000+ for complex topics

### Enforcement

Do not generate library-specific code without first consulting Context7 documentation. If documentation is unavailable, inform the user and proceed with caution, noting limitations.

---

## Data Access Guidelines

### Database Naming Convention

**ALL database identifiers MUST use `aesir_` prefix with lowercase snake_case:**

| Type | Convention | Example |
|------|------------|---------|
| Tables | `aesir_{name}` | `aesir_user`, `aesir_chat_session` |
| Columns | `snake_case` | `first_name`, `is_active`, `created_at` |
| Indexes | `ix_aesir_{table}_{column}` | `ix_aesir_user_username` |
| C# Properties | `PascalCase` | `FirstName`, `IsActive` |

**Automatic Mapping**: `DapperColumnMapper` converts PascalCase properties to snake_case columns.

### ORM and Database

- **ORM**: Dapper 2.1.66 and Dapper.Contrib 2.0.78 (not Entity Framework)
- **Database**: PostgreSQL 15+
- **Column Mapping**: Initialize `DapperColumnMapper.Initialize()` in `Program.cs`

### Migrations

- **Tool**: FluentMigrator 7.1.0
- **Location**: `[ModuleProject]/Migrations/` (e.g., `Aesir.Modules.Chat/Migrations/`)
- **Auto-Discovery**: Migrations discovered from `Aesir.Modules.*` assemblies
- **Naming**: Timestamp format `[Migration(YYYYMMDDHHMMSS)]`
- **Requirements**: Always implement both `Up()` and `Down()` methods
- **Primary Keys**: Use `.AsGuid()` (NOT `.AsInt32().Identity()`)

```csharp
using FluentMigrator;

namespace Aesir.Modules.Products.Migrations;

[Migration(20250119000002)]
public class AddProductsTable : Migration
{
    public override void Up()
    {
        Create.Table("aesir_product")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable();

        Create.Index("ix_aesir_product_name")
            .OnTable("aesir_product")
            .OnColumn("name");
    }

    public override void Down()
    {
        Delete.Table("aesir_product");
    }
}
```

### Connection Management

- Use `IDbConnectionFactory` with connection factory pattern
- Always wrap connections in `using` statements
- Use `async/await` with `.ConfigureAwait(false)`

### Entity Design

- **Primary Keys**: ALL entities use `Guid` type
- **Property Naming**: PascalCase in C#
- **No Attributes Required**: Entity classes don't need `[Table]` or `[ExplicitKey]` attributes (raw SQL handles table names)

```csharp
public class User : IEntity
{
    public Guid Id { get; set; }        // Maps to: id
    public string FirstName { get; set; } // Maps to: first_name
    public bool IsActive { get; set; }    // Maps to: is_active
}
```

### Repository Pattern

- Inherit from `Repository<TEntity>` base class
- Use Dapper.Contrib for simple CRUD: `Get`, `GetAll`, `Insert`, `Update`, `Delete`
- For custom queries: `QueryAsync`, `QueryFirstOrDefaultAsync`, `ExecuteAsync`
- **SQL naming**: Always use `aesir_` prefix with snake_case

```csharp
// CORRECT
var sql = "SELECT * FROM aesir_product WHERE name = @Name AND is_deleted = false";

// WRONG - missing prefix
var sql = "SELECT * FROM product WHERE name = @Name";
```

### Soft Deletes

- Properties: `IsDeleted`, `DeletedAt`, `DeletedBy` (PascalCase in C#)
- Columns: `is_deleted`, `deleted_at`, `deleted_by` (snake_case in DB)
- Override `RemoveAsync` to UPDATE instead of DELETE
- Filter: `WHERE is_deleted = false`

### Audit Trails

- Properties: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- Set audit fields in service layer, not repository

---

## API Documentation

### Swagger

- **Endpoint**: `/swagger` (Development only)
- **URL**: https://aesir.localhost/swagger
- **OpenAPI Version**: v1

---

## Code Style Guidelines

- **Naming**: PascalCase for classes/interfaces/methods, camelCase for locals
- **Interfaces**: Prefix with "I" (e.g., `IChatService`, `IDbConnectionFactory`)
- **Nullable**: Enable `<Nullable>enable</Nullable>`
- **Async**: Use async/await with `Task<T>`, suffix methods with "Async"
- **ConfigureAwait**: Use `.ConfigureAwait(false)` in libraries

### Dependency Injection

| Service Type | Lifetime |
|--------------|----------|
| `IDbConnectionFactory` | Singleton |
| Repositories | Scoped |
| `IUnitOfWork` | Scoped |

### Blazor Components

- Use code-behind pattern for complex logic
- See [Blazor WebAssembly Client](#blazor-webassembly-client) section

---

## Error Handling

### API Layer (Controllers)

- Return appropriate HTTP status codes
- Use `IActionResult` helpers: `Ok()`, `BadRequest()`, `NotFound()`
- Log errors at Error level with correlation ID

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetById(Guid id)
{
    var entity = await _service.GetByIdAsync(id);
    if (entity == null)
        return NotFound();
    return Ok(entity);
}
```

### Service Layer

- Catch specific exceptions, not general `Exception`
- Throw custom exceptions for business rule violations
- Let infrastructure exceptions bubble up
- Log warnings for validation failures

```csharp
public async Task<User> CreateUserAsync(CreateUserRequest request)
{
    var existing = await _repository.GetByUsernameAsync(request.Username);
    if (existing != null)
    {
        _logger.LogWarning("Username already exists: {Username}", request.Username);
        throw new DuplicateUsernameException(request.Username);
    }
    // ...
}
```

### Client Layer (Blazor)

- Use try/catch around API calls
- Display user-friendly messages via MudBlazor Snackbar
- Log errors for debugging

```csharp
try
{
    await ApiClient.PostAsync<Response>("/api/users", request);
    Snackbar.Add("User created successfully", Severity.Success);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to create user");
    Snackbar.Add("Failed to create user. Please try again.", Severity.Error);
}
```

---

## Logging Guidelines

### Framework

- **NLog 6.x** for all logging
- **Server**: `NLog.Web.AspNetCore`
- **Other projects**: `NLog.Extensions.Logging`
- **Config**: `nlog.config` at application root

### Log Levels

| Level | Use Case |
|-------|----------|
| Debug | Diagnostic info (data access operations) |
| Info | Business events (user created, login success) |
| Warning | Recoverable issues (validation failures) |
| Error | Exceptions requiring attention |

### Structured Logging

Always use named parameters:

```csharp
// CORRECT
_logger.LogInformation("Creating user: {Username}", request.Username);

// WRONG - string interpolation
_logger.LogInformation($"Creating user: {request.Username}");
```

### Constructor Injection

```csharp
public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
    }
}
```

### Sensitive Data

**DO NOT LOG**: Passwords, API keys, credit card numbers, SSNs

**CAN LOG**: Usernames, email addresses, User IDs (Guids)

### Correlation IDs

- Middleware: `CorrelationIdMiddleware` in `Aesir.Infrastructure.Middleware`
- Header: `X-Correlation-Id`
- Register early: `app.UseCorrelationId()`

---

## Docker & Kubernetes

### Docker Configuration

- **Dockerfile**: `Server/Aesir.Api.Server/Dockerfile`
- **Base Images**: `mcr.microsoft.com/dotnet/sdk:10.0` (build), `aspnet:10.0` (runtime)
- **User**: Run as `appuser` (UID 1000)
- **Health Check**: `/health` endpoint

### Docker Compose

- **Development**: `docker-compose-api-dev.yml`
- **Services**: PostgreSQL, Traefik (https://aesir.localhost), Qdrant
- **Environment**: Configure via `.env` file (never commit!)

```bash
# Build and start
./run-server.sh

# Or manually
docker compose -f docker-compose-api-dev.yml up -d
```

### Health Check

- **Path**: `/health`
- **200 OK**: Healthy
- **503**: Unhealthy

### Kubernetes (K3s)

- **Namespace**: `aesir`
- **ConfigMap**: Non-sensitive config
- **Secret**: Passwords, connection strings
- **PostgreSQL**: StatefulSet with PersistentVolume (10Gi)
- **API**: Deployment with 2 replicas

**Resource Limits**:
- API: 1 CPU / 1Gi memory (limit), 0.25 CPU / 256Mi (request)
- PostgreSQL: 2 CPU / 2Gi memory (limit), 0.5 CPU / 512Mi (request)

### Security

- Run containers as non-root user
- Use read-only root filesystem where possible
- Use secrets for sensitive data
- Regularly update base images

---

## Blazor WebAssembly Client

### Project Structure

```
Client/Aesir.Client.Web/
├── Aesir.Client.Web.App/           # Main WASM application
├── Aesir.Client.Web.Infrastructure/ # Shared services, API client
├── Modules/                         # Feature modules
│   ├── Aesir.Client.Web.Modules.Chat/
│   ├── Aesir.Client.Web.Modules.Settings/
│   └── ...
└── src-tauri/                       # Tauri desktop config
```

### UI Framework

- **MudBlazor 8.x** (Material Design)
- Required providers in App.razor: `MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider`

### Module System

**Modules should NOT depend on each other.** Dependencies allowed:
- `Aesir.Client.Web.Infrastructure`
- `Aesir.Common`
- External packages (MudBlazor)

**Cross-module communication**: Use event notifier pattern (see `IConfigurationChangedNotifier`)

### Creating a New Module

1. Create project at `Modules/Aesir.Client.Web.Modules.{Name}/`
2. Add project reference in `Aesir.Client.Web.App.csproj`
3. Implement `ClientModuleBase`
4. Register in `Program.cs`: `builder.Services.AddModule<MyModule>()`
5. Add namespace to `_Imports.razor`
6. Add assembly to router in `App.razor`

### API Client

```csharp
@inject IApiClient ApiClient

@code {
    private List<Agent>? _agents;

    protected override async Task OnInitializedAsync()
    {
        _agents = await ApiClient.GetAsync<List<Agent>>("/api/configuration/agents");
    }
}
```

### Tauri Desktop

- Config: `src-tauri/tauri.conf.json`
- Dev: `./run-tauri.sh` (requires Blazor running)
- Build: `cargo tauri build`

---

## Testing Guidelines

### Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test Server/Modules/Aesir.Modules.Chat.Tests/

# With timeout
dotnet test --timeout 180000
```

### Test Patterns

- Use xUnit for all tests
- Mock dependencies with Moq or NSubstitute
- Name tests: `{Method}_{Scenario}_{ExpectedResult}`

### Browser Testing

- **URL**: http://localhost:5173/
- Use `./run-blazor.sh` to start dev server
- Always stop existing processes before restarting

### After Code Changes

- Run unit tests after each change
- Update or create tests as needed
- Include appropriate timeout for test runs

---

## Development Environment

### API Server

**CRITICAL**: Always run API from Docker, never locally.

Connection strings use Docker service names (`pgdb`) which only resolve within Docker network.

### URLs

| Service | URL |
|---------|-----|
| API | https://aesir.localhost |
| Blazor Dev | http://localhost:5173 |
| Swagger | https://aesir.localhost/swagger |
| Traefik Dashboard | http://localhost:8080 |
| PostgreSQL | localhost:5432 |

---

## Planning & Workflow

### Work Plans

- Create detailed plan and wait for approval before implementing
- Add work plans to Solution Items in `Aesir.sln`
- Naming: `WORK_PLAN_RELEASE_{N}.md`

### Workflow Rules

**Before Implementation:**
- Create detailed plan
- Wait for user approval

**After Implementation:**
- Run unit tests after each code change
- Update or create tests if needed

**Testing:**
- Always stop existing `dotnet` processes before restarting Blazor (use `./run-blazor.sh` which handles this)
- Do NOT use `dotnet watch` - it is unstable; use `dotnet run` instead
- Include appropriate timeout for test runs

### Branding

- Application name: "Aesir" or "AESIR" (stylized)
