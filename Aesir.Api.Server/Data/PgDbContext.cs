using System.Data;
using Npgsql;

namespace Aesir.Api.Server.Data;

/// <summary>
/// Represents a database context for PostgreSQL connections. Implements the <see cref="IDbContext"/> interface
/// and provides functionality to establish and manage database connections.
/// </summary>
public class PgDbContext(string connectionString) : IDbContext
{
    /// <summary>
    /// Establishes and returns a new database connection using the configured connection string.
    /// </summary>
    /// <returns>A new instance of an <see cref="IDbConnection"/> for interacting with the database.</returns>
    public IDbConnection GetConnection()
    {
        return new NpgsqlConnection(connectionString);
    }
}