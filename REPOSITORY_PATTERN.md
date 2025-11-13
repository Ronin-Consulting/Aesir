# Repository Pattern with Dapper in AESIR

## Table of Contents
- [Introduction](#introduction)
- [Architecture Overview](#architecture-overview)
- [IEntity Interface](#ientity-interface)
- [IRepository Interface](#irepository-interface)
- [Repository Base Class](#repository-base-class)
- [Creating Custom Repositories](#creating-custom-repositories)
- [Using Repositories in Services](#using-repositories-in-services)
- [Advanced Patterns](#advanced-patterns)
- [Testing Repositories](#testing-repositories)
- [Best Practices](#best-practices)
- [Common Patterns](#common-patterns)
- [Anti-Patterns to Avoid](#anti-patterns-to-avoid)
- [Performance Considerations](#performance-considerations)
- [Migration Guide](#migration-guide)

## Introduction

### What is the Repository Pattern?

The Repository pattern is a Domain-Driven Design pattern that provides an abstraction layer between your business logic and data access logic. It encapsulates the logic needed to access data sources, creating a uniform interface for accessing data regardless of the underlying data source.

In AESIR, repositories serve as the single source of truth for all database operations, providing:
- **Abstraction**: Hide Dapper and SQL implementation details from business logic
- **Testability**: Easy to mock for unit testing
- **Consistency**: Standardized data access patterns across the application
- **Maintainability**: Centralized location for data access logic
- **Type Safety**: Strongly-typed entities and compile-time checking

### Benefits in the AESIR Architecture

1. **Separation of Concerns**: Business logic remains independent of data access implementation
2. **Flexibility**: Easy to switch between different data access technologies if needed
3. **Reusability**: Common queries are written once and reused throughout the application
4. **Security**: SQL injection protection through parameterized queries
5. **Performance**: Leverages Dapper's micro-ORM performance advantages

### When to Use Repositories vs Direct Data Access

**Use Repositories When:**
- Performing CRUD operations on domain entities
- Implementing business-specific queries
- Need transactional consistency across operations
- Working with aggregate roots
- Implementing domain-driven design

**Consider Direct Data Access When:**
- Running complex reporting queries
- Performing bulk data operations
- Executing database maintenance tasks
- Working with stored procedures for legacy systems

## Architecture Overview

### Layer Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   Presentation Layer                      │
│                  (Controllers / API)                      │
└─────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│                    Application Layer                      │
│                     (Services)                           │
│                 Business Logic & Rules                   │
└─────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│                  Domain/Infrastructure                    │
│                    (Repositories)                        │
│              IRepository<T> / Repository<T>              │
└─────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│                     Data Access                          │
│                 (Dapper + PostgreSQL)                    │
│              IDbConnectionFactory / UnitOfWork           │
└─────────────────────────────────────────────────────────┘
```

### Dependency Flow

- **Controllers** depend on → **Services** (interfaces)
- **Services** depend on → **Repositories** (interfaces) and **UnitOfWork**
- **Repositories** depend on → **IDbConnectionFactory** and **ILogger**
- **No upward dependencies** - lower layers don't know about upper layers

### Interface Segregation

Each layer communicates through interfaces:
- `IUserService` - Business operations interface
- `IUserRepository` - Data access interface
- `IRepository<User>` - Generic CRUD operations
- `IUnitOfWork` - Transaction management

## IEntity Interface

The `IEntity` interface is the foundation of all domain entities in AESIR:

```csharp
namespace Aesir.Infrastructure.Data;

/// <summary>
/// Base interface for all domain entities in AESIR.
/// Ensures all entities have a consistent primary key structure.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// Uses Guid to ensure globally unique identifiers across distributed systems.
    /// </summary>
    Guid Id { get; set; }
}
```

### Purpose and Design

The `IEntity` interface establishes a contract that all domain entities must have a unique identifier. This provides:

1. **Consistency**: All entities use the same primary key structure
2. **Generic Operations**: Enables generic repository methods
3. **Type Constraints**: Allows compile-time type checking
4. **Polymorphism**: Treat all entities uniformly when needed

### Why Guid Over Auto-Increment?

AESIR uses `Guid` (UUID in PostgreSQL) for primary keys instead of auto-increment integers:

**Advantages:**
- **Distributed Systems**: Generate IDs without database roundtrips
- **Merging Data**: No conflicts when merging databases
- **Security**: IDs are not guessable or sequential
- **Client Generation**: Clients can generate IDs before sending to server
- **Microservices Ready**: Each service can generate its own IDs

**Trade-offs:**
- **Storage Size**: 16 bytes vs 4 bytes for integer
- **Index Performance**: Slightly slower than integer indexes
- **Readability**: Not human-friendly for debugging

### Entity Implementation Example

```csharp
using System.ComponentModel.DataAnnotations.Schema;
using Dapper.Contrib.Extensions;

namespace Aesir.Modules.Users.Entities;

/// <summary>
/// Represents a user in the system.
/// Implements IEntity for repository pattern compatibility.
/// </summary>
[Table("aesir_user")]  // CRITICAL: Use aesir_ prefix with snake_case
public class User : IEntity
{
    /// <summary>
    /// Unique identifier for the user.
    /// Uses ExplicitKey attribute because Guid keys are not auto-generated by database.
    /// </summary>
    [ExplicitKey]  // IMPORTANT: Use ExplicitKey for Guid, not [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// User's username for authentication.
    /// Maps to 'username' column in database.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User's email address.
    /// Maps to 'email' column in database.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Hashed password for authentication.
    /// Maps to 'password_hash' column.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// User's first name.
    /// Maps to 'first_name' column via DapperColumnMapper.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name.
    /// Maps to 'last_name' column via DapperColumnMapper.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the user account is active.
    /// Maps to 'is_active' column.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
```

## IRepository Interface

The generic repository interface defines the contract for all data access operations:

```csharp
namespace Aesir.Infrastructure.Data;

/// <summary>
/// Generic repository interface for data access operations.
/// Provides standard CRUD operations for entities.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements IEntity</typeparam>
public interface IRepository<TEntity> where TEntity : IEntity
{
    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The entity's unique identifier</param>
    /// <returns>The entity if found, null otherwise</returns>
    Task<TEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves all entities from the repository.
    /// </summary>
    /// <returns>Collection of all entities</returns>
    Task<IEnumerable<TEntity>> GetAllAsync();

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add</param>
    /// <returns>The added entity with generated Id if needed</returns>
    Task<TEntity> AddAsync(TEntity entity);

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity with updated values</param>
    /// <returns>True if update succeeded, false otherwise</returns>
    Task<bool> UpdateAsync(TEntity entity);

    /// <summary>
    /// Removes an entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to remove</param>
    /// <returns>True if removal succeeded, false otherwise</returns>
    Task<bool> RemoveAsync(TEntity entity);

    /// <summary>
    /// Removes an entity by its identifier.
    /// </summary>
    /// <param name="id">The entity's unique identifier</param>
    /// <returns>True if removal succeeded, false otherwise</returns>
    Task<bool> RemoveByIdAsync(Guid id);

    /// <summary>
    /// Checks if an entity with the given ID exists.
    /// </summary>
    /// <param name="id">The entity's unique identifier</param>
    /// <returns>True if entity exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid id);

    /// <summary>
    /// Gets the count of all entities.
    /// </summary>
    /// <returns>The total count of entities</returns>
    Task<int> CountAsync();
}
```

### Method Signatures Explained

- **GetByIdAsync**: Single entity retrieval by primary key
- **GetAllAsync**: Retrieve all entities (use with caution for large tables)
- **AddAsync**: Insert new entity, auto-generates Guid if needed
- **UpdateAsync**: Update existing entity by primary key
- **RemoveAsync**: Delete entity (physical or soft delete)
- **RemoveByIdAsync**: Delete by ID without loading entity first
- **ExistsAsync**: Efficient existence check without loading full entity
- **CountAsync**: Get total count for pagination

### Design Decisions

1. **Async-Only**: All methods are async for better scalability
2. **Task Return Types**: Consistent async/await pattern
3. **Nullable Returns**: GetByIdAsync returns nullable for not found
4. **Boolean Results**: Update/Remove return success indicators
5. **Generic Constraint**: TEntity must implement IEntity

### Extensibility

Custom repositories can extend this interface:

```csharp
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllActiveAsync();
    Task<bool> IsUsernameUniqueAsync(string username, Guid? excludeUserId = null);
}
```

## Repository Base Class

The abstract `Repository<TEntity>` class provides the foundation for all repositories:

```csharp
using System.Data;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Extensions.Logging;

namespace Aesir.Infrastructure.Data;

/// <summary>
/// Base repository implementation using Dapper for data access.
/// Provides standard CRUD operations and helper methods for custom queries.
/// </summary>
public abstract class Repository<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
{
    protected readonly IDbConnectionFactory ConnectionFactory;
    protected readonly ILogger<Repository<TEntity>> Logger;

    protected Repository(
        IDbConnectionFactory connectionFactory,
        ILogger<Repository<TEntity>> logger)
    {
        ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves an entity by its ID using Dapper.Contrib.
    /// </summary>
    public virtual async Task<TEntity?> GetByIdAsync(Guid id)
    {
        Logger.LogDebug("Getting entity {EntityType} by Id {Id}", typeof(TEntity).Name, id);

        try
        {
            using var connection = await ConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);
            var entity = await connection.GetAsync<TEntity>(id).ConfigureAwait(false);

            if (entity != null)
            {
                Logger.LogDebug("Found entity {EntityType} with Id {Id}", typeof(TEntity).Name, id);
            }
            else
            {
                Logger.LogDebug("Entity {EntityType} with Id {Id} not found", typeof(TEntity).Name, id);
            }

            return entity;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting entity {EntityType} by Id {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }

    /// <summary>
    /// Retrieves all entities using Dapper.Contrib.
    /// </summary>
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        Logger.LogDebug("Getting all entities of type {EntityType}", typeof(TEntity).Name);

        try
        {
            using var connection = await ConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);
            var entities = await connection.GetAllAsync<TEntity>().ConfigureAwait(false);
            var entityList = entities?.ToList() ?? new List<TEntity>();

            Logger.LogDebug("Retrieved {Count} entities of type {EntityType}",
                entityList.Count, typeof(TEntity).Name);

            return entityList;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting all entities of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Adds a new entity. Auto-generates Guid if Id is empty.
    /// </summary>
    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        // Generate new Guid if not provided
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
            Logger.LogDebug("Generated new Id {Id} for entity {EntityType}",
                entity.Id, typeof(TEntity).Name);
        }

        Logger.LogInformation("Adding entity {EntityType} with Id {Id}",
            typeof(TEntity).Name, entity.Id);

        try
        {
            using var connection = await ConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);

            // Dapper.Contrib's Insert method returns the number of rows affected
            var rowsAffected = await connection.InsertAsync(entity).ConfigureAwait(false);

            if (rowsAffected > 0)
            {
                Logger.LogInformation("Successfully added entity {EntityType} with Id {Id}",
                    typeof(TEntity).Name, entity.Id);
                return entity;
            }

            throw new InvalidOperationException($"Failed to add entity of type {typeof(TEntity).Name}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding entity {EntityType} with Id {Id}",
                typeof(TEntity).Name, entity.Id);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing entity using Dapper.Contrib.
    /// </summary>
    public virtual async Task<bool> UpdateAsync(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        Logger.LogInformation("Updating entity {EntityType} with Id {Id}",
            typeof(TEntity).Name, entity.Id);

        try
        {
            using var connection = await ConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);
            var success = await connection.UpdateAsync(entity).ConfigureAwait(false);

            if (success)
            {
                Logger.LogInformation("Successfully updated entity {EntityType} with Id {Id}",
                    typeof(TEntity).Name, entity.Id);
            }
            else
            {
                Logger.LogWarning("Failed to update entity {EntityType} with Id {Id} - entity may not exist",
                    typeof(TEntity).Name, entity.Id);
            }

            return success;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating entity {EntityType} with Id {Id}",
                typeof(TEntity).Name, entity.Id);
            throw;
        }
    }

    /// <summary>
    /// Removes an entity. Can be overridden for soft deletes.
    /// </summary>
    public virtual async Task<bool> RemoveAsync(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        Logger.LogInformation("Removing entity {EntityType} with Id {Id}",
            typeof(TEntity).Name, entity.Id);

        try
        {
            using var connection = await ConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);
            var success = await connection.DeleteAsync(entity).ConfigureAwait(false);

            if (success)
            {
                Logger.LogInformation("Successfully removed entity {EntityType} with Id {Id}",
                    typeof(TEntity).Name, entity.Id);
            }
            else
            {
                Logger.LogWarning("Failed to remove entity {EntityType} with Id {Id} - entity may not exist",
                    typeof(TEntity).Name, entity.Id);
            }

            return success;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error removing entity {EntityType} with Id {Id}",
                typeof(TEntity).Name, entity.Id);
            throw;
        }
    }

    /// <summary>
    /// Removes an entity by ID without loading it first.
    /// </summary>
    public virtual async Task<bool> RemoveByIdAsync(Guid id)
    {
        Logger.LogInformation("Removing entity {EntityType} by Id {Id}",
            typeof(TEntity).Name, id);

        var entity = await GetByIdAsync(id).ConfigureAwait(false);
        if (entity == null)
        {
            Logger.LogWarning("Cannot remove entity {EntityType} with Id {Id} - not found",
                typeof(TEntity).Name, id);
            return false;
        }

        return await RemoveAsync(entity).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if an entity exists without loading it.
    /// </summary>
    public virtual async Task<bool> ExistsAsync(Guid id)
    {
        Logger.LogDebug("Checking existence of entity {EntityType} with Id {Id}",
            typeof(TEntity).Name, id);

        try
        {
            var tableName = GetTableName();
            var sql = $"SELECT EXISTS(SELECT 1 FROM {tableName} WHERE id = @Id)";

            using var connection = await ConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);
            var exists = await connection.ExecuteScalarAsync<bool>(sql, new { Id = id })
                .ConfigureAwait(false);

            Logger.LogDebug("Entity {EntityType} with Id {Id} exists: {Exists}",
                typeof(TEntity).Name, id, exists);

            return exists;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking existence of entity {EntityType} with Id {Id}",
                typeof(TEntity).Name, id);
            throw;
        }
    }

    /// <summary>
    /// Gets the count of all entities.
    /// </summary>
    public virtual async Task<int> CountAsync()
    {
        Logger.LogDebug("Counting all entities of type {EntityType}", typeof(TEntity).Name);

        try
        {
            var tableName = GetTableName();
            var sql = $"SELECT COUNT(*) FROM {tableName}";

            using var connection = await ConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);
            var count = await connection.ExecuteScalarAsync<int>(sql).ConfigureAwait(false);

            Logger.LogDebug("Count of entities {EntityType}: {Count}",
                typeof(TEntity).Name, count);

            return count;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error counting entities of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    #region Protected Helper Methods

    /// <summary>
    /// Executes a query and returns multiple results.
    /// </summary>
    protected async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text)
    {
        Logger.LogDebug("Executing query: {Sql}", sql);

        using var connection = await ConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);
        return await connection.QueryAsync<T>(sql, parameters, commandType: commandType)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a query and returns a single result.
    /// </summary>
    protected async Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text)
    {
        Logger.LogDebug("Executing query for single result: {Sql}", sql);

        using var connection = await ConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);
        return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters, commandType: commandType)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a command and returns the number of affected rows.
    /// </summary>
    protected async Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text)
    {
        Logger.LogDebug("Executing command: {Sql}", sql);

        using var connection = await ConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);
        var result = await connection.ExecuteAsync(sql, parameters, commandType: commandType)
            .ConfigureAwait(false);

        Logger.LogDebug("Command executed, {RowsAffected} rows affected", result);
        return result;
    }

    /// <summary>
    /// Executes a query and returns a scalar value.
    /// </summary>
    protected async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text)
    {
        Logger.LogDebug("Executing scalar query: {Sql}", sql);

        using var connection = await ConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<T>(sql, parameters, commandType: commandType)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the table name from the entity's Table attribute.
    /// </summary>
    private string GetTableName()
    {
        var tableAttr = typeof(TEntity).GetCustomAttributes(typeof(TableAttribute), false)
            .FirstOrDefault() as TableAttribute;

        if (tableAttr == null || string.IsNullOrWhiteSpace(tableAttr.Name))
        {
            throw new InvalidOperationException(
                $"Entity {typeof(TEntity).Name} must have a [Table] attribute with a valid name.");
        }

        return tableAttr.Name;
    }

    #endregion
}
```

### Key Implementation Details

1. **Dapper.Contrib Integration**: Uses `GetAsync`, `GetAllAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`
2. **Protected Helper Methods**: Provide direct Dapper access for custom queries
3. **Comprehensive Logging**: Every operation is logged with appropriate level
4. **Error Handling**: Try-catch blocks with logging before re-throwing
5. **ConfigureAwait(false)**: Used consistently to avoid deadlocks
6. **Connection Management**: Using statements ensure proper disposal
7. **Guid Generation**: Automatic for new entities when Id is empty

## Creating Custom Repositories

### Step-by-Step Guide

1. **Create Entity Interface** (optional but recommended):
```csharp
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllActiveAsync();
    Task<bool> IsUsernameUniqueAsync(string username, Guid? excludeUserId = null);
    Task<PagedResult<User>> GetPagedAsync(int page, int pageSize, string? searchTerm = null);
}
```

2. **Implement Custom Repository**:
```csharp
using Aesir.Infrastructure.Data;
using Aesir.Modules.Users.Entities;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Users.Repositories;

/// <summary>
/// Repository for user data access operations.
/// Extends base repository with user-specific queries.
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<Repository<User>> logger)
        : base(connectionFactory, logger)
    {
    }

    /// <summary>
    /// Retrieves a user by username.
    /// </summary>
    public async Task<User?> GetByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));

        Logger.LogDebug("Getting user by username: {Username}", username);

        const string sql = @"
            SELECT * FROM aesir_user
            WHERE username = @Username
              AND is_deleted = false
            LIMIT 1";

        var user = await QueryFirstOrDefaultAsync<User>(sql, new { Username = username })
            .ConfigureAwait(false);

        if (user != null)
        {
            Logger.LogDebug("Found user with username: {Username}, Id: {Id}",
                username, user.Id);
        }
        else
        {
            Logger.LogDebug("User not found with username: {Username}", username);
        }

        return user;
    }

    /// <summary>
    /// Retrieves a user by email address.
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        Logger.LogDebug("Getting user by email: {Email}", email);

        const string sql = @"
            SELECT * FROM aesir_user
            WHERE LOWER(email) = LOWER(@Email)
              AND is_deleted = false
            LIMIT 1";

        return await QueryFirstOrDefaultAsync<User>(sql, new { Email = email })
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all active (non-deleted) users.
    /// </summary>
    public async Task<IEnumerable<User>> GetAllActiveAsync()
    {
        Logger.LogDebug("Getting all active users");

        const string sql = @"
            SELECT * FROM aesir_user
            WHERE is_deleted = false
              AND is_active = true
            ORDER BY created_at DESC";

        var users = await QueryAsync<User>(sql).ConfigureAwait(false);
        var userList = users.ToList();

        Logger.LogInformation("Retrieved {Count} active users", userList.Count);

        return userList;
    }

    /// <summary>
    /// Checks if a username is unique in the system.
    /// </summary>
    public async Task<bool> IsUsernameUniqueAsync(string username, Guid? excludeUserId = null)
    {
        Logger.LogDebug("Checking username uniqueness: {Username}", username);

        const string sql = @"
            SELECT EXISTS(
                SELECT 1 FROM aesir_user
                WHERE LOWER(username) = LOWER(@Username)
                  AND is_deleted = false
                  AND (@ExcludeId IS NULL OR id != @ExcludeId)
            )";

        var exists = await ExecuteScalarAsync<bool>(sql, new
        {
            Username = username,
            ExcludeId = excludeUserId
        }).ConfigureAwait(false);

        return !exists;  // Return true if username is unique (doesn't exist)
    }

    /// <summary>
    /// Override RemoveAsync to implement soft delete.
    /// </summary>
    public override async Task<bool> RemoveAsync(User entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        Logger.LogInformation("Soft deleting user with Id {Id}", entity.Id);

        // Update entity properties for soft delete
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        // DeletedBy should be set by the service layer with current user

        const string sql = @"
            UPDATE aesir_user
            SET is_deleted = @IsDeleted,
                deleted_at = @DeletedAt,
                deleted_by = @DeletedBy
            WHERE id = @Id";

        var rowsAffected = await ExecuteAsync(sql, new
        {
            entity.Id,
            entity.IsDeleted,
            entity.DeletedAt,
            entity.DeletedBy
        }).ConfigureAwait(false);

        if (rowsAffected > 0)
        {
            Logger.LogInformation("Successfully soft deleted user with Id {Id}", entity.Id);
            return true;
        }

        Logger.LogWarning("Failed to soft delete user with Id {Id}", entity.Id);
        return false;
    }

    /// <summary>
    /// Override GetAllAsync to exclude soft-deleted users.
    /// </summary>
    public override async Task<IEnumerable<User>> GetAllAsync()
    {
        Logger.LogDebug("Getting all non-deleted users");

        const string sql = @"
            SELECT * FROM aesir_user
            WHERE is_deleted = false
            ORDER BY created_at DESC";

        return await QueryAsync<User>(sql).ConfigureAwait(false);
    }

    /// <summary>
    /// Override GetByIdAsync to check for soft-deleted status.
    /// </summary>
    public override async Task<User?> GetByIdAsync(Guid id)
    {
        Logger.LogDebug("Getting user by Id {Id} (excluding deleted)", id);

        const string sql = @"
            SELECT * FROM aesir_user
            WHERE id = @Id
              AND is_deleted = false
            LIMIT 1";

        return await QueryFirstOrDefaultAsync<User>(sql, new { Id = id })
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets paginated users with optional search.
    /// </summary>
    public async Task<PagedResult<User>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null)
    {
        Logger.LogDebug("Getting paged users - Page: {Page}, PageSize: {PageSize}, Search: {Search}",
            page, pageSize, searchTerm);

        var offset = (page - 1) * pageSize;

        // Build WHERE clause
        var whereClause = "WHERE is_deleted = false";
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            whereClause += @"
                AND (LOWER(username) LIKE LOWER(@SearchPattern)
                  OR LOWER(email) LIKE LOWER(@SearchPattern)
                  OR LOWER(first_name) LIKE LOWER(@SearchPattern)
                  OR LOWER(last_name) LIKE LOWER(@SearchPattern))";
        }

        // Count total records
        var countSql = $@"
            SELECT COUNT(*)
            FROM aesir_user
            {whereClause}";

        // Get paginated records
        var dataSql = $@"
            SELECT *
            FROM aesir_user
            {whereClause}
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset";

        var parameters = new
        {
            SearchPattern = $"%{searchTerm}%",
            PageSize = pageSize,
            Offset = offset
        };

        // Execute both queries
        var totalCount = await ExecuteScalarAsync<int>(countSql, parameters)
            .ConfigureAwait(false);

        var users = await QueryAsync<User>(dataSql, parameters)
            .ConfigureAwait(false);

        return new PagedResult<User>
        {
            Items = users.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}

