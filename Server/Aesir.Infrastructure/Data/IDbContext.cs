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
}

/// <summary>
/// Provides extension methods for IDbContext to support unit of work patterns with optional transaction management.
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Executes a database operation within a unit of work pattern with optional transaction support.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the database operation.</typeparam>
    /// <param name="dbContext">The database context.</param>
    /// <param name="actionAsync">The asynchronous function to execute that performs the database operation.</param>
    /// <param name="withTransaction">Whether to execute the operation within a database transaction.</param>
    /// <returns>A task representing the asynchronous operation that returns the result of the database operation.</returns>
    public static async Task<T> UnitOfWorkAsync<T>(this IDbContext dbContext,
        Func<IDbConnection, Task<T>> actionAsync, bool withTransaction = false)
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
    /// Executes a database operation within a unit of work pattern with optional transaction support.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="actionAsync">The asynchronous function to execute that performs the database operation.</param>
    /// <param name="withTransaction">Whether to execute the operation within a database transaction.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task UnitOfWorkAsync(this IDbContext dbContext,
        Func<IDbConnection, Task> actionAsync, bool withTransaction = false)
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
