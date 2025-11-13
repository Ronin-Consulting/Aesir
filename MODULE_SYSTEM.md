# Module System Documentation

## Overview

The AESIR module system provides a convention-based approach to creating self-contained, discoverable feature modules. Each module is a separate assembly that can be developed, tested, and deployed independently. The system uses **Dapper** for data access, **FluentMigrator** for database migrations, and follows strict naming conventions with the `aesir_` prefix for all database objects.

## Core Technologies

- **Data Access**: Dapper 2.1.66 and Dapper.Contrib 2.0.78
- **Migrations**: FluentMigrator 7.1.0
- **Database**: PostgreSQL 15+
- **Primary Keys**: All entities use `Guid` as the primary key type
- **Naming Convention**: `aesir_` prefix with snake_case for all database objects
- **Error Handling**: Exception-based approach with proper try/catch blocks

## How Module Discovery Works

### 1. Automatic Build System

**IMPORTANT**: The AESIR build system automatically discovers, builds, and deploys all modules. When you create a new module following the naming convention, **NO changes to Api.Server.csproj or Dockerfile are required**.

**Build Process:**

```
Developer runs: dotnet build Aesir.Api.Server.csproj
        ↓
Api.Server BuildAllModules target executes (BeforeTargets="Build")
        ↓
Discovers all projects matching: ../Aesir.Modules.*/*.csproj
        ↓
Builds each discovered module in parallel
        ↓
Each module's CopyModuleToApiServer target executes (AfterTargets="Build")
        ↓
Module DLLs copied to: Aesir.Api.Server/bin/{Configuration}/net10.0/
        ↓
Api.Server build completes
        ↓
Runtime ModuleDiscovery scans for Aesir.Modules.*.dll files
        ↓
All modules loaded and initialized
```

**Key Configuration in Api.Server.csproj:**
```xml
<!-- Automatically discover and build all module projects -->
<Target Name="BuildAllModules" BeforeTargets="Build">
    <Message Text="[Api.Server] Discovering module projects..." Importance="high" />

    <!-- Find all module projects matching the pattern -->
    <ItemGroup>
        <ModuleProjects Include="Modules/Aesir.Modules.*/*.csproj" />
    </ItemGroup>

    <!-- Build each discovered module -->
    <MSBuild Projects="@(ModuleProjects)" Properties="Configuration=$(Configuration)" />

    <Message Text="[Api.Server] Successfully built @(ModuleProjects->Count()) module(s)" Importance="high" />
</Target>
```

**What This Means for Developers:**
- ✅ Create a new module with naming pattern `Aesir.Modules.{YourFeature}`
- ✅ Add the auto-copy target (see Project File Configuration below)
- ✅ Build Api.Server → Your module automatically builds and deploys
- ✅ **No manual registration needed** in Api.Server.csproj or Dockerfile
- ✅ Works in local development AND Docker builds

### 2. Convention-Based Runtime Discovery

The `ModuleDiscovery` class scans for assemblies matching the pattern:
```
Aesir.Modules.*
```

**Runtime Discovery Process:**
1. Scans the application directory for `Aesir.Modules.*.dll` files
2. Loads assemblies dynamically
3. Looks for classes implementing `IModule` interface
4. Instantiates and registers each discovered module
5. Discovers FluentMigrator migrations from module assemblies

### 2. Module Registration

During application startup in `Program.cs`:

```csharp
// Initialize Dapper column mapping for snake_case conversion
DapperColumnMapper.Initialize();

// Configure database context
var npgsqlDataSource = CreateNpgsqlDataSource(configuration);
services.AddSingleton(npgsqlDataSource);

var dbContext = new PgDbContext(npgsqlDataSource);
services.AddSingleton<IDbContext>(dbContext);

// Discover and register all modules
await builder.Services.ConfigureAesirInfrastructureAsync(builder.Configuration);
```

### 3. Migration Discovery and Execution

```csharp
// Auto-discover migrations from all module assemblies
builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(runner =>
    {
        runner.AddPostgres()
              .WithGlobalConnectionString("DefaultConnection");

        // Automatically scan all module assemblies for migrations
        var moduleAssemblies = ModuleDiscovery.DiscoverModuleAssemblies();
        foreach (var assembly in moduleAssemblies)
        {
            runner.ScanIn(assembly).For.Migrations();
        }
    })
    .AddLogging(lb => lb.AddFluentMigratorConsole());

// Execute migrations on startup
using var scope = app.Services.CreateScope();
var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
runner.MigrateUp();
```

