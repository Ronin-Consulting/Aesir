using System.Data;

namespace Aesir.Infrastructure.Data;

/// <summary>
/// Provides database context functionality for managing database connections.
/// </summary>
public interface IDbContext
{
    /// <summary>
    /// Gets a database connection for performing database operations.
    /// </summary>
    /// <returns>A database connection instance.</returns>
    IDbConnection GetConnection();

    /// <summary>
    /// Executes a database operation within a unit of work pattern with optional transaction support.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the database operation.</typeparam>
    /// <param name="actionAsync">The asynchronous function to execute that performs the database operation.</param>
    /// <param name="withTransaction">Whether to execute the operation within a database transaction.</param>
    /// <returns>A task representing the asynchronous operation that returns the result of the database operation.</returns>
    Task<T> UnitOfWorkAsync<T>(Func<IDbConnection, Task<T>> actionAsync, bool withTransaction = false);

    /// <summary>
    /// Executes a database operation within a unit of work pattern with optional transaction support.
    /// </summary>
    /// <param name="actionAsync">The asynchronous function to execute that performs the database operation.</param>
    /// <param name="withTransaction">Whether to execute the operation within a database transaction.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnitOfWorkAsync(Func<IDbConnection, Task> actionAsync, bool withTransaction = false);
}