/// <summary>
/// Represents a paginated result set.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
```

### When to Create Custom Repositories

Create a custom repository when you need:
- **Domain-specific queries** beyond basic CRUD
- **Complex joins** involving multiple tables
- **Aggregations** or statistical queries
- **Soft delete** implementation
- **Caching** at the repository level
- **Bulk operations** specific to the entity
- **Performance optimizations** for specific queries

## Using Repositories in Services

### Service Implementation with Repository

```csharp
using Aesir.Infrastructure.Data;
using Aesir.Modules.Users.Entities;
using Aesir.Modules.Users.Repositories;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Users.Services;

/// <summary>
/// Service layer for user-related business operations.
/// Coordinates between controllers and repositories.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UserService> _logger;
    private readonly ICurrentUserService _currentUserService;

    public UserService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ILogger<UserService> logger,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Creates a new user with proper validation and audit fields.
    /// </summary>
    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        _logger.LogInformation("Creating new user with username: {Username}", request.Username);

        // Validate business rules
        await ValidateUsernameUniquenessAsync(request.Username);
        await ValidateEmailUniquenessAsync(request.Email);

        // Begin transaction
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            // Create user entity
            var user = new User
            {
                Id = Guid.NewGuid(),  // Generated here, not in repository
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.UserId  // Current user ID from JWT
            };

            // Save to repository
            var createdUser = await _userRepository.AddAsync(user);

            // Additional operations in same transaction
            await CreateDefaultUserSettingsAsync(createdUser.Id);
            await SendWelcomeEmailAsync(createdUser);

            // Commit transaction
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Successfully created user with Id: {UserId}", createdUser.Id);

            return MapToDto(createdUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user with username: {Username}", request.Username);
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Updates an existing user with concurrency checking.
    /// </summary>
    public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        _logger.LogInformation("Updating user with Id: {UserId}", userId);

        // Retrieve existing user
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException($"User with Id {userId} not found");
        }

        // Check for concurrent modifications
        if (request.Version != null && user.Version != request.Version)
        {
            throw new ConcurrencyException("User has been modified by another process");
        }

        // Validate business rules if username changed
        if (user.Username != request.Username)
        {
            await ValidateUsernameUniquenessAsync(request.Username, userId);
        }

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            // Update user properties
            user.Username = request.Username;
            user.Email = request.Email;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = _currentUserService.UserId;

            // Save changes
            var success = await _userRepository.UpdateAsync(user);
            if (!success)
            {
                throw new InvalidOperationException("Failed to update user");
            }

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Successfully updated user with Id: {UserId}", userId);

            return MapToDto(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user with Id: {UserId}", userId);
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Soft deletes a user.
    /// </summary>
    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        _logger.LogInformation("Deleting user with Id: {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Cannot delete user - not found with Id: {UserId}", userId);
            return false;
        }

        // Set audit fields for soft delete
        user.DeletedBy = _currentUserService.UserId;

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            // Repository handles soft delete logic
            var success = await _userRepository.RemoveAsync(user);

            if (success)
            {
                // Clean up related data
                await CleanupUserDataAsync(userId);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Successfully deleted user with Id: {UserId}", userId);
            }
            else
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogWarning("Failed to delete user with Id: {UserId}", userId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user with Id: {UserId}", userId);
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Gets paginated list of users.
    /// </summary>
    public async Task<PagedResult<UserDto>> GetUsersAsync(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null)
    {
        _logger.LogDebug("Getting users - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        var pagedUsers = await _userRepository.GetPagedAsync(page, pageSize, searchTerm);

        return new PagedResult<UserDto>
        {
            Items = pagedUsers.Items.Select(MapToDto).ToList(),
            TotalCount = pagedUsers.TotalCount,
            Page = pagedUsers.Page,
            PageSize = pagedUsers.PageSize,
            TotalPages = pagedUsers.TotalPages
        };
    }

    #region Private Helper Methods

    private async Task ValidateUsernameUniquenessAsync(string username, Guid? excludeUserId = null)
    {
        var isUnique = await _userRepository.IsUsernameUniqueAsync(username, excludeUserId);
        if (!isUnique)
        {
            _logger.LogWarning("Username already exists: {Username}", username);
            throw new ValidationException($"Username '{username}' is already taken");
        }
    }

    private async Task ValidateEmailUniquenessAsync(string email, Guid? excludeUserId = null)
    {
        // Similar implementation
    }

    private async Task CreateDefaultUserSettingsAsync(Guid userId)
    {
        // Create default settings for new user
    }

    private async Task SendWelcomeEmailAsync(User user)
    {
        // Send welcome email asynchronously
    }

    private async Task CleanupUserDataAsync(Guid userId)
    {
        // Clean up related data when user is deleted
    }

    private UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    #endregion
}
```

### Dependency Injection Configuration

```csharp
// In Program.cs or ServiceCollectionExtensions.cs

public static IServiceCollection AddDataAccess(this IServiceCollection services)
{
    // Connection factory - Singleton
    services.AddSingleton<IDbConnectionFactory, PostgreSqlConnectionFactory>();

    // Unit of Work - Scoped
    services.AddScoped<IUnitOfWork, UnitOfWork>();

    // Repositories - Scoped
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IProductRepository, ProductRepository>();
    services.AddScoped<IOrderRepository, OrderRepository>();

    // Services - Scoped
    services.AddScoped<IUserService, UserService>();
    services.AddScoped<IProductService, ProductService>();
    services.AddScoped<IOrderService, OrderService>();

    return services;
}
```

## Advanced Patterns

### Soft Delete Implementation

```csharp
/// <summary>
/// Base entity with soft delete support.
/// </summary>
public abstract class SoftDeletableEntity : IEntity
{
    [ExplicitKey]
    public Guid Id { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}

/// <summary>
/// Repository base for soft-deletable entities.
/// </summary>
public abstract class SoftDeleteRepository<TEntity> : Repository<TEntity>
    where TEntity : SoftDeletableEntity
{
    protected SoftDeleteRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<Repository<TEntity>> logger)
        : base(connectionFactory, logger)
    {
    }

    public override async Task<bool> RemoveAsync(TEntity entity)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        // DeletedBy should be set by service layer

        return await UpdateAsync(entity).ConfigureAwait(false);
    }

    public override async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        var tableName = GetTableName();
        var sql = $"SELECT * FROM {tableName} WHERE is_deleted = false";

        return await QueryAsync<TEntity>(sql).ConfigureAwait(false);
    }

    public async Task<IEnumerable<TEntity>> GetAllIncludingDeletedAsync()
    {
        return await base.GetAllAsync().ConfigureAwait(false);
    }

    public async Task<bool> RestoreAsync(Guid id)
    {
        var tableName = GetTableName();
        var sql = $@"
            UPDATE {tableName}
            SET is_deleted = false,
                deleted_at = NULL,
                deleted_by = NULL
            WHERE id = @Id";

        var rowsAffected = await ExecuteAsync(sql, new { Id = id })
            .ConfigureAwait(false);

        return rowsAffected > 0;
    }
}
```

### Audit Trail Tracking

```csharp
/// <summary>
/// Base entity with full audit support.
/// </summary>
public abstract class AuditableEntity : SoftDeletableEntity
{
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

/// <summary>
/// Repository with automatic audit trail.
/// </summary>
public abstract class AuditableRepository<TEntity> : SoftDeleteRepository<TEntity>
    where TEntity : AuditableEntity
{
    private readonly ICurrentUserService _currentUserService;

    protected AuditableRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<Repository<TEntity>> logger,
        ICurrentUserService currentUserService)
        : base(connectionFactory, logger)
    {
        _currentUserService = currentUserService;
    }