## Module Lifecycle

```
Application Starts
    ↓
DapperColumnMapper.Initialize() → Configure PascalCase to snake_case mapping
    ↓
ModuleDiscovery.DiscoverModules() → Find all Aesir.Modules.* assemblies
    ↓
For each discovered module:
    → Module.RegisterServicesAsync(IServiceCollection)
    ↓
FluentMigrator discovers migrations from module assemblies
    ↓
Application Pipeline Built
    ↓
Database migrations run automatically
    ↓
For each registered module:
    → Module.Initialize(IApplicationBuilder app)
    ↓
Application Running
```

## Creating a Module - Complete Example

Let's walk through creating a complete Users module that demonstrates all AESIR patterns.

### Module Structure

```
Aesir.Modules.Users/
├── UsersModule.cs                    # Module registration class
├── Controllers/                      # API endpoints
│   └── UsersController.cs
├── Services/                         # Business logic
│   ├── IUserService.cs
│   └── UserService.cs
├── Data/                            # OPTIONAL - Only if module has database needs
│   ├── Models/                      # Domain models (database entities)
│   │   └── User.cs
│   └── Repositories/                # Data access (optional within Data/)
│       ├── IUserRepository.cs
│       └── UserRepository.cs
├── Migrations/                       # OPTIONAL - Only if module has database tables
│   └── Migration20251025120001_AddUsersTable.cs
├── Models/                          # DTOs, requests, and responses
│   ├── UserDto.cs
│   ├── CreateUserRequest.cs
│   ├── UpdateUserRequest.cs
│   └── LoginRequest.cs
└── Aesir.Modules.Users.csproj
```

**When to Include Data/ Directory:**

The `Data/` directory is **completely optional** and should only be created if your module has **local private data needs**:

✅ **Include Data/ if your module:**
- Defines its own database tables/entities
- Performs direct data access operations (CRUD)
- Manages persistent state in the database
- Examples: Chat (stores messages), Storage (stores files), Logging (stores logs)

❌ **Skip Data/ if your module:**
- Only calls external APIs or services
- Coordinates other modules without storing data
- Provides stateless business logic
- Examples: Inference providers (Ollama, OpenAI), API coordinators, webhook handlers

**Note:** If you include `Data/`, you'll also need:
- `Migrations/` directory for database schema
- FluentMigrator package reference in `.csproj`
- Repository registrations in `RegisterServicesAsync()`

### 1. Module Class (IModule Implementation)

```csharp
using Aesir.Infrastructure.Modules;
using Aesir.Modules.Users.Data.Repositories;
using Aesir.Modules.Users.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Users;

public class UsersModule : ModuleBase
{
    public override string Name => "Users";
    public override string Version => "1.0.0";
    public override string Description => "User management and authentication module";

    public override Task RegisterServicesAsync(IServiceCollection services)
    {
        // Register repositories (optional - only if using repository pattern)
        services.AddScoped<IUserRepository, UserRepository>();

        // Register services
        services.AddScoped<IUserService, UserService>();

        // Module is ready for use
        Log("Users module services registered");

        return Task.CompletedTask;
    }

    public override void Initialize(IApplicationBuilder app)
    {
        // Optional: Add any startup logic (middleware, endpoints, etc.)
        Log("Users module initialized successfully");
    }
}
```

### 2. Entity with Dapper Attributes

