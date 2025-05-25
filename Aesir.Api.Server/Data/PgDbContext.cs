using System.Data;
using Npgsql;

namespace Aesir.Api.Server.Data;

public class PgDbContext : IDbContext
{
    private readonly string _connectionString;

    public PgDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection GetConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}