    public override async Task<TEntity> AddAsync(TEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.CreatedBy = _currentUserService.UserId;

        return await base.AddAsync(entity).ConfigureAwait(false);
    }

    public override async Task<bool> UpdateAsync(TEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUserService.UserId;

        return await base.UpdateAsync(entity).ConfigureAwait(false);
    }

    public override async Task<bool> RemoveAsync(TEntity entity)
    {
        entity.DeletedBy = _currentUserService.UserId;

        return await base.RemoveAsync(entity).ConfigureAwait(false);
    }
}
```

### Optimistic Concurrency Control

```csharp
/// <summary>
/// Entity with version tracking for optimistic concurrency.
/// </summary>
public interface IVersionedEntity : IEntity
{
    byte[] Version { get; set; }
}

/// <summary>
/// Repository with optimistic concurrency checking.
/// </summary>
public class VersionedRepository<TEntity> : Repository<TEntity>
    where TEntity : class, IEntity, IVersionedEntity
{
    public override async Task<bool> UpdateAsync(TEntity entity)
    {
        var tableName = GetTableName();
        var sql = $@"
            UPDATE {tableName}
            SET /* set all columns */,
                version = gen_random_bytes(8)
            WHERE id = @Id
              AND version = @Version";

        var rowsAffected = await ExecuteAsync(sql, entity).ConfigureAwait(false);

        if (rowsAffected == 0)
        {
            throw new DbUpdateConcurrencyException(
                "The entity has been modified by another process");
        }

        return true;
    }
}
```

### Caching Strategy

```csharp
/// <summary>
/// Repository decorator for caching.
/// </summary>
public class CachedUserRepository : IUserRepository
{
    private readonly IUserRepository _innerRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedUserRepository> _logger;

