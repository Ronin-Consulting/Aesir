using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Models;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Service interface for Chat API operations.
/// Provides typed methods for chat completions and history management.
/// </summary>
public interface IChatApiService
{
    /// <summary>
    /// Sends a chat message to an agent and receives a streamed response.
    /// </summary>
    /// <param name="request">The chat request with agent ID and conversation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async enumerable of streamed chat results.</returns>
    IAsyncEnumerable<AesirChatStreamedResult> StreamChatAsync(
        AesirAgentChatRequestBase request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets chat sessions for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<IReadOnlyList<AesirChatSessionItem>>> GetChatSessionsAsync(
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a chat session by ID.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<AesirChatSession?>> GetChatSessionAsync(
        Guid sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Updates the title of a chat session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="title">The new title.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult> UpdateChatSessionTitleAsync(
        Guid sessionId,
        string title,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a chat session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult> DeleteChatSessionAsync(
        Guid sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Searches chat sessions for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<IReadOnlyList<AesirChatSessionItem>>> SearchChatSessionsAsync(
        string userId,
        string searchTerm,
        CancellationToken ct = default);
}

/// <summary>
/// Concrete implementation of AesirChatSessionBase for client-side use.
/// </summary>
public class AesirChatSession : AesirChatSessionBase
{
}
