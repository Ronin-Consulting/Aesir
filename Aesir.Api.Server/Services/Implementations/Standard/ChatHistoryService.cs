using Aesir.Api.Server.Data;
using Aesir.Api.Server.Models;
using Aesir.Common.Models;
using Dapper;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Implements chat history management functionality, providing methods to interact with chat session data.
/// </summary>
/// <param name="logger">The logger instance for logging service activities.</param>
/// <param name="dbContext">The database context used for accessing and managing data.</param>
public class ChatHistoryService(ILogger<ChatHistoryService> logger, IDbContext dbContext) : IChatHistoryService
{
    /// <summary>
    /// Handles the management of chat history by providing operations such as
    /// upserting, retrieving, deleting, and searching chat sessions in a database.
    /// </summary>
    /// <remarks>
    /// The service uses a database as the backend for storing and retrieving
    /// chat-related data such as chat sessions and messages. It leverages
    /// custom type handling for JSON data types using Dapper.
    /// </remarks>
    static ChatHistoryService()
    {
        SqlMapper.AddTypeHandler(new JsonTypeHandler<AesirConversation>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<AesirChatMessage>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<AesirChatMessage>());
    }

    /// <summary>
    /// The logger instance used for logging information, warnings, errors, and other diagnostic messages
    /// related to the operations and lifecycle of the ChatHistoryService.
    /// </summary>
    private readonly ILogger<ChatHistoryService> _logger = logger;

    /// <summary>
    /// Inserts or updates a chat session in the database. If a session with the same ID exists, it is updated;
    /// otherwise, a new session is created.
    /// </summary>
    /// <param name="chatSession">The chat session object to insert or update.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

        await dbContext.UnitOfWorkAsync(async connection =>
        {
            await connection.ExecuteAsync(sql, chatSession);
        }, withTransaction: true);
    }

    /// <summary>
    /// Retrieves a specific chat session from the database based on its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the chat session to retrieve.</param>
    /// <returns>The chat session matching the specified identifier, or null if no session is found.</returns>
    public async Task<AesirChatSession?> GetChatSessionAsync(Guid id)
    {
        const string sql = @"
            SELECT id, user_id as UserId, updated_at as UpdatedAt, conversation::jsonb as Conversation, title as Title
            FROM aesir.aesir_chat_session
            WHERE id = @Id::uuid
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryFirstOrDefaultAsync<AesirChatSession>(sql, new { Id = id }));
    }

    /// <summary>
    /// Asynchronously retrieves chat sessions for a specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user for whom the chat sessions are being retrieved.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a collection of
    /// <see cref="Aesir.Api.Server.Models.AesirChatSession"/> objects corresponding to the user's chat sessions.
    /// </returns>
    public async Task<IEnumerable<AesirChatSession>> GetChatSessionsAsync(string userId)
    {
        const string sql = @"
            SELECT id, user_id as UserId, updated_at as UpdatedAt, conversation::jsonb as Conversation, title as Title
            FROM aesir.aesir_chat_session
            WHERE user_id = @UserId
            ORDER BY updated_at DESC
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirChatSession>(sql, new { UserId = userId }));
    }

    /// <summary>
    /// Retrieves a collection of chat sessions for a given user within a specified date range.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose chat sessions are to be retrieved.</param>
    /// <param name="from">The starting date and time of the range for which chat sessions should be retrieved.</param>
    /// <param name="to">The ending date and time of the range for which chat sessions should be retrieved.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an enumerable collection of chat sessions.
    /// </returns>
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

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirChatSession>(sql, new { UserId = userId, From = from, To = to }));
    }

    /// <summary>
    /// Deletes a chat session from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the chat session to be deleted.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeleteChatSessionAsync(Guid id)
    {
        const string sql = @"
            DELETE FROM aesir.aesir_chat_session
            WHERE id = @Id::uuid
        ";

        await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, new { Id = id }), withTransaction: true);
    }

    /// <summary>
    /// Searches chat sessions for a specific user based on a given search term.
    /// Filters results by matching the search term within the session's title or messages content.
    /// </summary>
    /// <param name="searchTerm">The search term to filter chat sessions.</param>
    /// <param name="userId">The unique identifier of the user whose chat sessions are being searched.</param>
    /// <returns>A collection of chat sessions that match the search criteria, or an empty collection if no matches are found.</returns>
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

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirChatSession>(sql, new { userId, searchQuery }));
    }
}