```csharp
using System.ComponentModel.DataAnnotations.Schema;
using Aesir.Infrastructure.Data;
using Dapper.Contrib.Extensions;

namespace Aesir.Modules.Users.Data.Models;

/// <summary>
/// User entity with Dapper.Contrib attributes for table mapping.
/// Properties use PascalCase which automatically maps to snake_case columns.
/// </summary>
[Dapper.Contrib.Extensions.Table("aesir_user")]  // CRITICAL: aesir_ prefix with snake_case
public class User : IEntity
{
    /// <summary>
    /// Primary key - Guid type with ExplicitKey attribute (not auto-increment)
    /// </summary>
    [ExplicitKey]  // Use ExplicitKey for Guid primary keys
    public Guid Id { get; set; }

    // Core properties - PascalCase maps to snake_case columns
    public string Username { get; set; } = string.Empty;        // → username
    public string Email { get; set; } = string.Empty;           // → email
    public string PasswordHash { get; set; } = string.Empty;    // → password_hash
    public string FirstName { get; set; } = string.Empty;       // → first_name
    public string LastName { get; set; } = string.Empty;        // → last_name

    // Soft delete support
    public bool IsDeleted { get; set; }                         // → is_deleted
    public DateTime? DeletedAt { get; set; }                    // → deleted_at
    public string? DeletedBy { get; set; }                      // → deleted_by

    // Audit trail
    public DateTime CreatedAt { get; set; }                     // → created_at
    public string CreatedBy { get; set; } = string.Empty;       // → created_by
    public DateTime? UpdatedAt { get; set; }                    // → updated_at
    public string? UpdatedBy { get; set; }                      // → updated_by

    // Navigation properties (not persisted)
    [Write(false)]  // Exclude from inserts/updates
    public string FullName => $"{FirstName} {LastName}".Trim();
}
```

### 3. Repository with Base Class

```csharp
using Aesir.Infrastructure.Data;
using Aesir.Modules.Users.Data.Models;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Users.Data.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string username, string email, CancellationToken cancellationToken = default);
}

/// <summary>
/// User repository extending Repository base class which provides CRUD operations via Dapper.Contrib
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<Repository<User>> logger)
        : base(connectionFactory, logger)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        // CRITICAL: Use aesir_ prefix and snake_case in SQL queries
        const string sql = @"
            SELECT * FROM aesir_user
            WHERE username = @Username
                AND is_deleted = false";

        using var connection = await ConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);

        var user = await connection
            .QueryFirstOrDefaultAsync<User>(sql, new { Username = username })
            .ConfigureAwait(false);

        if (user != null)
        {
            Logger.LogDebug("Found user with username: {Username}", username);
        }

        return user;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT * FROM aesir_user
            WHERE email = @Email
                AND is_deleted = false";

        using var connection = await ConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);

        return await connection
            .QueryFirstOrDefaultAsync<User>(sql, new { Email = email })
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(string username, string email, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM aesir_user
            WHERE (username = @Username OR email = @Email)
                AND is_deleted = false";

        using var connection = await ConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);

        var count = await connection
            .ExecuteScalarAsync<int>(sql, new { Username = username, Email = email })
            .ConfigureAwait(false);

        return count > 0;
    }

    /// <summary>
    /// Override RemoveAsync to implement soft delete
    /// </summary>
    public override async Task RemoveAsync(User entity, CancellationToken cancellationToken = default)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = "system"; // Should come from current user context

        const string sql = @"
            UPDATE aesir_user
            SET is_deleted = @IsDeleted,
                deleted_at = @DeletedAt,
                deleted_by = @DeletedBy,
                updated_at = @UpdatedAt
            WHERE id = @Id";

        using var connection = await ConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);

        var affected = await connection
            .ExecuteAsync(sql, new
            {
                entity.Id,
                entity.IsDeleted,
                entity.DeletedAt,
                entity.DeletedBy,
                UpdatedAt = DateTime.UtcNow
            })
            .ConfigureAwait(false);

        if (affected == 0)
        {
            throw new InvalidOperationException($"Failed to delete user with Id {entity.Id}");
        }

        Logger.LogInformation("Soft deleted user with Id {Id}", entity.Id);
    }
}
```

### 4. Service with Exception-Based Error Handling

