using System.Data;
using Npgsql;

namespace Aesir.Api.Server.Data;

public class PgDbContext(string connectionString) : IDbContext
{
    public IDbConnection GetConnection()
    {
        return new NpgsqlConnection(connectionString);
    }
}