    public CachedUserRepository(
        IUserRepository innerRepository,
        IMemoryCache cache,
        ILogger<CachedUserRepository> logger)
    {
        _innerRepository = innerRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        var cacheKey = $"user_{id}";

        if (_cache.TryGetValue<User>(cacheKey, out var cached))
        {
            _logger.LogDebug("Cache hit for user {Id}", id);
            return cached;
        }

        var user = await _innerRepository.GetByIdAsync(id);

        if (user != null)
        {
            _cache.Set(cacheKey, user, TimeSpan.FromMinutes(5));
            _logger.LogDebug("Cached user {Id}", id);
        }

        return user;
    }

    public async Task<User> AddAsync(User entity)
    {
        var user = await _innerRepository.AddAsync(entity);

        // Invalidate any related caches
        InvalidateUserCaches(user);

        return user;
    }

    private void InvalidateUserCaches(User user)
    {
        _cache.Remove($"user_{user.Id}");
        _cache.Remove($"user_username_{user.Username}");
        _cache.Remove("users_all");
    }

    // Implement other methods...
}
```

### Bulk Operations

```csharp
public interface IBulkRepository<TEntity> : IRepository<TEntity>
    where TEntity : IEntity
{
    Task BulkInsertAsync(IEnumerable<TEntity> entities);
    Task BulkUpdateAsync(IEnumerable<TEntity> entities);
    Task BulkDeleteAsync(IEnumerable<Guid> ids);
}

