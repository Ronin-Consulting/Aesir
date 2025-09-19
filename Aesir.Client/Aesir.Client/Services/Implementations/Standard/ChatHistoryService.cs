using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Client.Models;
using Aesir.Common.Models;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

/// <summary>
/// Provides methods to manage and retrieve chat history, including retrieval of chat sessions,
/// searching within chat sessions, and modifying or deleting individual chat sessions.
/// </summary>
public class ChatHistoryService(
    ILogger<ChatHistoryService> logger,
    IConfiguration configuration,
    IFlurlClientCache flurlClientCache)
    : IChatHistoryService
{
    /// <summary>
    /// Represents an instance of an <see cref="IFlurlClient"/> used to make HTTP requests.
    /// </summary>
    /// <remarks>
    /// This client is managed via an <see cref="IFlurlClientCache"/>, which ensures efficient
    /// and reusable HTTP client handling. It is configured for use with the Chat History Service
    /// by utilizing a specific endpoint defined in the configuration settings.
    /// </remarks>
    private readonly IFlurlClient _flurlClient = flurlClientCache
        .GetOrAdd("ChatHistoryClient",
            configuration.GetValue<string>("Inference:ChatHistory"));

    /// <summary>
    /// Retrieves a collection of chat session items associated with the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose chat sessions are to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of <see cref="AesirChatSessionItem"/> objects, or null if no sessions are available.</returns>
    public async Task<IEnumerable<AesirChatSessionItem>?> GetChatSessionsAsync(string userId = "Unknown")
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment("user")
                .AppendPathSegment(userId)
                .GetJsonAsync<IEnumerable<AesirChatSessionItem>>());
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a collection of chat session items associated with a file.
    /// </summary>
    /// <param name="fileName">The name of the file whose chat sessions are to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of <see cref="AesirChatSessionItem"/> objects, or null if no sessions are available.</returns>
    public async Task<IEnumerable<AesirChatSessionItem>?> GetChatSessionsByFileAsync(string fileName = "Unknown")
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment("file")
                .AppendPathSegment(fileName)
                .GetJsonAsync<IEnumerable<AesirChatSessionItem>>());
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }

    /// <summary>
    /// Searches for chat sessions associated with a given user ID, applying a search term filter, and retrieves matching sessions.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose chat sessions are being searched.</param>
    /// <param name="searchTerm">The search term to filter chat sessions based on their content or metadata.</param>
    /// <returns>A collection of matching chat session items as <see cref="AesirChatSessionItem"/>, or null if no matches are found.</returns>
    public async Task<IEnumerable<AesirChatSessionItem>?> SearchChatSessionsAsync(string userId = "Unknown", string searchTerm = "")
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment("user")
                .AppendPathSegment(userId)
                .AppendPathSegment("search")
                .AppendPathSegment(searchTerm)
                .GetJsonAsync<IEnumerable<AesirChatSessionItem>>());
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a chat session by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the chat session.</param>
    /// <returns>A task that represents the asynchronous operation.
    /// The task result contains the chat session object, or null if not found.</returns>
    public async Task<AesirChatSession?> GetChatSessionAsync(Guid id)
    {
        try
        {
            return await _flurlClient.Request()
                .AppendPathSegment(id)
                .GetJsonAsync<AesirChatSession>();
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }

    /// <summary>
    /// Updates the title of an existing chat session.
    /// </summary>
    /// <param name="id">The unique identifier of the chat session to be updated.</param>
    /// <param name="title">The new title for the chat session.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpdateChatSessionTitleAsync(Guid id, string title)
    {
        try
        {
            await _flurlClient.Request()
                .AppendPathSegment(id)
                .AppendPathSegment(title)
                .PutAsync();
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }

    /// <summary>
    /// Deletes a chat session with the specified unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the chat session to delete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task DeleteChatSessionAsync(Guid id)
    {
        try
        {
            await _flurlClient.Request()
                .AppendPathSegment(id)
                .DeleteAsync();
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }
}