```csharp
using Aesir.Infrastructure.Data;
using Aesir.Modules.Users.Data.Models;
using Aesir.Modules.Users.Data.Repositories;
using Aesir.Modules.Users.Models;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Aesir.Modules.Users.Services;

public interface IUserService : IService
{
    Task<UserDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}

public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IDbContext _dbContext;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository repository,
        IDbContext dbContext,
        ILogger<UserService> logger)
    {
        _repository = repository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting user by Id: {Id}", id);

        var user = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (user == null)
        {
            _logger.LogWarning("User not found with Id: {Id}", id);
            throw new KeyNotFoundException($"User with Id {id} not found");
        }

        return MapToDto(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new user with username: {Username}", request.Username);

        // Execute within a transaction using UnitOfWorkAsync
        return await _dbContext.UnitOfWorkAsync(async connection =>
        {
            // Check if user exists
            var exists = await _repository.ExistsAsync(request.Username, request.Email, cancellationToken)
                .ConfigureAwait(false);

            if (exists)
            {
                _logger.LogWarning("User already exists with username: {Username} or email: {Email}",
                    request.Username, request.Email);
                throw new InvalidOperationException("A user with this username or email already exists");
            }

            // Create user entity
            var user = new User
            {
                Id = Guid.NewGuid(),  // Generate new Guid
                Username = request.Username,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system",  // Should come from current user context
                IsDeleted = false
            };

            // Add to repository
            await _repository.AddAsync(user, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully created user with Id: {Id}", user.Id);
            return MapToDto(user);
        }, withTransaction: true).ConfigureAwait(false);
    }

    public async Task<UserDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login attempt for username: {Username}", request.Username);

        var user = await _repository.GetByUsernameAsync(request.Username, cancellationToken)
            .ConfigureAwait(false);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        _logger.LogInformation("Successful login for user: {Username}", request.Username);
        return MapToDto(user);
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            CreatedAt = user.CreatedAt
        };
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private static bool VerifyPassword(string password, string passwordHash)
    {
        var hashToVerify = HashPassword(password);
        return hashToVerify == passwordHash;
    }
}
```

### 5. FluentMigrator Migration

```csharp
using FluentMigrator;

namespace Aesir.Modules.Users.Migrations;

/// <summary>
/// Creates the users table with proper aesir_ prefix and snake_case columns
/// </summary>
[Migration(20251025120001)]
public class Migration20251025120001_AddUsersTable : Migration
{
    public override void Up()
    {
        // Create users table with aesir_ prefix
        Create.Table("aesir_user")
            // Primary key - always use Guid
            .WithColumn("id").AsGuid().PrimaryKey()

            // Core fields - all lowercase snake_case
            .WithColumn("username").AsString(50).NotNullable().Unique()
            .WithColumn("email").AsString(255).NotNullable()
            .WithColumn("password_hash").AsString(255).NotNullable()
            .WithColumn("first_name").AsString(100).NotNullable()
            .WithColumn("last_name").AsString(100).NotNullable()

            // Soft delete columns
            .WithColumn("is_deleted").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("deleted_at").AsDateTime().Nullable()
            .WithColumn("deleted_by").AsString(100).Nullable()

            // Audit columns
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
            .WithColumn("created_by").AsString(100).NotNullable()
            .WithColumn("updated_at").AsDateTime().Nullable()
            .WithColumn("updated_by").AsString(100).Nullable();

        // Create indexes with aesir_ prefix in the index name
        Create.Index("ix_aesir_user_username")
            .OnTable("aesir_user")
            .OnColumn("username")
            .Unique();

        Create.Index("ix_aesir_user_email")
            .OnTable("aesir_user")
            .OnColumn("email");

        Create.Index("ix_aesir_user_is_deleted")
            .OnTable("aesir_user")
            .OnColumn("is_deleted");
    }

    public override void Down()
    {
        Delete.Table("aesir_user");
    }
}
```

### 6. Controller with Proper HTTP Methods

```csharp
using Aesir.Modules.Users.Models;
using Aesir.Modules.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Users.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("GET /api/users - Retrieving all users");
            var users = await _userService.GetAllAsync(cancellationToken).ConfigureAwait(false);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            return StatusCode(500, new { error = "An error occurred while retrieving users" });
        }
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("GET /api/users/{Id} - Retrieving user", id);
            var user = await _userService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found with Id: {Id}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {Id}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the user" });
        }
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("POST /api/users - Creating user with username: {Username}", request.Username);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userService.CreateAsync(request, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully created user with Id: {Id}", user.Id);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create user: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { error = "An error occurred while creating the user" });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("PUT /api/users/{Id} - Updating user", id);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found with Id: {Id}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {Id}", id);
            return StatusCode(500, new { error = "An error occurred while updating the user" });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("DELETE /api/users/{Id} - Deleting user", id);
            await _userService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found with Id: {Id}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {Id}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the user" });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("POST /api/users/login - Login attempt for: {Username}", request.Username);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userService.LoginAsync(request, cancellationToken).ConfigureAwait(false);

            // In a real implementation, generate JWT token here
            return Ok(new
            {
                user,
                token = "jwt-token-would-go-here"  // Placeholder
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Failed login for: {Username}", request.Username);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { error = "An error occurred during login" });
        }
    }
}
```

