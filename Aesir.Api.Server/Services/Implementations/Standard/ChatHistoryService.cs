using Aesir.Api.Server.Data;
using Aesir.Api.Server.Models;
using Dapper;

namespace Aesir.Api.Server.Services.Implementations.Standard;

public class ChatHistoryService : IChatHistoryService
{
    static ChatHistoryService()
    {
        SqlMapper.AddTypeHandler(new JsonTypeHandler<AesirConversation>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<AesirChatMessage>());
    }

    private readonly ILogger<ChatHistoryService> _logger;
    private readonly IDbContext _dbContext;

    public ChatHistoryService(ILogger<ChatHistoryService> logger, IDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task UpsertChatSessionAsync(AesirChatSession chatSession)
    {
        const string sql = @"
            INSERT INTO aesir.aesir_chat_session (id, user_id, updated_at, conversation, title)
            VALUES (@Id, @UserId, @UpdatedAt, @Conversation::jsonb, @Title)
            ON CONFLICT (id) DO UPDATE SET
                user_id = @UserId,
                updated_at = @UpdatedAt,
                conversation = @Conversation::jsonb,
                title = @Title      
        ";

        await _dbContext.UnitOfWorkAsync(async connection =>
        {
            await connection.ExecuteAsync(sql, chatSession);
        }, withTransaction: true);
    }

    public async Task<AesirChatSession?> GetChatSessionAsync(Guid id)
    {
        const string sql = @"
            SELECT id, user_id as UserId, updated_at as UpdatedAt, conversation::jsonb as Conversation, title as Title
            FROM aesir.aesir_chat_session
            WHERE id = @Id::uuid
        ";

        return await _dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryFirstOrDefaultAsync<AesirChatSession>(sql, new { Id = id }));
    }

    public async Task<IEnumerable<AesirChatSession>> GetChatSessionsAsync(string userId)
    {
        const string sql = @"
            SELECT id, user_id as UserId, updated_at as UpdatedAt, conversation::jsonb as Conversation, title as Title
            FROM aesir.aesir_chat_session
            WHERE user_id = @UserId
            ORDER BY updated_at DESC
        ";

        return await _dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirChatSession>(sql, new { UserId = userId }));
    }

    public async Task<IEnumerable<AesirChatSession>> GetChatSessionsAsync(string userId, DateTimeOffset from, DateTimeOffset to)
    {
        const string sql = @"
            SELECT id, user_id as UserId, updated_at as UpdatedAt, conversation::jsonb as Conversation, title as Title
            FROM aesir.aesir_chat_session
            WHERE user_id = @UserId
            AND updated_at >= @From
            AND updated_at <= @To
            ORDER BY updated_at DESC
        ";

        return await _dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirChatSession>(sql, new { UserId = userId, From = from, To = to }));
    }

    public async Task DeleteChatSessionAsync(Guid id)
    {
        const string sql = @"
            DELETE FROM aesir.aesir_chat_session
            WHERE id = @Id::uuid
        ";

        await _dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, new { Id = id }), withTransaction: true);
    }

    public async Task<IEnumerable<AesirChatSession>> SearchChatSessionsAsync(string searchTerm, string userId)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Array.Empty<AesirChatSession>();

        var normalizedSearchTerm = searchTerm.Trim().Replace("'", "''");

        const string sql = @"
        SELECT id, user_id as UserId, updated_at as UpdatedAt, conversation::jsonb as Conversation, title as Title
        FROM aesir.aesir_chat_session
        WHERE user_id = @userId AND (
            -- Search in title
            to_tsvector('english', title) @@ to_tsquery('english', @searchQuery)
            OR
            -- Search in conversation Messages content
            to_tsvector('english', jsonb_path_query_array(conversation, '$.Messages[*] ? (@.Role != ""system"").Content')::text) @@ to_tsquery('english', @searchQuery)
        )
        ORDER BY updated_at DESC";

        var searchQuery = string.Join(" & ", normalizedSearchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return await _dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirChatSession>(sql, new { userId, searchQuery }));
    }
}
