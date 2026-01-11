using System.Data;
using Npgsql;

namespace Aesir.Infrastructure.Data;

/// <summary>
/// Represents a database context for PostgreSQL connections. Implements the <see cref="IDbContext"/> interface
/// and provides functionality to establish and manage database connections with connection pooling.
/// </summary>
public class PgDbContext : IDbContext
{
    private readonly NpgsqlDataSource _dataSource;

    /// <summary>
    /// Initializes a new instance of PgDbContext with connection pooling support.
    /// </summary>
    /// <param name="dataSource">The NpgsqlDataSource that manages connection pooling.</param>
    public PgDbContext(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    /// <summary>
    /// Establishes and returns a new database connection from the connection pool.
    /// </summary>
    /// <returns>A new instance of an <see cref="IDbConnection"/> for interacting with the database.</returns>
    public IDbConnection GetConnection()
    {
        return _dataSource.CreateConnection();
    }

    /// <inheritdoc />
    public async Task<T> UnitOfWorkAsync<T>(Func<IDbConnection, Task<T>> actionAsync, bool withTransaction = false)
    {
        using var connection = GetConnection();
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

    /// <inheritdoc />
    public async Task UnitOfWorkAsync(Func<IDbConnection, Task> actionAsync, bool withTransaction = false)
    {
        using var connection = GetConnection();
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