public class BulkUserRepository : UserRepository, IBulkRepository<User>
{
    public async Task BulkInsertAsync(IEnumerable<User> entities)
    {
        const string sql = @"
            INSERT INTO aesir_user
                (id, username, email, password_hash, first_name, last_name,
                 is_active, created_at, created_by)
            VALUES
                (@Id, @Username, @Email, @PasswordHash, @FirstName, @LastName,
                 @IsActive, @CreatedAt, @CreatedBy)";

        using var connection = await ConnectionFactory.CreateConnectionAsync()
            .ConfigureAwait(false);

        using var transaction = connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(sql, entities, transaction)
                .ConfigureAwait(false);

            transaction.Commit();

            Logger.LogInformation("Bulk inserted {Count} users", entities.Count());
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // Implement other bulk methods...
}
```

## Testing Repositories

### Unit Testing with Mocks

```csharp
using Moq;
using Xunit;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<UserService>>();

        _userService = new UserService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            new PasswordHasher(),
            _loggerMock.Object,
            Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public async Task GetUserById_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com"
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _userService.GetUserByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedUser.Username, result.Username);
        Assert.Equal(expectedUser.Email, result.Email);

        _userRepositoryMock.Verify(r => r.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task CreateUser_WithValidData_CreatesAndReturnsUser()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "newuser",
            Email = "new@example.com",
            Password = "SecurePass123!"
        };

        _userRepositoryMock
            .Setup(r => r.IsUsernameUniqueAsync(request.Username, null))
            .ReturnsAsync(true);

        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _userService.CreateUserAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Username, result.Username);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }
}
```

### Integration Testing

```csharp
public class UserRepositoryIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly UserRepository _repository;

    public UserRepositoryIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new UserRepository(
            _fixture.ConnectionFactory,
            new NullLogger<Repository<User>>());
    }

    [Fact]
    public async Task AddAsync_WithNewUser_InsertsAndReturnsUser()
    {
        // Arrange
        var user = new TestDataBuilder()
            .WithUsername($"test_{Guid.NewGuid()}")
            .WithEmail($"test_{Guid.NewGuid()}@example.com")
            .Build();

        // Act
        var result = await _repository.AddAsync(user);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);

        // Verify in database
        var fromDb = await _repository.GetByIdAsync(result.Id);
        Assert.NotNull(fromDb);
        Assert.Equal(user.Username, fromDb.Username);
    }

    [Fact]
    public async Task GetByUsernameAsync_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var user = await _fixture.SeedUserAsync();

        // Act
        var result = await _repository.GetByUsernameAsync(user.Username);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task RemoveAsync_WithSoftDelete_MarksAsDeleted()
    {
        // Arrange
        var user = await _fixture.SeedUserAsync();

        // Act
        var result = await _repository.RemoveAsync(user);

        // Assert
        Assert.True(result);

        // Verify soft deleted
        var fromDb = await _repository.GetByIdAsync(user.Id);
        Assert.Null(fromDb);  // GetByIdAsync excludes deleted

        // Verify exists in database
        var exists = await _repository.ExistsAsync(user.Id);
        Assert.True(exists);
    }
}