### 7. Project File Configuration

**CRITICAL**: Every module MUST include the `CopyModuleToApiServer` target to enable automatic deployment.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Infrastructure provides base repository, IEntity, IDbContext -->
    <ProjectReference Include="..\Aesir.Infrastructure\Aesir.Infrastructure.csproj" />

    <!-- Common provides shared utilities -->
    <ProjectReference Include="..\Aesir.Common\Aesir.Common.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- OPTIONAL: Only needed if module has database tables and migrations -->
    <PackageReference Include="FluentMigrator" Version="7.1.0" />

    <!-- Required for controllers -->
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Core" Version="2.2.5" />

    <!-- Logging -->
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.10" />
  </ItemGroup>

  <!--
      CRITICAL: Auto-copy target for module deployment
      This target automatically copies the module DLL to Api.Server after build.
      Without this, your module won't be discovered at runtime!
  -->
  <Target Name="CopyModuleToApiServer" AfterTargets="Build">
    <Message Text="[$(MSBuildProjectName)] Copying module to Api.Server..." Importance="high" />

    <!-- Copy module DLL -->
    <Copy SourceFiles="$(TargetPath)"
          DestinationFolder="$(ProjectDir)../Aesir.Api.Server/bin/$(Configuration)/$(TargetFramework)/"
          SkipUnchangedFiles="true" />

    <!-- Copy PDB for debugging -->
    <Copy SourceFiles="$(TargetDir)$(TargetName).pdb"
          DestinationFolder="$(ProjectDir)../Aesir.Api.Server/bin/$(Configuration)/$(TargetFramework)/"
          Condition="Exists('$(TargetDir)$(TargetName).pdb')"
          SkipUnchangedFiles="true" />

    <Message Text="[$(MSBuildProjectName)] Module copied successfully" Importance="high" />
  </Target>

</Project>
```

**What This Does:**
- `AfterTargets="Build"` - Runs automatically after the module builds
- `SourceFiles="$(TargetPath)"` - Copies the built DLL
- `DestinationFolder` - Targets Api.Server's bin directory
- `SkipUnchangedFiles="true"` - Only copies if changed (faster builds)
- Also copies `.pdb` file for debugging support

## Key Infrastructure Components

### IEntity Interface

All entities must implement the `IEntity` interface:

```csharp
namespace Aesir.Infrastructure.Data;

public interface IEntity
{
    /// <summary>
    /// Primary key - always Guid type in AESIR
    /// </summary>
    Guid Id { get; set; }
}
```

### Repository Base Class

The `Repository<TEntity>` base class provides CRUD operations using Dapper.Contrib:

```csharp
namespace Aesir.Infrastructure.Data;

public abstract class Repository<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
{
    protected readonly IDbConnectionFactory ConnectionFactory;
    protected readonly ILogger<Repository<TEntity>> Logger;

    protected Repository(IDbConnectionFactory connectionFactory, ILogger<Repository<TEntity>> logger)
    {
        ConnectionFactory = connectionFactory;
        Logger = logger;
    }

    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting entity {EntityType} by Id {Id}", typeof(TEntity).Name, id);

        using var connection = await ConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);
        var entity = await connection.GetAsync<TEntity>(id).ConfigureAwait(false);

        return entity;
    }

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        // Generate new Guid if not set
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        Logger.LogInformation("Adding entity {EntityType} with Id {Id}", typeof(TEntity).Name, entity.Id);

        using var connection = await ConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);
        await connection.InsertAsync(entity).ConfigureAwait(false);
    }

    // Additional CRUD methods: UpdateAsync, RemoveAsync, GetAllAsync
    // All methods use direct return types and let exceptions propagate to the service layer
}
```

### IDbContext.UnitOfWorkAsync for Transactions

AESIR uses the `IDbContext.UnitOfWorkAsync` extension method pattern for managing database transactions. This provides a simpler, more functional approach compared to traditional unit of work patterns.

```csharp
namespace Aesir.Infrastructure.Data;

