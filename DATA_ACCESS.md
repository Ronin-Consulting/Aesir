# Data Access Layer Documentation

## Overview

The AESIR data access layer provides a robust, scalable, and maintainable approach to database operations using modern patterns and best practices. Built on top of Dapper for performance and Dapper.Contrib for convenience, it implements the Repository and Unit of Work patterns while maintaining strict naming conventions and comprehensive error handling.

### Technology Stack

- **Dapper 2.1.66**: High-performance micro-ORM for .NET
- **Dapper.Contrib 2.0.78**: Extensions for CRUD operations
- **FluentMigrator 7.1.0**: Database migration framework
- **PostgreSQL 15+**: Primary database platform
- **NLog 5.3.x**: Structured logging throughout

### Design Principles

1. **Performance First**: Using Dapper for optimal query performance
2. **Separation of Concerns**: Clear boundaries between data access, business logic, and presentation
3. **Testability**: Interfaces and dependency injection for unit testing
4. **Consistency**: Strict naming conventions and patterns across all modules
5. **Maintainability**: Clear code organization and comprehensive documentation

## Database Naming Convention

### CRITICAL: The aesir_ Prefix Requirement

**ALL database identifiers MUST use the `aesir_` prefix with lowercase snake_case naming**. This is a non-negotiable convention that ensures namespace isolation and consistency across the entire AESIR system.

### Naming Rules

| Database Element | Convention | Example |
|-----------------|------------|---------|
| Tables | `aesir_` + entity name (snake_case) | `aesir_user`, `aesir_product`, `aesir_order_item` |
| Columns | snake_case (no prefix) | `first_name`, `is_active`, `created_at` |
| Primary Keys | `pk_` + table name | `pk_aesir_user`, `pk_aesir_product` |
| Indexes | `ix_` + table name + column(s) | `ix_aesir_user_username`, `ix_aesir_product_name` |
| Foreign Keys | `fk_` + table + reference | `fk_aesir_order_user_id` |

### C# to Database Mapping

The system automatically maps between C# PascalCase properties and database snake_case columns using `DapperColumnMapper`:

| C# Property (PascalCase) | Database Column (snake_case) | Notes |
|--------------------------|------------------------------|-------|
| `Id` | `id` | UUID/Guid type |
| `FirstName` | `first_name` | Automatic mapping |
| `IsActive` | `is_active` | Boolean fields |
| `CreatedAt` | `created_at` | DateTime fields |
| `DeletedBy` | `deleted_by` | Nullable Guid |

### Correct vs Incorrect Examples

```sql
-- CORRECT: aesir_ prefix with snake_case
CREATE TABLE aesir_user (
    id UUID PRIMARY KEY,
    first_name VARCHAR(100),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- INCORRECT: Missing aesir_ prefix
CREATE TABLE users (  -- WRONG!
    id UUID PRIMARY KEY
);

-- INCORRECT: PascalCase columns
CREATE TABLE aesir_user (
    Id UUID PRIMARY KEY,  -- WRONG!
    FirstName VARCHAR(100)  -- WRONG!
);

-- INCORRECT: Quoted identifiers
CREATE TABLE "AesirUsers" (  -- WRONG!
    "FirstName" VARCHAR(100)  -- WRONG!
);
```

## Entity Design

### The IEntity Interface

All entities must implement the `IEntity` interface, which requires a Guid identifier:

```csharp
namespace Aesir.Infrastructure.Data;

public interface IEntity
{
    Guid Id { get; set; }
}
```

### Required Attributes

Entities use Dapper.Contrib attributes for ORM mapping:

| Attribute | Purpose | Usage |
|-----------|---------|-------|
| `[Table("aesir_xxx")]` | Maps class to database table | Required on all entities |
| `[ExplicitKey]` | Marks Guid primary key | Required for Id property |
| `[Write(false)]` | Excludes from INSERT/UPDATE | For computed columns |
| `[Computed]` | Marks computed columns | For database-generated values |

### Complete User Entity Example

```csharp
using Aesir.Infrastructure.Data;
using Dapper.Contrib.Extensions;

namespace Aesir.Modules.Users.Data.Entities;

/// <summary>
/// Represents a user in the AESIR system.
/// Demonstrates all required fields and conventions.
/// </summary>
[Table("aesir_user")]  // CRITICAL: aesir_ prefix with snake_case
public class User : IEntity
{
    /// <summary>
    /// Primary key - always Guid type with [ExplicitKey] attribute
    /// </summary>
    [ExplicitKey]  // NOT [Key] - that's for auto-increment integers
    public Guid Id { get; set; }

    // Business fields
    public string Username { get; set; } = string.Empty;  // Maps to: username
    public string Email { get; set; } = string.Empty;     // Maps to: email
    public string? FirstName { get; set; }                // Maps to: first_name
    public string? LastName { get; set; }                 // Maps to: last_name
    public bool IsActive { get; set; } = true;           // Maps to: is_active

    // Soft delete fields
    public bool IsDeleted { get; set; }                  // Maps to: is_deleted
    public DateTime? DeletedAt { get; set; }             // Maps to: deleted_at
    public Guid? DeletedBy { get; set; }                 // Maps to: deleted_by

    // Audit trail fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // Maps to: created_at
    public Guid? CreatedBy { get; set; }                 // Maps to: created_by
    public DateTime? UpdatedAt { get; set; }             // Maps to: updated_at
    public Guid? UpdatedBy { get; set; }                 // Maps to: updated_by

    // Computed property example (not saved to database)
    [Write(false)]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
```

### Guid Primary Keys

All entities use Guid as the primary key type. This provides:

- **Global uniqueness** without coordination
- **Better security** (no sequential IDs to enumerate)
- **Easier data merging** across systems
- **Client-side generation** capability