public class DatabaseFixture : IAsyncLifetime
{
    public IDbConnectionFactory ConnectionFactory { get; private set; }
    private string _connectionString;

    public async Task InitializeAsync()
    {
        // Create test database
        _connectionString = $"Host=localhost;Database=aesir_test_{Guid.NewGuid()};Username=postgres;Password=postgres";

        // Run migrations
        var serviceProvider = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(_connectionString)
                .ScanIn(typeof(User).Assembly).For.Migrations())
            .BuildServiceProvider();

        var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();

        ConnectionFactory = new PostgreSqlConnectionFactory(_connectionString);
    }

    public async Task DisposeAsync()
    {
        // Drop test database
        // ...
    }

    public async Task<User> SeedUserAsync()
    {
        var repository = new UserRepository(
            ConnectionFactory,
            new NullLogger<Repository<User>>());

        return await repository.AddAsync(new TestDataBuilder().Build());
    }
}
```

### Test Data Builder

```csharp
public class TestDataBuilder
{
    private readonly User _user;

    public TestDataBuilder()
    {
        _user = new User
        {
            Id = Guid.NewGuid(),
            Username = $"user_{Guid.NewGuid()}",
            Email = $"user_{Guid.NewGuid()}@example.com",
            PasswordHash = "hashed_password",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public TestDataBuilder WithUsername(string username)
    {
        _user.Username = username;
        return this;
    }

    public TestDataBuilder WithEmail(string email)
    {
        _user.Email = email;
        return this;
    }

    public TestDataBuilder AsInactive()
    {
        _user.IsActive = false;
        return this;
    }

    public TestDataBuilder AsDeleted()
    {
        _user.IsDeleted = true;
        _user.DeletedAt = DateTime.UtcNow;
        return this;
    }

    public User Build() => _user;
}
```

## Best Practices

### Keep Repositories Focused

**DO:**
- Create one repository per aggregate root
- Keep repositories focused on data access only
- Use meaningful method names that describe the query

**DON'T:**
- Create repositories for value objects
- Put business logic in repositories
- Create overly generic methods

### Use Specific Methods Over Generic Queries

**Good:**
```csharp
public async Task<IEnumerable<User>> GetActiveUsersCreatedAfterAsync(DateTime date)
{
    const string sql = @"
        SELECT * FROM aesir_user
        WHERE is_active = true
          AND is_deleted = false
          AND created_at > @Date
        ORDER BY created_at DESC";

    return await QueryAsync<User>(sql, new { Date = date });
}
```

**Avoid:**
```csharp
public async Task<IEnumerable<User>> GetUsersWhere(Expression<Func<User, bool>> predicate)
{
    // Too generic, leaks implementation details
}
```

### Async/Await Consistency

**Always use async/await:**
```csharp
// Good
public async Task<User?> GetByEmailAsync(string email)
{
    return await QueryFirstOrDefaultAsync<User>(sql, new { Email = email })
        .ConfigureAwait(false);
}

// Avoid
public User? GetByEmail(string email)
{
    // Synchronous methods block threads
}
```

### Logging Conventions

```csharp
// Log method entry with parameters (Debug level)
Logger.LogDebug("Getting user by username: {Username}", username);

// Log successful operations (Information level)
Logger.LogInformation("Successfully created user with Id: {UserId}", user.Id);

// Log failures and warnings (Warning level)
Logger.LogWarning("User not found with username: {Username}", username);

// Log exceptions (Error level)
Logger.LogError(ex, "Error creating user with username: {Username}", username);
```

### Error Handling Strategies

```csharp
public async Task<User?> GetByUsernameAsync(string username)
{
    try
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));