public interface IDbContext
{
    IDbConnection GetConnection();
}

public static class DbContextExtensions
{
    /// <summary>
    /// Executes a database operation within a unit of work pattern with optional transaction support.
    /// Automatically handles connection management, transaction lifecycle, and error handling.
    /// </summary>
    public static async Task<T> UnitOfWorkAsync<T>(
        this IDbContext dbContext,
        Func<IDbConnection, Task<T>> actionAsync,
        bool withTransaction = false)
    {
        using var connection = dbContext.GetConnection();
        connection.Open();

        if (withTransaction)
        {
            using var transaction = connection.BeginTransaction();
            var result = await actionAsync(connection);
            transaction.Commit();
            return result;
        }
        else
        {
            return await actionAsync(connection);
        }
    }

    /// <summary>
    /// Executes a database operation without a return value.
    /// </summary>
    public static async Task UnitOfWorkAsync(
        this IDbContext dbContext,
        Func<IDbConnection, Task> actionAsync,
        bool withTransaction = false)
    {
        using var connection = dbContext.GetConnection();
        connection.Open();

        if (withTransaction)
        {
            using var transaction = connection.BeginTransaction();
            await actionAsync(connection);
            transaction.Commit();
        }
        else
        {
            await actionAsync(connection);
        }
    }
}
```

**Usage Example:**

```csharp
// Simple operation without transaction
var users = await _dbContext.UnitOfWorkAsync(async connection =>
{
    return await connection.QueryAsync<User>("SELECT * FROM aesir_user");
}, withTransaction: false);

// Complex operation with transaction
var createdUser = await _dbContext.UnitOfWorkAsync(async connection =>
{
    // Multiple operations within a single transaction
    var user = new User { /* ... */ };
    await connection.InsertAsync(user);

    // If any exception is thrown, the transaction is automatically rolled back
    var profile = new UserProfile { UserId = user.Id };
    await connection.InsertAsync(profile);

    return user;
}, withTransaction: true);
```

**Key Benefits:**
- **Automatic resource management**: Connection and transaction disposal is handled automatically
- **Exception-based rollback**: Any exception automatically rolls back the transaction
- **Functional style**: Lambda-based approach is cleaner than explicit Begin/Commit/Rollback
- **Flexible**: Can be used with or without transactions via the `withTransaction` parameter

### Exception-Based Error Handling

AESIR uses exception-based error handling rather than Result<T> patterns:

**Guidelines:**
- **Services**: Throw specific exceptions (`KeyNotFoundException`, `InvalidOperationException`, `UnauthorizedAccessException`)
- **Repositories**: Let database exceptions propagate to the service layer
- **Controllers**: Catch exceptions and return appropriate HTTP status codes
- **Logging**: Log errors at the appropriate level (Warning for expected errors, Error for unexpected)

**Example Exception Types:**
- `KeyNotFoundException`: Entity not found by ID
- `InvalidOperationException`: Business rule violation
- `UnauthorizedAccessException`: Authentication/authorization failure
- `ArgumentException`: Invalid input parameters

**Controller Error Handling Pattern:**
```csharp
try
{
    var result = await _service.GetByIdAsync(id);
    return Ok(result);
}
catch (KeyNotFoundException ex)
{
    _logger.LogWarning(ex, "Entity not found");
    return NotFound(new { error = ex.Message });
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    return StatusCode(500, new { error = "An unexpected error occurred" });
}
```

### DapperColumnMapper

Automatically converts PascalCase properties to snake_case columns:

```csharp
namespace Aesir.Infrastructure.Data;

public static class DapperColumnMapper
{
    public static void Initialize()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        // Custom mapping for Dapper.Contrib
        SqlMapperExtensions.TableNameMapper = (type) =>
        {
            var tableAttribute = type.GetCustomAttribute<TableAttribute>();
            return tableAttribute?.Name ?? type.Name;
        };
    }
}
```

## Database Naming Conventions

### CRITICAL Rules

1. **ALL database objects MUST use `aesir_` prefix**
2. **ALL identifiers MUST use lowercase snake_case**
3. **ALL primary keys MUST be Guid type**

### Examples

```sql
-- CORRECT: Table with aesir_ prefix and snake_case
CREATE TABLE aesir_product (
    id UUID PRIMARY KEY,
    product_name VARCHAR(100) NOT NULL,
    unit_price DECIMAL(10,2) NOT NULL,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP NOT NULL
);

