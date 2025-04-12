using System.Data;

namespace Aesir.Api.Server.Data;

public interface IDbContext
{
    IDbConnection GetConnection();
}

public static class DbContextExtensions
{
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