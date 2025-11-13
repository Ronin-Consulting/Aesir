# Claude Code Guidelines

This document contains guidelines and instructions for Claude Code when working in this project.

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
- **MVVM Pattern**: When developing Desktop applications, when using ViewModels they should derive from ViewModelBase, use CommunityToolkit.Mvvm
- **Error Handling**: Use try/catch with specific exception types, avoid general Exception catches

## Logging Guidelines

### Logging Framework
- **Framework**: Use **NLog 5.3.x** for all logging across the solution
- **Server Projects**: Use `NLog.Web.AspNetCore` (version 5.3.14) for API projects
- **Other Projects**: Use `NLog.Extensions.Logging` (version 5.3.14) for all other projects
- **Configuration**: NLog configuration is stored in `nlog.config` files at the application root
- **Format**: Simple text format with correlation IDs for request tracking

### Configuration Files
- **API**: `src/Server/Aesir.Api/nlog.config`
- **Desktop**: `src/Desktop/Aesir.Desktop.Agent/nlog.config`
- Both configurations include:
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

**Desktop (App.axaml.cs)**:
```csharp
using NLog;
using NLog.Extensions.Logging;

private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    loggingBuilder.AddNLog();
});
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
- **Dockerfile Location**: `src/Server/Aesir.Api/Dockerfile`
- **Base Images**:
    - Build: `mcr.microsoft.com/dotnet/sdk:9.0`
    - Runtime: `mcr.microsoft.com/dotnet/aspnet:9.0`
- **Multi-stage Build**: Required for optimal image size (~200MB runtime)
- **Non-root User**: Always run as `appuser` (UID 1000)
- **Exposed Ports**: 8080 (HTTP), 8081 (HTTPS)
- **Health Check**: Built-in via `/health` endpoint

### Docker Compose
- **Development**: `docker-compose.yml`
    - Includes PostgreSQL container
    - Volume mounts for logs
    - Port mappings: 5000:8080, 5001:8081
    - Development environment variables
- **Production**: `docker-compose.prod.yml`
    - Resource limits configured
    - Optimized health checks
    - Logging driver configuration
    - Security hardening
- **Environment**: Configure via `.env` file (never commit!)

### Health Check Endpoint
- **Path**: `/health`
- **Response**:
    - `200 OK` - API healthy, database connected
    - `503 Service Unavailable` - API unhealthy
- **Dependencies Checked**: PostgreSQL connectivity
- **Package**: `AspNetCore.HealthChecks.NpgSql` version 9.0.0

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
docker build -t aesir-api:latest -f src/Server/Aesir.Api/Dockerfile .

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
- **Docker Guide**: `DOCKER.md` - Complete deployment instructions
- **Docker Compose**: `docker-compose.yml` (dev), `docker-compose.prod.yml` (prod)
- **Kubernetes Manifests**: `k8s/` directory
- **Environment Template**: `.env.example`

## Plan Creation
- Always create a detailed plan and wait for approval before implementing any code changes.