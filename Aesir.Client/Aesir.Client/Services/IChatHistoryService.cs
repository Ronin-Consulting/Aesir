using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Client.Models;
using Aesir.Common.Models;

namespace Aesir.Client.Services;

/// <summary>
/// Defines methods for managing chat history, including retrieving, searching, updating, and deleting chat sessions.
/// </summary>
public interface IChatHistoryService
{
    /// <summary>
    /// Retrieves a collection of chat session items associated with the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose chat sessions are to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of <see cref="AesirChatSessionItem"/> objects, or null if no sessions are available.</returns>
    Task<IEnumerable<AesirChatSessionItem>?> GetChatSessionsAsync(string userId = "Unknown");

    /// <summary>
    /// Searches for chat sessions associated with a specific user, filtered by a search term.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose chat sessions are being searched.</param>
    /// <param name="searchTerm">The term used to search within chat session titles or metadata.</param>
    /// <returns>A collection of matching chat session items as <see cref="AesirChatSessionItem"/>, or null if no matches are found.</returns>
    Task<IEnumerable<AesirChatSessionItem>?> SearchChatSessionsAsync(string userId = "Unknown", string searchTerm = "");

    /// <summary>
    /// Retrieves a chat session by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the chat session.</param>
    /// <returns>A task that represents the asynchronous operation.
    /// The task result contains the chat session object, or null if the session is not found.</returns>
    Task<AesirChatSession?> GetChatSessionAsync(Guid id);

    /// <summary>
    /// Updates the title of an existing chat session.
    /// </summary>
    /// <param name="id">The unique identifier of the chat session to be updated.</param>
    /// <param name="title">The new title for the chat session.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateChatSessionTitleAsync(Guid id, string title);

    /// <summary>
    /// Deletes a chat session with the specified unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the chat session to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteChatSessionAsync(Guid id);
}