        // Execute query
        return await QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
    }
    catch (PostgresException ex) when (ex.SqlState == "23505")
    {
        // Handle specific database errors
        Logger.LogError(ex, "Unique constraint violation for username: {Username}", username);
        throw new DuplicateUsernameException($"Username '{username}' already exists", ex);
    }
    catch (Exception ex)
    {
        // Log and re-throw unexpected errors
        Logger.LogError(ex, "Unexpected error getting user by username: {Username}", username);
        throw;
    }
}
```

## Common Patterns

### Pagination

```csharp
public async Task<PagedResult<TEntity>> GetPagedAsync(
    int page,
    int pageSize,
    string? sortBy = null,
    bool ascending = true)
{
    var tableName = GetTableName();
    var offset = (page - 1) * pageSize;
    var orderDirection = ascending ? "ASC" : "DESC";
    var orderBy = string.IsNullOrWhiteSpace(sortBy) ? "created_at" : sortBy;

    // Count query
    var countSql = $"SELECT COUNT(*) FROM {tableName} WHERE is_deleted = false";

    // Data query with pagination
    var dataSql = $@"
        SELECT * FROM {tableName}
        WHERE is_deleted = false
        ORDER BY {orderBy} {orderDirection}
        LIMIT @PageSize OFFSET @Offset";

    var totalCount = await ExecuteScalarAsync<int>(countSql);
    var items = await QueryAsync<TEntity>(dataSql, new { PageSize = pageSize, Offset = offset });

    return new PagedResult<TEntity>
    {
        Items = items.ToList(),
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
    };
}
```

### Filtering and Searching

```csharp
public async Task<IEnumerable<User>> SearchUsersAsync(UserSearchCriteria criteria)
{
    var sql = new StringBuilder("SELECT * FROM aesir_user WHERE is_deleted = false");
    var parameters = new DynamicParameters();

    // Build dynamic WHERE clause
    if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
    {
        sql.Append(@" AND (
            LOWER(username) LIKE LOWER(@SearchPattern) OR
            LOWER(email) LIKE LOWER(@SearchPattern) OR
            LOWER(first_name) LIKE LOWER(@SearchPattern) OR
            LOWER(last_name) LIKE LOWER(@SearchPattern)
        )");
        parameters.Add("SearchPattern", $"%{criteria.SearchTerm}%");
    }

    if (criteria.IsActive.HasValue)
    {
        sql.Append(" AND is_active = @IsActive");
        parameters.Add("IsActive", criteria.IsActive.Value);
    }

    if (criteria.CreatedAfter.HasValue)
    {
        sql.Append(" AND created_at > @CreatedAfter");
        parameters.Add("CreatedAfter", criteria.CreatedAfter.Value);
    }

    // Add sorting
    sql.Append($" ORDER BY {criteria.SortBy ?? "created_at"} {criteria.SortDirection ?? "DESC"}");

    return await QueryAsync<User>(sql.ToString(), parameters);
}
```

### Includes/Joins

```csharp
public async Task<User?> GetUserWithRolesAsync(Guid userId)
{
    const string sql = @"
        SELECT
            u.*,
            r.*
        FROM aesir_user u
        LEFT JOIN aesir_user_role ur ON u.id = ur.user_id
        LEFT JOIN aesir_role r ON ur.role_id = r.id
        WHERE u.id = @UserId
          AND u.is_deleted = false";

    using var connection = await ConnectionFactory.CreateConnectionAsync();

    var userDictionary = new Dictionary<Guid, User>();

    var users = await connection.QueryAsync<User, Role, User>(
        sql,
        (user, role) =>
        {
            if (!userDictionary.TryGetValue(user.Id, out var userEntry))
            {
                userEntry = user;
                userEntry.Roles = new List<Role>();
                userDictionary.Add(userEntry.Id, userEntry);
            }

            if (role != null)
                userEntry.Roles.Add(role);

            return userEntry;
        },
        new { UserId = userId },
        splitOn: "id");

    return userDictionary.Values.FirstOrDefault();
}
```

### Specification Pattern

```csharp
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T entity);
    string ToSql();
    object GetParameters();
}

public class ActiveUserSpecification : ISpecification<User>
{
    public bool IsSatisfiedBy(User entity)
    {
        return entity.IsActive && !entity.IsDeleted;
    }

    public string ToSql()
    {
        return "is_active = true AND is_deleted = false";
    }

    public object GetParameters() => new { };
}

public class UsernameSpecification : ISpecification<User>
{
    private readonly string _username;

    public UsernameSpecification(string username)
    {
        _username = username;
    }

    public bool IsSatisfiedBy(User entity)
    {
        return entity.Username.Equals(_username, StringComparison.OrdinalIgnoreCase);
    }

    public string ToSql()
    {
        return "LOWER(username) = LOWER(@Username)";
    }

    public object GetParameters() => new { Username = _username };
}