-- INCORRECT: Missing prefix, wrong case
CREATE TABLE "Products" (
    "Id" SERIAL PRIMARY KEY,  -- Wrong: Auto-increment instead of UUID
    "ProductName" VARCHAR(100),  -- Wrong: PascalCase
    "UnitPrice" DECIMAL(10,2)  -- Wrong: PascalCase
);
```

### Mapping Between C# and Database

| C# Property | Database Column | SQL Type |
|-------------|-----------------|----------|
| `Id` | `id` | `UUID` |
| `FirstName` | `first_name` | `VARCHAR` |
| `IsActive` | `is_active` | `BOOLEAN` |
| `CreatedAt` | `created_at` | `TIMESTAMP` |
| `OrderItems` | `aesir_order_item` (table) | N/A |

## Inter-Module Communication

### Option 1: Direct Service Injection

Modules can inject services from other modules:

```csharp
public class OrderService : IOrderService
{
    private readonly IUserService _userService;  // From Users module
    private readonly IProductService _productService;  // From Products module
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IUserService userService,
        IProductService productService,
        ILogger<OrderService> logger)
    {
        _userService = userService;
        _productService = productService;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        // Validate user exists
        var userResult = await _userService.GetByIdAsync(request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (userResult.IsFailure)
        {
            _logger.LogWarning("Order creation failed - user not found: {UserId}", request.UserId);
            return Result<OrderDto>.Failure("User not found");
        }

        // Validate product exists
        var productResult = await _productService.GetByIdAsync(request.ProductId, cancellationToken)
            .ConfigureAwait(false);

        if (productResult.IsFailure)
        {
            _logger.LogWarning("Order creation failed - product not found: {ProductId}", request.ProductId);
            return Result<OrderDto>.Failure("Product not found");
        }

        // Create order with proper transaction handling...
    }
}
```

### Option 2: Repository Cross-Access

For read-only access to other module's data:

```csharp
public class ReportingService : IReportingService
{
    private readonly IUserRepository _userRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;

    public async Task<Result<UserOrderSummary>> GetUserOrderSummaryAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Direct repository access for reporting
        const string sql = @"
            SELECT
                u.id,
                u.username,
                COUNT(o.id) as total_orders,
                SUM(o.total_amount) as total_spent
            FROM aesir_user u
            LEFT JOIN aesir_order o ON u.id = o.user_id
            WHERE u.id = @UserId
                AND u.is_deleted = false
            GROUP BY u.id, u.username";

