namespace Aesir.Infrastructure.Data;

/// <summary>
/// Defines the contract for repository operations on entities.
/// Provides basic CRUD operations following the repository pattern with Dapper.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements IEntity.</typeparam>
public interface IRepository<TEntity> where TEntity : class, IEntity
{
    /// <summary>
    /// Gets an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The entity if found, null otherwise.</returns>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities of this type from the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A collection of all entities.</returns>
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity to the database.
    /// Automatically generates a new Guid if the entity Id is empty.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The added entity with its Id populated.</returns>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity in the database.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity from the database.
    /// Can be overridden in derived repositories to implement soft delete.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if the removal was successful, false otherwise.</returns>
    Task<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity by its unique identifier.
    /// Can be overridden in derived repositories to implement soft delete.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if the removal was successful, false otherwise.</returns>
    Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
