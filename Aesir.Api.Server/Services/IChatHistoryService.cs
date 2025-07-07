using Aesir.Api.Server.Models;

namespace Aesir.Api.Server.Services;

/// <summary>
/// Provides chat history management functionality for storing and retrieving chat sessions.
/// </summary>
public interface IChatHistoryService
{
    /// <summary>
    /// Inserts a new chat session or updates an existing one.
    /// </summary>
    /// <param name="chatSession">The chat session to insert or update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpsertChatSessionAsync(AesirChatSession chatSession);

    /// <summary>
    /// Retrieves a specific chat session by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the chat session.</param>
    /// <returns>A task representing the asynchronous operation that returns the chat session or null if not found.</returns>
    Task<AesirChatSession?> GetChatSessionAsync(Guid id);

    /// <summary>
    /// Retrieves all chat sessions for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A task representing the asynchronous operation that returns a collection of chat sessions.</returns>
    Task<IEnumerable<AesirChatSession>> GetChatSessionsAsync(string userId);

    /// <summary>
    /// Retrieves chat sessions for a specific user within a date range.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="from">The start date of the range.</param>
    /// <param name="to">The end date of the range.</param>
    /// <returns>A task representing the asynchronous operation that returns a collection of chat sessions.</returns>
    Task<IEnumerable<AesirChatSession>> GetChatSessionsAsync(string userId, DateTimeOffset from, DateTimeOffset to);

    /// <summary>
    /// Searches for chat sessions containing the specified search term for a specific user.
    /// </summary>
    /// <param name="searchTerm">The term to search for.</param>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A task representing the asynchronous operation that returns a collection of matching chat sessions.</returns>
    Task<IEnumerable<AesirChatSession>> SearchChatSessionsAsync(string searchTerm, string userId);

    /// <summary>
    /// Deletes a specific chat session by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the chat session to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteChatSessionAsync(Guid id);
}