        // Execute cross-module query...
    }
}
```

## Testing Modules

### Unit Tests for Services

```csharp
using Aesir.Common.Results;
using Aesir.Modules.Users.Data.Entities;
using Aesir.Modules.Users.Data.Repositories;
using Aesir.Modules.Users.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _service = new UserService(_mockRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(Result<User?>.Success(user));

        // Act
        var result = await _service.GetByIdAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("testuser", result.Value.Username);
    }

    [Fact]
    public async Task CreateAsync_WhenUsernameExists_ReturnsFailure()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "existing",
            Email = "new@example.com",
            Password = "password123"
        };

        _mockRepository
            .Setup(r => r.ExistsAsync(request.Username, request.Email, default))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("already exists", result.Error);

        // Verify transaction was rolled back
        _mockUnitOfWork.Verify(u => u.RollbackAsync(default), Times.Once);
    }
}
```

### Integration Tests for Controllers

```csharp
using System.Net;
using System.Net.Http.Json;
using Aesir.Modules.Users.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class UsersControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UsersControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateUser_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = $"user_{Guid.NewGuid()}",
            Email = $"test_{Guid.NewGuid()}@example.com",
            Password = "SecurePass123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);
        Assert.Equal(request.Username, user.Username);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Fact]
    public async Task GetUserById_Unauthorized_Returns401()
    {
        // Act
        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

## Troubleshooting

### Module Not Discovered

**Problem**: Module doesn't load at startup

**Solutions**:
1. Ensure assembly name starts with `Aesir.Modules.`
2. Check that module class implements `IModule`
3. Verify module DLL is in the output directory
4. Check logs for discovery errors:
   ```
   No modules discovered
   Found module: Aesir.Modules.Users.UsersModule
   ```

### Migration Not Running

**Problem**: Database tables not created

**Solutions**:
1. Ensure migration class has `[Migration(YYYYMMDDHHMMSS)]` attribute
2. Verify FluentMigrator package is installed in module project
3. Check that module assembly is being discovered
4. Review migration runner logs for errors
5. Ensure connection string is configured correctly

### Column Mapping Issues

**Problem**: "Column 'FirstName' does not exist" errors

**Solutions**:
1. Verify `DapperColumnMapper.Initialize()` is called in `Program.cs`
2. Check that database columns use snake_case (`first_name` not `FirstName`)
3. Ensure table has `aesir_` prefix
4. Review SQL queries for proper column naming

### Transaction Failures

**Problem**: Operations not rolling back on error

**Solutions**:
1. Ensure `IUnitOfWork` is properly scoped (Scoped lifetime)
2. Call `BeginTransactionAsync` before operations
3. Always call `CommitAsync` or `RollbackAsync`
4. Dispose `IUnitOfWork` properly (using statement or DI container)

## Best Practices

### 1. Use Exception-Based Error Handling
- Throw specific exceptions from service methods (`KeyNotFoundException`, `InvalidOperationException`, `UnauthorizedAccessException`)
- Catch and handle exceptions in controllers with appropriate HTTP status codes
- Provide meaningful error messages in exceptions
- Log failures appropriately with structured logging

### 2. Only Create Data/ Directory When Needed
- The `Data/` directory is **completely optional**
- Only include it if your module has **local private data needs** (stores its own database tables)
- If your module only calls external APIs or coordinates other modules, skip Data/ entirely
- This keeps modules lightweight and focused on their specific responsibilities

### 3. Follow Naming Conventions Strictly
- Database: `aesir_` prefix with snake_case
- C# Code: PascalCase for public members
- Migrations: `Migration{Timestamp}_{Description}`
- Indexes: `ix_aesir_tablename_columnname`

### 4. Implement Soft Deletes (Optional)
Soft deletes are an optional pattern. Only implement if your module requires data retention or audit history:
- Add `IsDeleted`, `DeletedAt`, `DeletedBy` properties to entities (PascalCase)
- Database columns: `is_deleted`, `deleted_at`, `deleted_by` (snake_case)
- Override `RemoveAsync` in repositories to UPDATE instead of DELETE
- Filter soft-deleted records in queries: `WHERE is_deleted = false`
- Consider archiving old soft-deleted records periodically

### 5. Add Audit Trails (Optional)
Audit trails are optional. Only implement if your module needs to track data changes:
- Add `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` properties to entities (PascalCase)
- Database columns: `created_at`, `created_by`, `updated_at`, `updated_by` (snake_case)
- Set audit fields in service layer, not repository
- Consider using middleware for automatic user context injection

### 6. Use Structured Logging
- Inject `ILogger<T>` in all classes
- Use structured logging with parameters
- Log at appropriate levels (Debug, Info, Warning, Error)
- Include correlation IDs for request tracking

### 7. Write Comprehensive Tests
- Unit test services with mocked dependencies
- Integration test API endpoints
- Test error conditions and edge cases
- Verify transaction behavior

### 8. Document Your Module
- Add XML documentation comments
- Include README in module folder
- Document any special configuration
- Provide usage examples

## Summary

The AESIR module system provides:

- ✅ **Convention-based discovery** - Automatic module and migration loading
- ✅ **Dapper-based data access** - Fast, lightweight ORM with full SQL control
- ✅ **FluentMigrator migrations** - Version-controlled database schema
- ✅ **Exception-based error handling** - Clear, predictable error flow
- ✅ **Transaction support** - ACID compliance with IDbContext.UnitOfWorkAsync
- ✅ **Strict naming conventions** - `aesir_` prefix with snake_case
- ✅ **Comprehensive logging** - Structured logging with NLog
- ✅ **Testability** - Easy to unit and integration test
- ✅ **Scalability** - Add modules without modifying core
- ✅ **Type safety** - Guid primary keys and strong typing throughout