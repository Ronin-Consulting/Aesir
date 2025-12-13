# Claude Code Guidelines

This document contains guidelines and instructions for Claude Code when working in this project.

## Table of Contents

1. [Project Configuration](#project-configuration)
2. [Code Generation](#code-generation)
3. [Data Access Guidelines](#data-access-guidelines)
4. [API Documentation (Swagger)](#api-documentation-swagger)
5. [Code Style Guidelines](#code-style-guidelines)
6. [Logging Guidelines](#logging-guidelines)
7. [Docker & Kubernetes Guidelines](#docker--kubernetes-guidelines)
8. [Blazor WebAssembly Client Guidelines](#blazor-webassembly-client-guidelines)
9. [Development Environment](#development-environment)
10. [MCP Tools](#mcp-tools)
11. [Testing Guidelines](#testing-guidelines)
12. [Planning & Workflow](#planning--workflow)

---

## Project Configuration

- **Target Framework:** .NET 10.0
- **C# Version:** C# 13
- All projects must target `net10.0` framework

## Code Generation

When generating code that involves external libraries, frameworks, or technologies, you MUST:

1. **Always fetch up-to-date documentation** before writing code
    - Use the `mcp__context7__resolve-library-id` tool to find the library ID
    - Use the `mcp__context7__get-library-docs` tool to retrieve current documentation
    - Never rely solely on training data for library-specific implementations

2. **Required for context7 usage:**
    - Any time you need to implement features using specific libraries (e.g., React, Next.js, TensorFlow, PyTorch, DeepStream, GStreamer)
    - When the user mentions a specific version or wants the latest API patterns
    - Before generating boilerplate or starter code for frameworks
    - When troubleshooting library-specific errors or deprecations

3. **Workflow:**
   ```
   User Request → Identify Libraries → Resolve Library ID → Fetch Docs → Generate Code
   ```

4. **Examples of when to use context7:**
    - "Create a React component with hooks" → Fetch React docs first
    - "Set up a DeepStream pipeline" → Fetch DeepStream docs first
    - "Write a FastAPI endpoint" → Fetch FastAPI docs first
    - "Configure GStreamer elements" → Fetch GStreamer docs first

5. **What to include in context7 queries:**
    - Specify the `topic` parameter to focus on relevant sections (e.g., "hooks", "routing", "pipeline configuration")
    - Adjust `tokens` parameter based on complexity (default: 5000, complex topics: 10000+)

## Enforcement

Do not generate library-specific code without first consulting context7 documentation. If documentation is unavailable for a library, inform the user and proceed with caution, clearly noting the limitations.

## Data Access Guidelines

### **CRITICAL: Database Naming Convention**
**ALL database identifiers MUST use aesir_ prefix with lowercase snake_case:**
- **Table names**: `aesir_user`, `aesir_product`, `aesir_order_item` (ALWAYS use aesir_ prefix, NOT "Users", "Products", "OrderItems")
- **Column names**: `first_name`, `is_active`, `created_at` (NOT "FirstName", "IsActive", "CreatedAt")
- **Index names**: `ix_aesir_user_username`, `ix_aesir_product_name` (use aesir_ prefix in table name portion)
- **C# Properties**: Use PascalCase as per C# conventions (`FirstName`, `IsActive`)
- **Automatic Mapping**: `DapperColumnMapper` automatically converts PascalCase properties to snake_case columns

### ORM and Database
- **ORM**: Use **Dapper 2.1.66** and **Dapper.Contrib 2.0.78** for all data access (not Entity Framework Core)
- **Database**: PostgreSQL 15+ is the target database
- **Column Mapping**: Initialize `DapperColumnMapper.Initialize()` in `Program.cs` for automatic PascalCase to snake_case conversion

### Migrations
- **Tool**: Use **FluentMigrator 7.1.0** for schema management
- **Location**:
    - **Module-specific migrations**: Create in `[ModuleProject]/Migrations/` (e.g., `Aesir.Modules.Users/Migrations/`)
    - **Infrastructure migrations**: Create in `Aesir.Infrastructure/Migrations/` (for shared/system tables only)
- **Auto-Discovery**: Migrations are automatically discovered from all `Aesir.Modules.*` assemblies via `ModuleDiscovery.DiscoverModuleAssemblies()`
    - **How it works**: Scans the application directory for `Aesir.Modules.*.dll` files and loads them
    - **No manual registration needed**: Just add the module DLL to the output directory
    - **Centralized logic**: Uses the same `ModuleDiscovery` class for both migrations and module registration
- **Naming**: Use timestamp format: `[Migration(YYYYMMDDHHMMSS)]`
- **Namespace**: Use module namespace (e.g., `Aesir.Modules.Users.Migrations`)
- **Requirements**: Always implement both `Up()` and `Down()` methods
- **CRITICAL**: Use aesir_ prefix with lowercase snake_case for ALL table, column, and index names in migrations
- **CRITICAL**: Use `.AsGuid()` for primary key columns (NOT `.AsInt32().Identity()`)
- **Package Required**: Add `FluentMigrator` package (version 7.1.0) to module project
- **Example**:
  ```csharp
  using FluentMigrator;

  namespace Aesir.Modules.Products.Migrations;  // Module namespace

  [Migration(20250119000002)]
  public class AddProductsTable : Migration
  {
      public override void Up()
      {
          Create.Table("aesir_product")  // CORRECT: aesir_ prefix
              .WithColumn("id").AsGuid().PrimaryKey()  // CORRECT: Guid primary key
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

### Entity Attributes (Dapper.Contrib)
- **CRITICAL**: Use `[Table("aesir_table_name")]` with **aesir_ prefix and lowercase snake_case** (e.g., `[Table("aesir_user")]`, `[Table("aesir_product")]`)
- **CRITICAL**: ALL entities use **Guid** as the primary key type
- Use `[ExplicitKey]` to mark Guid primary key properties (NOT `[Key]` which is for auto-increment)
- Use `[Write(false)]` to exclude properties from inserts/updates
- Use `[Computed]` for computed/calculated columns
- **Example**:
  ```csharp
  [Table("aesir_user")]  // CORRECT: aesir_ prefix with snake_case
  public class User : IEntity
  {
      [ExplicitKey]  // CORRECT: Use ExplicitKey for Guid primary keys
      public Guid Id { get; set; }  // Maps to column: id (UUID in PostgreSQL)
      public string FirstName { get; set; }  // Maps to column: first_name
      public bool IsActive { get; set; }  // Maps to column: is_active
  }
  ```

### Repository Pattern
- Inherit from `Repository<TEntity>` base class
- Base repository uses Dapper.Contrib for simple CRUD: `Get`, `GetAll`, `Insert`, `Update`, `Delete`
- For custom queries, use Dapper methods: `QueryAsync`, `QueryFirstOrDefaultAsync`, `ExecuteAsync`, `ExecuteScalarAsync`
- Write raw SQL with parameterized queries using anonymous objects
- **CRITICAL**: Use aesir_ prefix with lowercase snake_case for table and column names in SQL
- **Guid Primary Keys**: The base `AddAsync` method automatically generates a new Guid if `entity.Id == Guid.Empty`
- **Example**:
  ```csharp
  var sql = "SELECT * FROM aesir_product WHERE name = @Name AND is_deleted = false";
  // CORRECT: aesir_ prefix with snake_case
  // NOT: "SELECT * FROM \"Products\" WHERE \"Name\" = @Name"
  // NOT: "SELECT * FROM product WHERE name = @Name"  (missing aesir_ prefix!)
  ```

### Soft Deletes
- Add `IsDeleted`, `DeletedAt`, `DeletedBy` properties to entities (PascalCase in C#)
- Database columns: `is_deleted`, `deleted_at`, `deleted_by` (snake_case)
- Override `RemoveAsync` to perform UPDATE instead of DELETE
- Filter soft-deleted records: `WHERE is_deleted = false` (NOT `WHERE "IsDeleted" = false`)

### Audit Trails
- Add `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` properties (PascalCase in C#)
- Database columns: `created_at`, `created_by`, `updated_at`, `updated_by` (snake_case)
- Set audit fields in service layer, not repository

## API Documentation (Swagger)

### Swagger Configuration
- **Endpoint**: `/swagger` (Development environment only)
- **OpenAPI Version**: v1
- **Authentication**: JWT Bearer token support configured
- **Security Scheme**: HTTP Bearer authentication with JWT format

### JWT Authentication in Swagger
- **How to use**:
    1. Call `POST /api/users/login` to get a JWT token
    2. Click the "Authorize" button (lock icon) in Swagger UI
    3. Enter: `Bearer {your-token}` (include the word "Bearer" followed by a space)
    4. Click "Authorize" and then "Close"
    5. All subsequent requests will include the JWT token in the Authorization header

- **Configuration**:
  ```csharp
  builder.Services.AddSwaggerGen(options =>
  {
      options.SwaggerDoc("v1", new OpenApiInfo { Title = "AESIR API", Version = "v1" });

      // JWT Bearer authentication
      options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
      {
          Type = SecuritySchemeType.Http,
          Scheme = "bearer",
          BearerFormat = "JWT",
          Description = "JWT Authorization header using the Bearer scheme."
      });

      options.AddSecurityRequirement(new OpenApiSecurityRequirement { /* ... */ });
  });
  ```

### Testing Protected Endpoints
1. Create a user: `POST /api/users` (no auth required)
2. Login: `POST /api/users/login` → Copy the token from response
3. Authorize in Swagger with the token
4. Test protected endpoints (marked with lock icon)

## Code Style Guidelines
- **Naming**: PascalCase for classes/interfaces/public methods, camelCase for local variables
- **Interfaces**: Prefix with "I" (e.g., IChatService, IModelsService, IDbConnectionFactory)
- **Organization**: Group implementations in dedicated folders (e.g., Standard, PlatformX86, PlatformX64)
- **Nullable**: Enable nullable reference types (`<Nullable>enable</Nullable>`)
- **Async**: Use async/await consistently with Task<T> return types.
    - Use `ConfigureAwait(false)` where appropriate.
    - Async methods should be named with the "Async" suffix.
- **Dependency Injection**: Use constructor injection and register services with proper lifetimes
    - `IDbConnectionFactory` should be Singleton
    - Repositories should be Scoped
    - `IUnitOfWork` should be Scoped
- **Component Pattern**: For Blazor components, use code-behind pattern for complex logic. See Blazor WebAssembly Client Guidelines section for details
- **Error Handling**: Use try/catch with specific exception types, avoid general Exception catches

## Logging Guidelines

### Logging Framework
- **Framework**: Use **NLog 5.3.x** for all logging across the solution
- **Server Projects**: Use `NLog.Web.AspNetCore` (version 5.3.14) for API projects
- **Other Projects**: Use `NLog.Extensions.Logging` (version 5.3.14) for all other projects
- **Configuration**: NLog configuration is stored in `nlog.config` files at the application root
- **Format**: Simple text format with correlation IDs for request tracking

### Configuration Files
- **API**: `Server/Aesir.Api.Server/nlog.config`
- Configuration includes:
    - File targets for all logs and errors
    - Console output for development
    - Automatic log rotation (daily)
    - Correlation ID support in layout
    - Suppression of verbose Microsoft framework logs

### Correlation IDs
- **Middleware**: `CorrelationIdMiddleware` in `Aesir.Infrastructure.Middleware`
- **Header**: `X-Correlation-Id` (automatically added to responses)
- **Purpose**: Track requests across services and logs
- **Usage**: Automatically included in log entries via `${event-properties:item=CorrelationId}`
- **Middleware Registration**: Add early in pipeline with `app.UseCorrelationId()`

### Logging Best Practices

#### Log Levels
- **Debug**: Detailed diagnostic information (e.g., "Getting user by username: {Username}")
- **Info**: Important business events (e.g., "Successfully created user with Id: {UserId}")
- **Warning**: Unexpected but recoverable events (e.g., "Failed login attempt - invalid username")
- **Error**: Errors and exceptions that need attention

#### Structured Logging
Always use structured logging with named parameters:
```csharp
// CORRECT: Structured logging
_logger.LogInformation("Creating new user with username: {Username}", request.Username);
_logger.LogDebug("Getting entity {EntityType} by Id {Id}", typeof(TEntity).Name, id);

// INCORRECT: String interpolation
_logger.LogInformation($"Creating new user with username: {request.Username}");
```

#### What to Log

**Infrastructure Layer (Repository)**:
- Debug: Data access operations (Get, GetAll)
- Info: Data modifications (Add, Update, Remove)
- Log entity type and ID for all operations
- Example:
  ```csharp
  Logger.LogDebug("Getting entity {EntityType} by Id {Id}", typeof(TEntity).Name, id);
  Logger.LogInformation("Adding entity {EntityType} with Id {Id}", typeof(TEntity).Name, entity.Id);
  ```

**Service Layer**:
- Info: Business operations (Create, Update, Delete, Login)
- Warning: Business validation failures (duplicate username, invalid credentials)
- Include relevant business context (username, email, user ID)
- Example:
  ```csharp
  _logger.LogInformation("Creating new user with username: {Username}", request.Username);
  _logger.LogWarning("Failed to create user - username already exists: {Username}", request.Username);
  ```

**Controller Layer**:
- Info: API endpoint calls with important operations (POST, PUT, DELETE)
- Debug: Read operations (GET)
- Warning: Failed requests (validation errors, not found)
- Include HTTP method and route context
- Example:
  ```csharp
  _logger.LogInformation("POST /api/users - Creating user with username: {Username}", request.Username);
  _logger.LogDebug("GET /api/users/{Id} - Retrieving user", id);
  ```

#### Constructor Injection
All classes requiring logging should inject `ILogger<T>`:
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

**Repository Base Class**: The base `Repository<TEntity>` already accepts `ILogger<Repository<TEntity>>`, so derived repositories must pass it:
```csharp
public class UserRepository : Repository<User>
{
    public UserRepository(IDbConnectionFactory connectionFactory, ILogger<Repository<User>> logger)
        : base(connectionFactory, logger)
    {
    }
}
```

#### Sensitive Data
**DO NOT LOG**:
- Passwords or password hashes
- API keys or secrets
- Full credit card numbers
- Personal identification numbers (SSN, etc.)

**CAN LOG**:
- Usernames (for security audit trail)
- Email addresses (for operational debugging)
- User IDs (Guids)
- Request/response metadata

### Initialization

**API (Program.cs)**:
```csharp
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("Initializing Aesir API");

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // ... rest of setup
}
catch (Exception exception)
{
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}
```

### Log Files
- **Location**: `logs/` directory in application root
- **Rotation**: Daily (automatically archived)
- **Retention**:
    - All logs: 30 days
    - Error logs: 90 days
- **Git**: Logs are excluded via `logs/.gitignore`

## Docker & Kubernetes Guidelines

### Docker Configuration
- **Dockerfile Location**: `Server/Aesir.Api.Server/Dockerfile`
- **Base Images**:
    - Build: `mcr.microsoft.com/dotnet/sdk:10.0`
    - Runtime: `mcr.microsoft.com/dotnet/aspnet:10.0`
- **Multi-stage Build**: Required for optimal image size (~200MB runtime)
- **Non-root User**: Always run as `appuser` (UID 1000)
- **Exposed Ports**: 8080 (HTTP), 8081 (HTTPS)
- **Health Check**: Built-in via `/health` endpoint

### Docker Compose
- **Development**: `docker-compose-api-dev.yml`
    - Includes PostgreSQL container
    - Traefik reverse proxy (https://aesir.localhost)
    - Volume mounts for logs
    - Development environment variables
- **Deprecated**: `docker-compose-aesir-all.yml` (do not use, will be removed)
- **Environment**: Configure via `.env` file (never commit!)

### Health Check Endpoint
- **Path**: `/health`
- **Response**:
    - `200 OK` - API healthy, database connected
    - `503 Service Unavailable` - API unhealthy
- **Dependencies Checked**: PostgreSQL connectivity
- **Package**: `AspNetCore.HealthChecks.NpgSql` version 10.0.0

### Kubernetes (K3s) Configuration
- **Namespace**: `aesir`
- **ConfigMap**: Non-sensitive configuration (environment, JWT issuer/audience)
- **Secret**: Sensitive data (passwords, JWT secret key, connection string)
- **PostgreSQL**: StatefulSet with PersistentVolume (10Gi)
- **API**: Deployment with 2 replicas (horizontal scaling)
- **Service**: LoadBalancer (or NodePort for K3s)
- **Resource Limits**:
    - API: 1 CPU / 1Gi memory (limit), 0.25 CPU / 256Mi memory (request)
    - PostgreSQL: 2 CPU / 2Gi memory (limit), 0.5 CPU / 512Mi memory (request)

### Best Practices

**Image Building**:
```bash
# Build from solution root
docker build -t aesir-api:latest -f Server/Aesir.Api.Server/Dockerfile .

# Tag for registry
docker tag aesir-api:latest your-registry/aesir-api:v1.0.0
```

**Secret Management**:
- **Docker Compose**: Use `.env` file (add to `.gitignore`)
- **Kubernetes**: Use `Secret` resource with base64-encoded values
- **Never commit**: `.env`, `k8s/secret.yaml`, or any files containing secrets

**Database Migrations**:
- **Auto-run on startup**: Current behavior (migrations run in `Program.cs`)
- **Docker**: Migrations run when container starts
- **Kubernetes**: Migrations run when API pod starts

**Volume Persistence**:
- **Docker Compose**: Named volume `postgres-data`
- **Kubernetes**: PersistentVolumeClaim `postgres-storage`
- **Backup**: Regularly backup database volumes

**Deployment Workflow**:
1. Build Docker image
2. Test locally with docker-compose
3. Tag and push to registry (for K8s)
4. Apply Kubernetes manifests
5. Verify health checks pass
6. Monitor logs and metrics

### Security Guidelines
- Run containers as non-root user
- Use read-only root filesystem where possible
- Drop all capabilities except required ones
- Implement network policies in Kubernetes
- Use secrets for sensitive data
- Enable audit logging
- Regularly update base images
- Scan images for vulnerabilities

### Documentation
- **Docker Compose**: `docker-compose-api-dev.yml` (development)
- **Environment Template**: `.env.example` (if available)

## Blazor WebAssembly Client Guidelines

### Project Structure
The Blazor WebAssembly client is located at `Client/Aesir.Client.Web/`:
```
Client/Aesir.Client.Web/
├── Aesir.Client.Web.App/              # Main Blazor WASM application
│   ├── Layout/                        # Layout components
│   ├── Pages/                         # App-level pages (Home)
│   ├── wwwroot/                       # Static assets, appsettings.json
│   ├── App.razor                      # Root component with providers
│   ├── Program.cs                     # Service registration
│   └── _Imports.razor                 # Global usings
├── Aesir.Client.Web.Infrastructure/   # Shared client infrastructure
│   ├── Http/                          # API client (IApiClient, ApiClient)
│   ├── Modules/                       # Module system interfaces
│   └── Services/                      # Shared services
├── Modules/                           # Feature modules
│   └── Aesir.Client.Web.Modules.Chat/ # Chat module
│       ├── Pages/                     # Module pages
│       ├── Components/                # Module components
│       ├── Services/                  # Module services
│       └── ChatModule.cs              # Module registration
└── src-tauri/                         # Tauri desktop configuration
```

### UI Framework
- **Component Library**: MudBlazor 8.x (Material Design)
- **Providers Required** (in App.razor):
  ```razor
  <MudThemeProvider />
  <MudPopoverProvider />
  <MudDialogProvider />
  <MudSnackbarProvider />
  ```
- **Service Registration**:
  ```csharp
  builder.Services.AddMudServices();
  ```

### Module System

#### Architectural Decision
Blazor client modules use **explicit project references** for component visibility (compile-time) combined with **runtime discovery** for services and routes.

**Why explicit references for components:**
- Razor components are compiled at build time
- IntelliSense, Go to Definition require project references
- Compile-time type checking for component parameters

**What uses explicit project references (compile-time):**
- Component visibility (`<ChatMessage />` tags in Razor)
- `@using` directives in `_Imports.razor`
- Strongly-typed component parameters

**What uses auto-discovery (runtime):**
- Service registration via `IClientModule.RegisterServices()`
- Route scanning (Blazor scans `@page` directives from referenced assemblies)
- Navigation menu items via `INavigationRegistry`

#### Creating a New Module

1. **Create project** at `Modules/Aesir.Client.Web.Modules.{Name}/`
2. **Add project reference** in `Aesir.Client.Web.App.csproj`:
   ```xml
   <ProjectReference Include="..\Modules\Aesir.Client.Web.Modules.{Name}\..." />
   ```
3. **Implement `IClientModule`**:
   ```csharp
   public class MyModule : ClientModuleBase
   {
       public override string Name => "MyModule";
       public override string Version => "1.0.0";
       public override string Description => "Description here";

       public override void RegisterServices(IServiceCollection services)
       {
           services.AddScoped<IMyService, MyService>();
       }

       public override void RegisterNavigation(INavigationRegistry registry)
       {
           registry.Register(new NavigationItem
           {
               Title = "My Page",
               Href = "/mypage",
               Icon = "Dashboard",
               Priority = 50
           });
       }
   }
   ```
4. **Register module** in `Program.cs`:
   ```csharp
   builder.Services.AddModule<MyModule>();
   ```
5. **Add namespace** to `_Imports.razor`:
   ```razor
   @using Aesir.Client.Web.Modules.{Name}
   ```
6. **Add assembly** to router in `App.razor`:
   ```csharp
   private static readonly Assembly[] AdditionalAssemblies =
   [
       typeof(ChatModule).Assembly,
       typeof(MyModule).Assembly  // Add new module
   ];
   ```

### API Client

#### Interface
```csharp
public interface IApiClient
{
    Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default);
    Task<T?> PostAsync<T>(string endpoint, object data, CancellationToken ct = default);
    Task<T?> PutAsync<T>(string endpoint, object data, CancellationToken ct = default);
    Task<bool> DeleteAsync(string endpoint, CancellationToken ct = default);
    IAsyncEnumerable<T> StreamAsync<T>(string endpoint, CancellationToken ct = default);
    IAsyncEnumerable<T> StreamPostAsync<T>(string endpoint, object data, CancellationToken ct = default);
}
```

#### Configuration
```csharp
// Program.cs
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
builder.Services.AddAesirApiClient(apiBaseUrl);
```

#### Usage in Components
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

### Tauri Desktop Integration

#### Configuration
The Tauri configuration is in `src-tauri/tauri.conf.json`:
```json
{
  "build": {
    "frontendDist": "../Aesir.Client.Web.App/bin/Release/net10.0/publish/wwwroot",
    "devUrl": "http://localhost:5173",
    "beforeBuildCommand": "dotnet publish Aesir.Client.Web.App -c Release"
  }
}
```

#### Development Workflow
```bash
# Browser development (primary - hot reload)
cd Client/Aesir.Client.Web/Aesir.Client.Web.App
dotnet watch run --urls "http://localhost:5173"

# Desktop development (connects to dev server)
cd Client/Aesir.Client.Web
cargo tauri dev

# Production build (creates native app)
cargo tauri build
```

### Best Practices

#### Component Patterns
- Use `@inject` for dependency injection in components
- Prefer `EventCallback` over direct method calls for parent-child communication
- Use cascading parameters for deeply nested state

#### State Management
- Simple: Component state + cascading parameters
- Medium: Custom services + events (recommended for AESIR)
- Complex: Fluxor (Redux-like) - overkill for most cases

#### Naming Conventions
- **Pages**: `{Feature}Page.razor` (e.g., `ChatPage.razor`)
- **Components**: Descriptive names (e.g., `ChatMessage.razor`, `AgentSelector.razor`)
- **Services**: `I{Name}Service` / `{Name}Service`
- **Modules**: `{Feature}Module.cs`

#### Code-Behind Pattern
For complex components, use code-behind:
```csharp
// ChatPage.razor.cs
public partial class ChatPage
{
    [Inject] private IApiClient ApiClient { get; set; } = null!;

    private async Task LoadDataAsync() { ... }
}
```

## Development Environment

### API Server
- **CRITICAL**: The API server must ALWAYS be run from Docker container, never locally
- Connection strings use Docker service names (e.g., `pgdb`) which only resolve within the Docker network
- Do NOT attempt to run `dotnet run` on the server project directly

### API URL
- The API is always accessible at `https://aesir.localhost` via reverse proxy (Traefik)
- All client applications should use this URL for API communication

### Docker Compose
- **Use**: `docker-compose-api-dev.yml` for development
- **Do NOT use**: `docker-compose-aesir-all.yml` (deprecated, will be removed)

## MCP Tools

### Context7 (Documentation)
- **Purpose**: Fetch up-to-date documentation for external libraries and frameworks
- **Required**: Must be installed and available
- **Usage**: Always use Context7 before generating library-specific code (see Code Generation section)
- If Context7 is not available, ask to have it installed

### Playwright (Browser Testing)
- **Purpose**: Automated browser testing for the web application
- **Required**: Must be installed and available
- **Usage**: Always use Playwright MCP tool when testing the web app in the browser
- If Playwright is not available, ask to have it installed

## Testing Guidelines

### Manual Testing
- The user will perform most testing manually unless explicitly asked to run tests
- Do not run test suites unless specifically requested

### Automated Browser Testing
- Use the Playwright MCP tool for browser-based testing
- Navigate to `https://aesir.localhost` for testing the web client
- Take screenshots to verify UI state when appropriate

## Planning & Workflow

### Work Plans
- Always create a detailed plan and wait for approval before implementing any code changes
- Add newly created work plans to the Solution Items in the solution file
- Work plan files should follow the naming convention: `WORK_PLAN_RELEASE_{N}.md`

### Branding
- Use the stylized Æ ligature (ÆSIR) in prominent areas where appropriate
- The application name is "Aesir" or "ÆSIR" for stylized display
- When testing the web app use http://localhost:5173/ as the url.
- When running tests, always include a timeout that makes sense based on the test.
- when developing fixs or features always make sure tests are ran afterwards and if needed update or create unit tests.
- always run unit tests after each code change made.