// Repository method using specifications
public async Task<IEnumerable<User>> FindAsync(ISpecification<User> specification)
{
    var tableName = GetTableName();
    var sql = $"SELECT * FROM {tableName} WHERE {specification.ToSql()}";

    return await QueryAsync<User>(sql, specification.GetParameters());
}
```

## Anti-Patterns to Avoid

### 1. Generic Repository with No Custom Methods

**Avoid:**
```csharp
// Provides no value over direct Dapper usage
public class UserRepository : Repository<User>
{
    // No custom methods - why does this exist?
}
```

**Better:**
```csharp
public class UserRepository : Repository<User>, IUserRepository
{
    // Add domain-specific queries
    public async Task<User?> GetByUsernameAsync(string username) { }
    public async Task<IEnumerable<User>> GetActiveUsersAsync() { }
    // etc.
}
```

### 2. Business Logic in Repositories

**Avoid:**
```csharp
public class UserRepository : Repository<User>
{
    public async Task<User> CreateUserWithValidationAsync(CreateUserRequest request)
    {
        // Business logic doesn't belong here!
        if (request.Password.Length < 8)
            throw new ValidationException("Password too short");

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Sending emails from repository!
        await _emailService.SendWelcomeEmailAsync(request.Email);

        // This belongs in a service layer
    }
}
```

### 3. Direct Connection Usage in Services

**Avoid:**
```csharp
public class UserService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public async Task<User> GetUserAsync(Guid id)
    {
        // Services shouldn't know about database connections
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.GetAsync<User>(id);
    }
}
```

### 4. Synchronous Methods

**Avoid:**
```csharp
public User GetById(Guid id)
{
    // Blocks thread, doesn't scale
    return GetByIdAsync(id).Result;
}
```

### 5. Leaking Entities to Presentation Layer

**Avoid:**
```csharp
// Controller directly returning entity
[HttpGet("{id}")]
public async Task<User> GetUser(Guid id)
{
    // Exposes internal structure, couples layers
    return await _userRepository.GetByIdAsync(id);
}
```

**Better:**
```csharp
[HttpGet("{id}")]
public async Task<UserDto> GetUser(Guid id)
{
    var user = await _userService.GetUserByIdAsync(id);
    return _mapper.Map<UserDto>(user);
}
```

## Performance Considerations

### Connection Management

**Efficient:**
```csharp
// Connection is opened, used, and disposed immediately
public async Task<User?> GetByIdAsync(Guid id)
{
    using var connection = await ConnectionFactory.CreateConnectionAsync();
    return await connection.GetAsync<User>(id);
}
```

**Inefficient:**
```csharp
// Don't hold connections longer than necessary
public class BadRepository
{
    private readonly IDbConnection _connection; // Don't do this!

    public BadRepository()
    {
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open(); // Connection held for object lifetime
    }
}
```

### Query Optimization

```csharp
// Use specific columns when possible
const string sql = @"
    SELECT id, username, email, is_active
    FROM aesir_user
    WHERE is_deleted = false";

// Use proper indexes
[Migration(20250120000001)]
public class AddUserIndexes : Migration
{
    public override void Up()
    {
        Create.Index("ix_aesir_user_username")
            .OnTable("aesir_user")
            .OnColumn("username")
            .Unique()
            .WithOptions().Filter("is_deleted = false");

        Create.Index("ix_aesir_user_email")
            .OnTable("aesir_user")
            .OnColumn("email")
            .WithOptions().Filter("is_deleted = false");
    }
}
```

### Dapper's Performance Benefits

1. **Minimal Overhead**: Close to raw ADO.NET performance
2. **Efficient Mapping**: Fast object materialization
3. **Cached Queries**: SQL parsing cached after first execution
4. **Buffered Results**: Control over result buffering

```csharp
// Non-buffered for large result sets
public async Task<IEnumerable<User>> GetLargeResultSetAsync()
{
    const string sql = "SELECT * FROM aesir_user WHERE is_deleted = false";

    using var connection = await ConnectionFactory.CreateConnectionAsync();

    // buffered: false prevents loading all results into memory at once
    return await connection.QueryAsync<User>(sql, buffered: false);
}
```

### When to Use Micro-ORMs vs Full ORMs

**Use Dapper (Micro-ORM) When:**
- Performance is critical
- You need fine control over SQL
- Working with complex queries or stored procedures
- Team has strong SQL skills
- Read-heavy applications

**Consider EF Core (Full ORM) When:**
- Rapid prototyping needed
- Complex object graphs with many relationships
- Need automatic change tracking
- LINQ queries preferred over SQL
- Less SQL expertise on team

## Migration Guide

### Converting from Entity Framework to Dapper

#### Before (Entity Framework):
```csharp
public class EFUserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);
    }

    public async Task<User> AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }
}
```

#### After (Dapper):
```csharp
public class DapperUserRepository : Repository<User>, IUserRepository
{
    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = @"
            SELECT * FROM aesir_user
            WHERE username = @Username
              AND is_deleted = false";

        return await QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
    }

    public override async Task<User> AddAsync(User user)
    {
        if (user.Id == Guid.Empty)
            user.Id = Guid.NewGuid();

        using var connection = await ConnectionFactory.CreateConnectionAsync();
        await connection.InsertAsync(user);
        return user;
    }
}
```

### Common Migration Gotchas

1. **No Automatic Change Tracking**
   - Must explicitly call Update methods
   - No lazy loading - load related data explicitly

2. **Manual SQL Writing**
   - Table and column names must match exactly
   - Remember the aesir_ prefix convention

3. **No Navigation Properties**
   - Load related data with joins or multiple queries
   - Map relationships manually

4. **Transaction Handling**
   - Use IUnitOfWork pattern for transactions
   - Explicitly manage transaction boundaries

### Side-by-Side Comparison

| Feature | Entity Framework | Dapper |
|---------|-----------------|---------|
| **Performance** | Good | Excellent |
| **SQL Control** | Generated | Full control |
| **Learning Curve** | Moderate | Low (if SQL known) |
| **Change Tracking** | Automatic | Manual |
| **Migrations** | Built-in | FluentMigrator |
| **Relationships** | Navigation properties | Manual joins |
| **Caching** | First/Second level | Manual implementation |
| **LINQ Support** | Full | Limited (SQL only) |
| **Database Support** | Many providers | Any ADO.NET provider |
| **Memory Usage** | Higher | Lower |

## Conclusion

The Repository pattern with Dapper provides AESIR with a powerful, performant, and maintainable data access layer. By following these patterns and practices, you can:

- **Maintain clean separation** between business logic and data access
- **Leverage Dapper's performance** while keeping code organized
- **Ensure testability** through proper abstractions
- **Scale efficiently** with async/await patterns
- **Evolve the system** without major refactoring

Remember: repositories are about organizing data access, not hiding it. Embrace SQL, use Dapper's strengths, and keep your repositories focused and maintainable.

For more information, see:
- [DATA_ACCESS.md](./DATA_ACCESS.md) - Comprehensive data access documentation
- [MODULE_SYSTEM.md](./MODULE_SYSTEM.md) - Module architecture and organization
- [TESTING.md](./TESTING.md) - Testing strategies and patterns