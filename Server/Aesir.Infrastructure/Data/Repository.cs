using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Extensions.Logging;

namespace Aesir.Infrastructure.Data;

/// <summary>
/// Base repository implementation providing CRUD operations using Dapper and Dapper.Contrib.
/// Follows AESIR conventions: Guid primary keys, snake_case columns, async/await patterns.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements IEntity.</typeparam>
public abstract class Repository<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
{
    /// <summary>
    /// Gets the database context for creating connections.
    /// </summary>
    protected IDbContext DbContext { get; }

    /// <summary>
    /// Gets the logger for diagnostic information.
    /// </summary>
    protected ILogger<Repository<TEntity>> Logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository{TEntity}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger for diagnostic information.</param>
    protected Repository(IDbContext dbContext, ILogger<Repository<TEntity>> logger)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting entity {EntityType} by Id {Id}", typeof(TEntity).Name, id);

        using var connection = DbContext.GetConnection();
        connection.Open();

        var entity = await connection.GetAsync<TEntity>(id).ConfigureAwait(false);

        if (entity == null)
        {
            Logger.LogDebug("Entity {EntityType} with Id {Id} not found", typeof(TEntity).Name, id);
        }

        return entity;
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting all entities of type {EntityType}", typeof(TEntity).Name);

        using var connection = DbContext.GetConnection();
        connection.Open();

        var entities = await connection.GetAllAsync<TEntity>().ConfigureAwait(false);

        Logger.LogDebug("Retrieved {Count} entities of type {EntityType}", entities.Count(), typeof(TEntity).Name);

        return entities;
    }

    /// <inheritdoc />
    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        // Generate new Guid if not provided
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
            Logger.LogDebug("Generated new Id {Id} for entity {EntityType}", entity.Id, typeof(TEntity).Name);
        }

        Logger.LogInformation("Adding entity {EntityType} with Id {Id}", typeof(TEntity).Name, entity.Id);

        using var connection = DbContext.GetConnection();
        connection.Open();

        // Dapper.Contrib's InsertAsync returns the Id (but we already have it as Guid)
        await connection.InsertAsync(entity).ConfigureAwait(false);

        Logger.LogInformation("Successfully added entity {EntityType} with Id {Id}", typeof(TEntity).Name, entity.Id);

        return entity;
    }

    /// <inheritdoc />
    public virtual async Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (entity.Id == Guid.Empty)
            throw new ArgumentException("Entity Id cannot be empty for update operation.", nameof(entity));

        Logger.LogInformation("Updating entity {EntityType} with Id {Id}", typeof(TEntity).Name, entity.Id);

        using var connection = DbContext.GetConnection();
        connection.Open();

        var result = await connection.UpdateAsync(entity).ConfigureAwait(false);

        if (result)
        {
            Logger.LogInformation("Successfully updated entity {EntityType} with Id {Id}", typeof(TEntity).Name, entity.Id);
        }
        else
        {
            Logger.LogWarning("Failed to update entity {EntityType} with Id {Id}", typeof(TEntity).Name, entity.Id);
        }

        return result;
    }

    /// <inheritdoc />
    public virtual async Task<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return await RemoveAsync(entity.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Entity Id cannot be empty for remove operation.", nameof(id));

        Logger.LogInformation("Removing entity {EntityType} with Id {Id}", typeof(TEntity).Name, id);

        using var connection = DbContext.GetConnection();
        connection.Open();

        // Get the entity first (required by Dapper.Contrib)
        var entity = await connection.GetAsync<TEntity>(id).ConfigureAwait(false);
        if (entity == null)
        {
            Logger.LogWarning("Entity {EntityType} with Id {Id} not found for removal", typeof(TEntity).Name, id);
            return false;
        }

        var result = await connection.DeleteAsync(entity).ConfigureAwait(false);

        if (result)
        {
            Logger.LogInformation("Successfully removed entity {EntityType} with Id {Id}", typeof(TEntity).Name, id);
        }
        else
        {
            Logger.LogWarning("Failed to remove entity {EntityType} with Id {Id}", typeof(TEntity).Name, id);
        }

        return result;
    }

    /// <summary>
    /// Executes a raw SQL query and maps the results to entities.
    /// Use this for custom queries that can't be expressed through Dapper.Contrib.
    /// </summary>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="parameters">The query parameters (use anonymous object).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A collection of entities matching the query.</returns>
    protected async Task<IEnumerable<TEntity>> QueryAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Executing custom query for {EntityType}", typeof(TEntity).Name);

        using var connection = DbContext.GetConnection();
        connection.Open();

        var results = await connection.QueryAsync<TEntity>(sql, parameters).ConfigureAwait(false);

        Logger.LogDebug("Custom query returned {Count} results for {EntityType}", results.Count(), typeof(TEntity).Name);

        return results;
    }

    /// <summary>
    /// Executes a raw SQL query and returns a single entity or null.
    /// Use this for custom queries that return a single result.
    /// </summary>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="parameters">The query parameters (use anonymous object).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The entity if found, null otherwise.</returns>
    protected async Task<TEntity?> QueryFirstOrDefaultAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Executing custom single-result query for {EntityType}", typeof(TEntity).Name);

        using var connection = DbContext.GetConnection();
        connection.Open();

        var result = await connection.QueryFirstOrDefaultAsync<TEntity>(sql, parameters).ConfigureAwait(false);

        if (result == null)
        {
            Logger.LogDebug("Custom query returned no results for {EntityType}", typeof(TEntity).Name);
        }

        return result;
    }

    /// <summary>
    /// Executes a non-query SQL command (INSERT, UPDATE, DELETE).
    /// Returns the number of rows affected.
    /// </summary>
    /// <param name="sql">The SQL command to execute.</param>
    /// <param name="parameters">The command parameters (use anonymous object).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The number of rows affected.</returns>
    protected async Task<int> ExecuteAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Executing custom command for {EntityType}", typeof(TEntity).Name);

        using var connection = DbContext.GetConnection();
        connection.Open();

        var rowsAffected = await connection.ExecuteAsync(sql, parameters).ConfigureAwait(false);

        Logger.LogDebug("Custom command affected {RowsAffected} rows for {EntityType}", rowsAffected, typeof(TEntity).Name);

        return rowsAffected;
    }
}