## Repository Pattern

### The IRepository<TEntity> Interface

The base repository interface provides standard CRUD operations:

```csharp
public interface IRepository<TEntity> where TEntity : class, IEntity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
```

### Repository<TEntity> Base Class

The base repository class provides implementation for all standard operations:

#### Key Features

1. **Automatic Guid Generation**: If `entity.Id == Guid.Empty`, a new Guid is generated
2. **Dapper.Contrib Integration**: Uses `GetAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`
3. **Protected Query Methods**: For custom queries in derived repositories
4. **Comprehensive Logging**: Debug and Info level logging for all operations
5. **ConfigureAwait(false)**: Proper async context handling

#### Protected Methods for Custom Queries

```csharp
// Query multiple entities
protected Task<IEnumerable<TEntity>> QueryAsync(
    string sql,
    object? parameters = null,
    CancellationToken cancellationToken = default)

// Query single entity
protected Task<TEntity?> QueryFirstOrDefaultAsync(
    string sql,
    object? parameters = null,
    CancellationToken cancellationToken = default)

// Execute non-query commands
protected Task<int> ExecuteAsync(
    string sql,
    object? parameters = null,
    CancellationToken cancellationToken = default)
```

### Complete UserRepository Example

```csharp
using Aesir.Infrastructure.Data;
using Aesir.Modules.Users.Data.Entities;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Users.Data.Repositories;

/// <summary>
/// Repository implementation for User entity using Dapper.
/// Demonstrates custom queries alongside base CRUD operations.
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    /// <summary>
    /// Constructor must pass logger with correct generic type to base class
    /// </summary>
    public UserRepository(
        IDbContext dbContext,
        ILogger<Repository<User>> logger)  // Note: Repository<User>, not UserRepository
        : base(dbContext, logger)
    {
    }

    /// <summary>
    /// Custom query to find user by username
    /// </summary>
    public async Task<User?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting user by username: {Username}", username);

        // CRITICAL: Use aesir_ prefix and snake_case in SQL
        const string sql = @"
            SELECT * FROM aesir_user
            WHERE username = @Username
              AND is_deleted = false";  // Filter soft-deleted records

        return await QueryFirstOrDefaultAsync(
            sql,
            new { Username = username },
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Custom query to get all active users
    /// </summary>
    public async Task<IEnumerable<User>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting all active users");

        const string sql = @"
            SELECT * FROM aesir_user
            WHERE is_deleted = false
              AND is_active = true
            ORDER BY username";

        return await QueryAsync(sql, null, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Override RemoveAsync to implement soft delete
    /// </summary>
    public override async Task<bool> RemoveAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Soft deleting user with Id {Id}", id);

        // UPDATE instead of DELETE for soft delete
        const string sql = @"
            UPDATE aesir_user
            SET is_deleted = true,
                deleted_at = @DeletedAt
            WHERE id = @Id
              AND is_deleted = false";

        var rowsAffected = await ExecuteAsync(sql, new
        {
            Id = id,
            DeletedAt = DateTime.UtcNow
        }, cancellationToken).ConfigureAwait(false);

        return rowsAffected > 0;
    }

    /// <summary>
    /// Example of a complex query with joins
    /// </summary>
    public async Task<IEnumerable<UserWithRoles>> GetUsersWithRolesAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                u.id, u.username, u.email,
                r.id, r.name
            FROM aesir_user u
            LEFT JOIN aesir_user_role ur ON u.id = ur.user_id
            LEFT JOIN aesir_role r ON ur.role_id = r.id
            WHERE u.is_deleted = false";

        using var connection = DbContext.GetConnection();
        connection.Open();

        var userDict = new Dictionary<Guid, UserWithRoles>();

        await connection.QueryAsync<User, Role, UserWithRoles>(
            sql,
            (user, role) =>
            {
                if (!userDict.TryGetValue(user.Id, out var userWithRoles))
                {
                    userWithRoles = new UserWithRoles { User = user };
                    userDict.Add(user.Id, userWithRoles);
                }
                if (role != null)
                    userWithRoles.Roles.Add(role);
                return userWithRoles;
            },
            splitOn: "id"
        ).ConfigureAwait(false);

        return userDict.Values;
    }
}
```

### Custom Queries with Raw Dapper

When Dapper.Contrib methods aren't sufficient, use raw Dapper methods:

```csharp
// Pagination example
public async Task<(IEnumerable<User> Users, int TotalCount)> GetPaginatedAsync(
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
{
    const string countSql = @"
        SELECT COUNT(*)
        FROM aesir_user
        WHERE is_deleted = false";

    const string dataSql = @"
        SELECT * FROM aesir_user
        WHERE is_deleted = false
        ORDER BY created_at DESC
        LIMIT @PageSize OFFSET @Offset";

    using var connection = DbContext.GetConnection();
    connection.Open();

    var totalCount = await connection.ExecuteScalarAsync<int>(countSql)
        .ConfigureAwait(false);

    var users = await connection.QueryAsync<User>(
        dataSql,
        new { PageSize = pageSize, Offset = (page - 1) * pageSize }
    ).ConfigureAwait(false);

    return (users, totalCount);
}
```

## Unit of Work Pattern

### IUnitOfWork Interface

The Unit of Work pattern manages transactions across multiple repository operations:

```csharp
public interface IUnitOfWork : IDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }
    void BeginTransaction();
    void Commit();
    void Rollback();
}
```

### Transaction Management

Use Unit of Work when you need to ensure multiple operations succeed or fail together:

```csharp
public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    public async Task<Result<Order>> CreateOrderAsync(CreateOrderRequest request)
    {
        try
        {
            _unitOfWork.BeginTransaction();

            // Create order
            var order = new Order
            {
                CustomerId = request.CustomerId,
                Items = request.Items,
                TotalAmount = CalculateTotal(request.Items)
            };

            order = await _orderRepository.AddAsync(order);

            // Update inventory for each item
            foreach (var item in request.Items)
            {
                var inventory = await _inventoryRepository.GetByProductIdAsync(item.ProductId);
                if (inventory.Quantity < item.Quantity)
                {
                    _unitOfWork.Rollback();
                    return Result<Order>.Failure($"Insufficient inventory for product {item.ProductId}");
                }

                inventory.Quantity -= item.Quantity;
                await _inventoryRepository.UpdateAsync(inventory);
            }

            _unitOfWork.Commit();
            _logger.LogInformation("Order {OrderId} created successfully", order.Id);

            return Result<Order>.Success(order);
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            _logger.LogError(ex, "Failed to create order");
            return Result<Order>.Failure("Failed to create order. Please try again.");
        }
    }
}
```

### Best Practices for Unit of Work

1. **Always use try-catch-finally** or `using` statements
2. **Rollback on any exception** to maintain consistency
3. **Keep transactions short** to avoid locks
4. **Log transaction boundaries** for debugging
5. **Consider retry logic** for transient failures

## Column Mapping

### DapperColumnMapper Initialization

The `DapperColumnMapper` must be initialized once during application startup:

```csharp
// In Program.cs
using Aesir.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Initialize Dapper column mapping
DapperColumnMapper.Initialize();

// Rest of configuration...
```

### How Automatic Mapping Works

The mapper automatically converts between naming conventions:

1. **PascalCase → snake_case** (C# to Database)
   - `FirstName` → `first_name`
   - `IsActive` → `is_active`
   - `CreatedAt` → `created_at`

2. **snake_case → PascalCase** (Database to C#)
   - `first_name` → `FirstName`
   - `is_active` → `IsActive`
   - `created_at` → `CreatedAt`

### When to Use [Column] Attribute

In rare cases where automatic mapping isn't sufficient, use the `[Column]` attribute:

```csharp
public class Product : IEntity
{
    [ExplicitKey]
    public Guid Id { get; set; }

    // Special case: acronym handling
    [Column("sku")]  // Maps to 'sku' instead of 's_k_u'
    public string SKU { get; set; }

    // Legacy column name
    [Column("product_desc")]  // Different from property name
    public string Description { get; set; }
}
```

## Migrations with FluentMigrator

### Creating Migrations in Modules

Each module manages its own migrations in a dedicated Migrations folder:

```
Aesir.Modules.Users/
├── Migrations/
│   ├── Migration20251025120001_AddUsersTable.cs
│   └── Migration20251025120002_AddUserIndexes.cs
├── Data/
├── Services/
└── Controllers/
```

### Naming Convention

Use timestamp format for migration ordering:

```
[Migration(YYYYMMDDHHMMSS)]
Migration{timestamp}_{Description}
```

Examples:
- `Migration20251025120001_AddUsersTable`
- `Migration20251025143022_AddProductCategories`
- `Migration20251026091500_AddUserRoles`

### Migration Requirements

1. **Always implement both `Up()` and `Down()` methods**
2. **Use aesir_ prefix for all database objects**
3. **Use `.AsGuid()` for primary keys (never `.AsInt32().Identity()`)**
4. **Include appropriate indexes for query performance**
5. **Add FluentMigrator package to module project**

### Example: Complete Users Table Migration

```csharp
using FluentMigrator;

namespace Aesir.Modules.Users.Migrations;

/// <summary>
/// Creates the aesir_user table with all required fields and indexes.
/// </summary>
[Migration(20251025120001)]
public class Migration20251025120001_AddUsersTable : Migration
{
    public override void Up()
    {
        // Create the main table
        Create.Table("aesir_user")
            // Primary key - ALWAYS use Guid
            .WithColumn("id").AsGuid().PrimaryKey("pk_aesir_user")

            // Business fields
            .WithColumn("username").AsString(50).NotNullable()
            .WithColumn("email").AsString(255).NotNullable()
            .WithColumn("password_hash").AsString(255).NotNullable()
            .WithColumn("first_name").AsString(100).Nullable()
            .WithColumn("last_name").AsString(100).Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)

            // Soft delete fields
            .WithColumn("is_deleted").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("deleted_at").AsDateTime().Nullable()
            .WithColumn("deleted_by").AsGuid().Nullable()

            // Audit trail fields
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("created_by").AsGuid().Nullable()
            .WithColumn("updated_at").AsDateTime().Nullable()
            .WithColumn("updated_by").AsGuid().Nullable();

        // Create unique indexes for business constraints
        Create.Index("ix_aesir_user_username")
            .OnTable("aesir_user")
            .OnColumn("username")
            .Ascending()
            .WithOptions()
            .Unique()
            .Where("is_deleted = false");  // Partial index for soft deletes

        Create.Index("ix_aesir_user_email")
            .OnTable("aesir_user")
            .OnColumn("email")
            .Ascending()
            .WithOptions()
            .Unique()
            .Where("is_deleted = false");

        // Performance indexes
        Create.Index("ix_aesir_user_is_deleted")
            .OnTable("aesir_user")
            .OnColumn("is_deleted")
            .Ascending();

        Create.Index("ix_aesir_user_created_at")
            .OnTable("aesir_user")
            .OnColumn("created_at")
            .Descending();  // Most recent first
    }

    public override void Down()
    {
        // Remove in reverse order
        Delete.Index("ix_aesir_user_created_at").OnTable("aesir_user");
        Delete.Index("ix_aesir_user_is_deleted").OnTable("aesir_user");
        Delete.Index("ix_aesir_user_email").OnTable("aesir_user");
        Delete.Index("ix_aesir_user_username").OnTable("aesir_user");
        Delete.Table("aesir_user");
    }
}
```

### Complex Migration Examples

#### Adding Foreign Keys

```csharp
[Migration(20251026100000)]
public class Migration20251026100000_AddUserRoles : Migration
{
    public override void Up()
    {
        // Create role table
        Create.Table("aesir_role")
            .WithColumn("id").AsGuid().PrimaryKey("pk_aesir_role")
            .WithColumn("name").AsString(50).NotNullable()
            .WithColumn("description").AsString(255).Nullable();

        // Create junction table for many-to-many relationship
        Create.Table("aesir_user_role")
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("role_id").AsGuid().NotNullable()
            .WithColumn("assigned_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("assigned_by").AsGuid().Nullable();

        // Composite primary key
        Create.PrimaryKey("pk_aesir_user_role")
            .OnTable("aesir_user_role")
            .Columns("user_id", "role_id");

        // Foreign keys
        Create.ForeignKey("fk_aesir_user_role_user")
            .FromTable("aesir_user_role").ForeignColumn("user_id")
            .ToTable("aesir_user").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("fk_aesir_user_role_role")
            .FromTable("aesir_user_role").ForeignColumn("role_id")
            .ToTable("aesir_role").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        // Indexes for foreign keys
        Create.Index("ix_aesir_user_role_user_id")
            .OnTable("aesir_user_role")
            .OnColumn("user_id");

        Create.Index("ix_aesir_user_role_role_id")
            .OnTable("aesir_user_role")
            .OnColumn("role_id");
    }

    public override void Down()
    {
        Delete.ForeignKey("fk_aesir_user_role_user").OnTable("aesir_user_role");
        Delete.ForeignKey("fk_aesir_user_role_role").OnTable("aesir_user_role");
        Delete.Table("aesir_user_role");
        Delete.Table("aesir_role");
    }
}
```

## Result Pattern for Error Handling

### Why Use the Result Pattern?

The Result pattern provides a functional approach to error handling that:

1. **Makes errors explicit** in method signatures
2. **Avoids exceptions** for expected failure scenarios
3. **Provides consistent** error handling across services
4. **Improves performance** by avoiding exception overhead
5. **Enables better testing** of failure scenarios

### Result<T> and Result Classes

```csharp
// For operations that return a value
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value { get; }
    public string? Error { get; }

    public static Result<T> Success(T value);
    public static Result<T> Failure(string error);
}

// For operations without a return value
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    public static Result Success();
    public static Result Failure(string error);
}
```

### Complete UserService Example

```csharp
using Aesir.Infrastructure.Common;
using Aesir.Infrastructure.Data;
using Aesir.Modules.Users.Data.Entities;
using Aesir.Modules.Users.Data.Repositories;
using Aesir.Modules.Users.Models;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Users.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UserService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new user with validation
    /// </summary>
    public async Task<Result<UserDto>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating user with username: {Username}", request.Username);

        // Validation - return failures for expected scenarios
        if (string.IsNullOrWhiteSpace(request.Username))
            return Result<UserDto>.Failure("Username is required");

        if (request.Username.Length < 3)
            return Result<UserDto>.Failure("Username must be at least 3 characters");

        // Check for duplicate username
        var existingUser = await _repository.GetByUsernameAsync(
            request.Username,
            cancellationToken
        ).ConfigureAwait(false);

        if (existingUser != null)
        {
            _logger.LogWarning("Username already exists: {Username}", request.Username);
            return Result<UserDto>.Failure($"Username '{request.Username}' is already taken");
        }

        // Check for duplicate email
        var existingByEmail = await _repository.GetByEmailAsync(
            request.Email,
            cancellationToken
        ).ConfigureAwait(false);

        if (existingByEmail != null)
        {
            _logger.LogWarning("Email already registered: {Email}", request.Email);
            return Result<UserDto>.Failure($"Email '{request.Email}' is already registered");
        }

        // Create the user entity
        var user = new User
        {
            Id = Guid.NewGuid(),  // Can be set here or let repository generate it
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = GetCurrentUserId()  // From context/claims
        };

        // Hash password (example - use proper hashing in production)
        user.PasswordHash = HashPassword(request.Password);

        try
        {
            // Save to database
            user = await _repository.AddAsync(user, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Successfully created user with Id: {UserId}", user.Id);

            // Map to DTO for response
            var dto = MapToDto(user);
            return Result<UserDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user");
            return Result<UserDto>.Failure("An error occurred while creating the user");
        }
    }

    /// <summary>
    /// Updates an existing user with validation
    /// </summary>
    public async Task<Result<UserDto>> UpdateAsync(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating user with Id: {Id}", id);

        // Get existing user
        var user = await _repository.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("User not found: {Id}", id);
            return Result<UserDto>.Failure($"User with Id {id} not found");
        }

        // Validate email uniqueness if changing
        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
        {
            var existingByEmail = await _repository.GetByEmailAsync(
                request.Email,
                cancellationToken
            ).ConfigureAwait(false);

            if (existingByEmail != null && existingByEmail.Id != id)
            {
                return Result<UserDto>.Failure($"Email '{request.Email}' is already registered");
            }

            user.Email = request.Email;
        }

        // Update fields
        if (request.FirstName != null)
            user.FirstName = request.FirstName;

        if (request.LastName != null)
            user.LastName = request.LastName;

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        // Set audit fields
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = GetCurrentUserId();

        try
        {
            var updated = await _repository.UpdateAsync(user, cancellationToken)
                .ConfigureAwait(false);

            if (!updated)
            {
                return Result<UserDto>.Failure("Failed to update user");
            }

            _logger.LogInformation("Successfully updated user: {Id}", id);

            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {Id}", id);
            return Result<UserDto>.Failure("An error occurred while updating the user");
        }
    }

    /// <summary>
    /// Soft deletes a user
    /// </summary>
    public async Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting user with Id: {Id}", id);

        var user = await _repository.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (user == null || user.IsDeleted)
        {
            return Result.Failure($"User with Id {id} not found");
        }

        // Prevent deleting system users
        if (IsSystemUser(user))
        {
            return Result.Failure("System users cannot be deleted");
        }

        try
        {
            var deleted = await _repository.RemoveAsync(id, cancellationToken)
                .ConfigureAwait(false);

            if (!deleted)
            {
                return Result.Failure("Failed to delete user");
            }

            _logger.LogInformation("Successfully deleted user: {Id}", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user {Id}", id);
            return Result.Failure("An error occurred while deleting the user");
        }
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
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
```

### Using Results in Controllers

```csharp
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var result = await _userService.CreateAsync(request);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value.Id },
            result.Value
        );
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _userService.GetByIdAsync(id);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}
```

## Soft Deletes

### Implementation in Entity

Soft delete requires three fields in your entity:

```csharp
public class Product : IEntity
{
    [ExplicitKey]
    public Guid Id { get; set; }

    // ... other properties ...

    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
```

### Repository Override Example

Override the `RemoveAsync` method to UPDATE instead of DELETE:

```csharp
public class ProductRepository : Repository<Product>, IProductRepository
{
    public override async Task<bool> RemoveAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Soft deleting product {Id}", id);

        const string sql = @"
            UPDATE aesir_product
            SET is_deleted = true,
                deleted_at = @DeletedAt,
                deleted_by = @DeletedBy
            WHERE id = @Id
              AND is_deleted = false";

        var rowsAffected = await ExecuteAsync(sql, new
        {
            Id = id,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = GetCurrentUserId()  // From context
        }, cancellationToken).ConfigureAwait(false);

        if (rowsAffected > 0)
        {
            Logger.LogInformation("Successfully soft deleted product {Id}", id);
        }
        else
        {
            Logger.LogWarning("Product {Id} not found or already deleted", id);
        }

        return rowsAffected > 0;
    }
}
```

### Query Filtering

Always filter soft-deleted records in queries:

```csharp
// In all SELECT queries
const string sql = @"
    SELECT * FROM aesir_product
    WHERE is_deleted = false
      AND category_id = @CategoryId";

// For unique constraints, use partial indexes
Create.Index("ix_aesir_product_sku")
    .OnTable("aesir_product")
    .OnColumn("sku")
    .Unique()
    .Where("is_deleted = false");  // Only enforce uniqueness for active records
```

### Restore Functionality

Implement restore capability when needed:

```csharp
public async Task<Result> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
{
    const string sql = @"
        UPDATE aesir_product
        SET is_deleted = false,
            deleted_at = NULL,
            deleted_by = NULL,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id
          AND is_deleted = true";

    var rowsAffected = await ExecuteAsync(sql, new
    {
        Id = id,
        UpdatedAt = DateTime.UtcNow,
        UpdatedBy = GetCurrentUserId()
    }, cancellationToken).ConfigureAwait(false);

    if (rowsAffected > 0)
    {
        _logger.LogInformation("Successfully restored product {Id}", id);
        return Result.Success();
    }

    return Result.Failure($"Product {id} not found or not deleted");
}
```

## Audit Trails

### Setting Audit Fields

Audit fields should be set at the service layer, not in repositories:

```csharp
public class ProductService
{
    public async Task<Result<Product>> CreateAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            // Set audit fields
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.UserId  // From ICurrentUserService
        };

        await _repository.AddAsync(product);
        return Result<Product>.Success(product);
    }

    public async Task<Result<Product>> UpdateAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _repository.GetByIdAsync(id);

        // Update properties...

        // Set audit fields
        product.UpdatedAt = DateTime.UtcNow;
        product.UpdatedBy = _currentUserService.UserId;

        await _repository.UpdateAsync(product);
        return Result<Product>.Success(product);
    }
}
```

### Querying Audit History

Create specialized methods for audit queries:

```csharp
public async Task<IEnumerable<AuditEntry>> GetAuditHistoryAsync(
    Guid entityId,
    CancellationToken cancellationToken = default)
{
    const string sql = @"
        SELECT
            id,
            'Created' as action,
            created_at as timestamp,
            created_by as user_id
        FROM aesir_product
        WHERE id = @EntityId

        UNION ALL

        SELECT
            id,
            'Updated' as action,
            updated_at as timestamp,
            updated_by as user_id
        FROM aesir_product
        WHERE id = @EntityId
          AND updated_at IS NOT NULL

        UNION ALL

        SELECT
            id,
            'Deleted' as action,
            deleted_at as timestamp,
            deleted_by as user_id
        FROM aesir_product
        WHERE id = @EntityId
          AND deleted_at IS NOT NULL

        ORDER BY timestamp DESC";

    return await QueryAsync<AuditEntry>(sql, new { EntityId = entityId }, cancellationToken)
        .ConfigureAwait(false);
}
```

## Logging

### Repository Layer Logging

Use Debug and Info levels in repositories:

```csharp
public class ProductRepository : Repository<Product>
{
    public override async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Debug level for read operations
        Logger.LogDebug("Getting product by Id {Id}", id);

        var product = await base.GetByIdAsync(id, cancellationToken);

        if (product == null)
        {
            Logger.LogDebug("Product {Id} not found", id);
        }

        return product;
    }

    public override async Task<Product> AddAsync(Product entity, CancellationToken cancellationToken = default)
    {
        // Info level for write operations
        Logger.LogInformation("Adding new product: {Name}", entity.Name);

        var result = await base.AddAsync(entity, cancellationToken);

        Logger.LogInformation("Successfully added product {Id}", result.Id);

        return result;
    }
}
```

### Service Layer Logging

Use Info and Warning levels in services:

```csharp
public class ProductService
{
    public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest request)
    {
        // Info level for business operations
        _logger.LogInformation("Creating product: {Name}", request.Name);

        // Warning level for business validation failures
        if (await IsDuplicateSkuAsync(request.Sku))
        {
            _logger.LogWarning("Duplicate SKU detected: {Sku}", request.Sku);
            return Result<ProductDto>.Failure($"SKU '{request.Sku}' already exists");
        }

        // ... create product ...

        _logger.LogInformation("Successfully created product {Id}: {Name}",
            product.Id, product.Name);

        return Result<ProductDto>.Success(dto);
    }
}
```

### Controller Layer Logging

Log API operations with context:

```csharp
[ApiController]
public class ProductsController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        _logger.LogInformation("POST /api/products - Creating product: {Name}", request.Name);

        var result = await _productService.CreateAsync(request);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to create product: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("Product created successfully: {Id}", result.Value.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogDebug("GET /api/products/{Id} - Retrieving product", id);

        var result = await _productService.GetByIdAsync(id);

        if (result.IsFailure)
        {
            _logger.LogDebug("Product {Id} not found", id);
            return NotFound();
        }

        return Ok(result.Value);
    }
}
```

### Structured Logging Best Practices

Always use named parameters for structured logging:

```csharp
// CORRECT - Structured logging with parameters
_logger.LogInformation("Creating order for customer {CustomerId} with {ItemCount} items",
    customerId, items.Count);

// INCORRECT - String interpolation
_logger.LogInformation($"Creating order for customer {customerId} with {items.Count} items");

// CORRECT - Complex object logging
_logger.LogDebug("Processing order {@Order}", order);  // @ serializes the object

// CORRECT - Performance metrics
using (_logger.BeginScope("OrderId: {OrderId}", orderId))
{
    var stopwatch = Stopwatch.StartNew();

    // ... process order ...

    _logger.LogInformation("Order processed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
}
```

## Best Practices

### Connection Management

Always properly manage database connections:

```csharp
// CORRECT - Using statement ensures disposal
using var connection = _dbContext.GetConnection();
connection.Open();
var result = await connection.QueryAsync<Product>(sql);

// INCORRECT - Manual disposal prone to leaks
var connection = _dbContext.GetConnection();
connection.Open();
var result = await connection.QueryAsync<Product>(sql);
connection.Dispose();  // May not be called if exception occurs
```

### Transaction Handling

Use transactions for multi-step operations:

```csharp
public async Task<Result> TransferFundsAsync(
    Guid fromAccountId,
    Guid toAccountId,
    decimal amount)
{
    using var connection = _dbContext.GetConnection();
    connection.Open();

    using var transaction = connection.BeginTransaction();

    try
    {
        // Debit source account
        await connection.ExecuteAsync(
            "UPDATE aesir_account SET balance = balance - @Amount WHERE id = @Id",
            new { Id = fromAccountId, Amount = amount },
            transaction
        );

        // Credit destination account
        await connection.ExecuteAsync(
            "UPDATE aesir_account SET balance = balance + @Amount WHERE id = @Id",
            new { Id = toAccountId, Amount = amount },
            transaction
        );

        transaction.Commit();
        return Result.Success();
    }
    catch (Exception ex)
    {
        transaction.Rollback();
        _logger.LogError(ex, "Transfer failed");
        return Result.Failure("Transfer failed");
    }
}
```

### Async/Await Usage

Always use ConfigureAwait(false) in library code:

```csharp
// CORRECT - ConfigureAwait(false) in library/service code
public async Task<Product?> GetByIdAsync(Guid id)
{
    var result = await _repository.GetByIdAsync(id)
        .ConfigureAwait(false);  // Avoids capturing context

    return result;
}

// CORRECT - No ConfigureAwait in ASP.NET Core controllers
[HttpGet("{id}")]
public async Task<IActionResult> Get(Guid id)
{
    var product = await _service.GetByIdAsync(id);  // No ConfigureAwait needed
    return Ok(product);
}
```

### Error Handling

Handle different error types appropriately:

```csharp
public async Task<Result<Product>> CreateProductAsync(CreateProductRequest request)
{
    try
    {
        // Validate business rules
        if (request.Price <= 0)
            return Result<Product>.Failure("Price must be greater than zero");

        // Check for duplicates
        var existing = await _repository.GetBySkuAsync(request.Sku);
        if (existing != null)
            return Result<Product>.Failure($"SKU '{request.Sku}' already exists");

        // Create product
        var product = new Product { /* ... */ };
        await _repository.AddAsync(product);

        return Result<Product>.Success(product);
    }
    catch (PostgresException ex) when (ex.SqlState == "23505")  // Unique violation
    {
        _logger.LogWarning(ex, "Duplicate key violation");
        return Result<Product>.Failure("A product with this SKU already exists");
    }
    catch (PostgresException ex) when (ex.SqlState == "23503")  // Foreign key violation
    {
        _logger.LogWarning(ex, "Foreign key violation");
        return Result<Product>.Failure("Invalid category specified");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error creating product");
        return Result<Product>.Failure("An unexpected error occurred");
    }
}
```

### Testing Strategies

#### Repository Testing with Test Database

```csharp
public class UserRepositoryTests : IDisposable
{
    private readonly IDbContext _dbContext;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        // Use test database
        var connectionString = "Host=localhost;Database=aesir_test;Username=test;Password=test";
        _dbContext = new PostgresDbContext(connectionString);

        // Run migrations
        var migrator = new MigrationRunner(/* ... */);
        migrator.MigrateUp();

        _repository = new UserRepository(
            _dbContext,
            new NullLogger<Repository<User>>()
        );
    }

    [Fact]
    public async Task GetByUsername_ReturnsUser_WhenExists()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com"
        };
        await _repository.AddAsync(user);

        // Act
        var result = await _repository.GetByUsernameAsync("testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("testuser", result.Username);
    }

    public void Dispose()
    {
        // Clean up test data
        using var connection = _dbContext.GetConnection();
        connection.Execute("TRUNCATE TABLE aesir_user CASCADE");
    }
}
```

#### Service Testing with Mocks

```csharp
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _service = new UserService(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            new NullLogger<UserService>()
        );
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailure_WhenUsernameExists()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "existing",
            Email = "new@example.com"
        };

        _repositoryMock.Setup(x => x.GetByUsernameAsync("existing", default))
            .ReturnsAsync(new User { Username = "existing" });

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("already taken", result.Error);
    }
}
```

## Common Patterns

### Finding by Unique Fields

```csharp
public async Task<T?> GetByUniqueFieldAsync<T>(
    string tableName,
    string fieldName,
    object fieldValue) where T : IEntity
{
    var sql = $@"
        SELECT * FROM {tableName}
        WHERE {fieldName} = @Value
          AND is_deleted = false";

    using var connection = _dbContext.GetConnection();
    connection.Open();

    return await connection.QueryFirstOrDefaultAsync<T>(
        sql,
        new { Value = fieldValue }
    ).ConfigureAwait(false);
}
```

### Pagination

```csharp
public class PaginatedResult<T>
{
    public IEnumerable<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public async Task<PaginatedResult<Product>> GetPaginatedAsync(
    int pageNumber = 1,
    int pageSize = 10,
    string? sortBy = null,
    bool descending = false)
{
    var offset = (pageNumber - 1) * pageSize;
    var orderBy = sortBy ?? "created_at";
    var direction = descending ? "DESC" : "ASC";

    var countSql = "SELECT COUNT(*) FROM aesir_product WHERE is_deleted = false";

    var dataSql = $@"
        SELECT * FROM aesir_product
        WHERE is_deleted = false
        ORDER BY {orderBy} {direction}
        LIMIT @PageSize OFFSET @Offset";

    using var connection = _dbContext.GetConnection();
    connection.Open();

    var totalCount = await connection.ExecuteScalarAsync<int>(countSql);
    var items = await connection.QueryAsync<Product>(
        dataSql,
        new { PageSize = pageSize, Offset = offset }
    );

    return new PaginatedResult<Product>
    {
        Items = items,
        TotalCount = totalCount,
        PageNumber = pageNumber,
        PageSize = pageSize
    };
}
```

### Filtering

```csharp
public async Task<IEnumerable<Product>> GetFilteredAsync(ProductFilter filter)
{
    var sql = new StringBuilder("SELECT * FROM aesir_product WHERE is_deleted = false");
    var parameters = new DynamicParameters();

    if (!string.IsNullOrWhiteSpace(filter.Name))
    {
        sql.Append(" AND name ILIKE @Name");
        parameters.Add("Name", $"%{filter.Name}%");
    }

    if (filter.MinPrice.HasValue)
    {
        sql.Append(" AND price >= @MinPrice");
        parameters.Add("MinPrice", filter.MinPrice.Value);
    }

    if (filter.MaxPrice.HasValue)
    {
        sql.Append(" AND price <= @MaxPrice");
        parameters.Add("MaxPrice", filter.MaxPrice.Value);
    }

    if (filter.CategoryIds?.Any() == true)
    {
        sql.Append(" AND category_id = ANY(@CategoryIds)");
        parameters.Add("CategoryIds", filter.CategoryIds.ToArray());
    }

    sql.Append(" ORDER BY created_at DESC");

    using var connection = _dbContext.GetConnection();
    connection.Open();

    return await connection.QueryAsync<Product>(sql.ToString(), parameters);
}
```

### Complex Joins

```csharp
public async Task<OrderDetails> GetOrderWithDetailsAsync(Guid orderId)
{
    const string sql = @"
        SELECT
            o.id, o.order_number, o.customer_id, o.total_amount, o.status,
            oi.id, oi.product_id, oi.quantity, oi.unit_price,
            p.id, p.name, p.sku,
            c.id, c.first_name, c.last_name, c.email
        FROM aesir_order o
        INNER JOIN aesir_order_item oi ON o.id = oi.order_id
        INNER JOIN aesir_product p ON oi.product_id = p.id
        INNER JOIN aesir_customer c ON o.customer_id = c.id
        WHERE o.id = @OrderId
          AND o.is_deleted = false";

    using var connection = _dbContext.GetConnection();
    connection.Open();

    OrderDetails? orderDetails = null;

    await connection.QueryAsync<Order, OrderItem, Product, Customer, OrderDetails>(
        sql,
        (order, item, product, customer) =>
        {
            if (orderDetails == null)
            {
                orderDetails = new OrderDetails
                {
                    Order = order,
                    Customer = customer,
                    Items = new List<OrderItemDetails>()
                };
            }

            orderDetails.Items.Add(new OrderItemDetails
            {
                Item = item,
                Product = product
            });

            return orderDetails;
        },
        new { OrderId = orderId },
        splitOn: "id,id,id"
    );

    return orderDetails;
}
```

### Bulk Operations

```csharp
public async Task<int> BulkInsertAsync(IEnumerable<Product> products)
{
    const string sql = @"
        INSERT INTO aesir_product (id, name, sku, price, created_at)
        VALUES (@Id, @Name, @Sku, @Price, @CreatedAt)";

    using var connection = _dbContext.GetConnection();
    connection.Open();

    // Prepare data
    var data = products.Select(p => new
    {
        Id = Guid.NewGuid(),
        p.Name,
        p.Sku,
        p.Price,
        CreatedAt = DateTime.UtcNow
    });

    return await connection.ExecuteAsync(sql, data);
}

public async Task<bool> BulkUpdatePricesAsync(Dictionary<Guid, decimal> priceUpdates)
{
    const string sql = @"
        UPDATE aesir_product
        SET price = @Price,
            updated_at = @UpdatedAt
        WHERE id = @Id";

    using var connection = _dbContext.GetConnection();
    connection.Open();

    using var transaction = connection.BeginTransaction();

    try
    {
        var parameters = priceUpdates.Select(kvp => new
        {
            Id = kvp.Key,
            Price = kvp.Value,
            UpdatedAt = DateTime.UtcNow
        });

        await connection.ExecuteAsync(sql, parameters, transaction);

        transaction.Commit();
        return true;
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
```

## Troubleshooting

### Column Name Mismatches

**Problem**: "Column 'FirstName' does not exist"

**Solution**: Ensure DapperColumnMapper is initialized:

```csharp
// In Program.cs
DapperColumnMapper.Initialize();
```

**Alternative**: Use explicit column mapping:

```csharp
public class User
{
    [Column("first_name")]
    public string FirstName { get; set; }
}
```

### Transaction Issues

**Problem**: "There is already an open DataReader associated with this Connection"

**Solution**: Use separate connections for concurrent operations:

```csharp
// INCORRECT - Reusing connection
using var connection = _dbContext.GetConnection();
var users = await connection.QueryAsync<User>("SELECT * FROM aesir_user");
foreach (var user in users)
{
    // This will fail - connection is still busy
    var roles = await connection.QueryAsync<Role>($"SELECT * FROM aesir_role WHERE user_id = {user.Id}");
}

// CORRECT - New connection for nested query
using var connection = _dbContext.GetConnection();
var users = await connection.QueryAsync<User>("SELECT * FROM aesir_user");
foreach (var user in users)
{
    using var roleConnection = _dbContext.GetConnection();
    var roles = await roleConnection.QueryAsync<Role>($"SELECT * FROM aesir_role WHERE user_id = {user.Id}");
}
```

### Migration Problems

**Problem**: "Migration not found" or "Migration already applied"

**Solution**: Check migration versioning:

```bash
# Check applied migrations
SELECT version FROM aesir_version_info ORDER BY version DESC;

# Reset specific migration
DELETE FROM aesir_version_info WHERE version = 20251025120001;
```

**Problem**: "Table 'aesir_user' already exists"

**Solution**: Implement proper Down() method:

```csharp
public override void Down()
{
    // Drop in reverse order of creation
    Delete.Index("ix_aesir_user_email").OnTable("aesir_user");
    Delete.Index("ix_aesir_user_username").OnTable("aesir_user");
    Delete.Table("aesir_user");
}
```

### Common Errors and Solutions

#### 1. Null Reference Exceptions

**Problem**: NullReferenceException when accessing navigation properties

**Solution**: Initialize collections in constructor:

```csharp
public class Order : IEntity
{
    public Order()
    {
        Items = new List<OrderItem>();  // Initialize collection
    }

    public List<OrderItem> Items { get; set; }
}
```

#### 2. Guid.Empty Issues

**Problem**: "Cannot insert NULL into column 'id'"

**Solution**: Ensure Guid is generated:

```csharp
// In repository
if (entity.Id == Guid.Empty)
{
    entity.Id = Guid.NewGuid();
}
```

#### 3. Case Sensitivity Issues

**Problem**: Queries fail on Linux due to case sensitivity

**Solution**: Use lowercase for all database identifiers:

```sql
-- CORRECT
SELECT * FROM aesir_user WHERE username = @Username;

-- INCORRECT (may fail on Linux)
SELECT * FROM Aesir_User WHERE Username = @Username;
```

#### 4. Connection Pool Exhaustion

**Problem**: "Timeout expired while obtaining a connection from the pool"

**Solution**: Always dispose connections properly:

```csharp
// Use using statements
using var connection = _dbContext.GetConnection();

// Or implement IDisposable in your service
public class ProductService : IDisposable
{
    private IDbConnection? _connection;

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
```

#### 5. Concurrent Update Conflicts

**Problem**: Lost updates when multiple users edit same record

**Solution**: Implement optimistic concurrency with version field:

```csharp
public class Product : IEntity
{
    public int Version { get; set; }  // Increment on each update
}

// In update method
const string sql = @"
    UPDATE aesir_product
    SET name = @Name,
        price = @Price,
        version = @NewVersion
    WHERE id = @Id
      AND version = @OldVersion";

var rowsAffected = await connection.ExecuteAsync(sql, new
{
    entity.Id,
    entity.Name,
    entity.Price,
    NewVersion = entity.Version + 1,
    OldVersion = entity.Version
});

if (rowsAffected == 0)
{
    throw new ConcurrentUpdateException("Record was modified by another user");
}
```

## Summary

The AESIR data access layer provides a robust foundation for database operations with:

- **Strict naming conventions** ensuring consistency
- **Repository pattern** for clean data access abstractions
- **Unit of Work** for transaction management
- **Result pattern** for explicit error handling
- **Comprehensive logging** at all layers
- **Soft deletes and audit trails** for data integrity
- **FluentMigrator** for version-controlled schema changes

By following these patterns and conventions, you ensure maintainable, testable, and performant data access throughout your AESIR modules.