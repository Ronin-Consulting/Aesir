using System.Text.Json;
using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services;
using Aesir.Api.Server.Services.Implementations.Standard;
using Aesir.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Api.Server.Controllers
{
    /// <summary>
    /// The ChatHistoryController class provides API endpoints for managing chat history data.
    /// It allows retrieval, search, modification, and deletion of chat session information for users.
    /// </summary>
    [ApiController]
    [Route("chat/history")]
    [Produces("application/json")]
    public class ChatHistoryController(
        ILogger<ChatHistoryController> logger,
        IChatHistoryService chatHistoryService,
        IFileStorageService fileStorageService,
        IDocumentCollectionService documentCollectionService)
        : ControllerBase
    {
        /// <summary>
        /// Retrieves a collection of chat session items for a specified user.
        /// </summary>
        /// <param name="userId">The ID of the user whose chat sessions are being requested.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of <see cref="AesirChatSessionItem"/> representing the chat sessions.</returns>
        [HttpGet("user/{userId}")]
        public async Task<IEnumerable<AesirChatSessionItem>> GetChatSessionsAsync([FromRoute] string userId)
        {
            var results = (await chatHistoryService.GetChatSessionsAsync(userId))
                .Select(
                    chatSession => new AesirChatSessionItem()
                    {
                        Id = chatSession.Id,
                        Title = chatSession.Title,
                        UpdatedAt = chatSession.UpdatedAt,
                    }
                ).ToList();

            logger.LogDebug("Found {Count} chat sessions for user {UserId}", results.Count, userId);
            logger.LogDebug("Results = {Results}", JsonSerializer.Serialize(results));

            return results;
        }

        /// <summary>
        /// Searches for chat sessions associated with the specified user ID that match the given search term.
        /// Returns a collection of lightweight chat session items containing basic metadata for listing purposes.
        /// </summary>
        /// <param name="userId">The ID of the user for whom the chat sessions are being searched.</param>
        /// <param name="searchTerm">The term used to filter and search within the user's chat sessions.</param>
        /// <returns>A collection of <see cref="AesirChatSessionItem"/> objects representing the search results.</returns>
        [HttpGet("user/{userId}/search/{searchTerm:required}")]
        public async Task<IEnumerable<AesirChatSessionItem>> SearchChatSessionsAsync(
            [FromRoute] string userId, [FromRoute] string searchTerm)
        {
            var results = (await chatHistoryService.SearchChatSessionsAsync(searchTerm, userId))
                .Select(
                    chatSession => new AesirChatSessionItem()
                    {
                        Id = chatSession.Id,
                        Title = chatSession.Title,
                        UpdatedAt = chatSession.UpdatedAt,
                    }
                ).ToList();

            logger.LogDebug("Found {Count} chat sessions for user {UserId} and search term {SearchTerm}", results.Count, userId, searchTerm);
            logger.LogDebug("Results = {Results}", JsonSerializer.Serialize(results));

            return results;
        }

        /// <summary>
        /// Retrieves a single chat session by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the chat session.</param>
        /// <returns>An <see cref="IActionResult"/> representing the HTTP response.
        /// If the chat session is found, the response contains the session's details with a 200 OK status.
        /// If the chat session is not found, the response contains a 204 No Content status.</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(AesirChatSession), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetChatSessionAsync([FromRoute] Guid id)
        {
            var found = await chatHistoryService.GetChatSessionAsync(id);

            if (found is null)
            {
                return NoContent();
            }

            return Ok(found);
        }

        /// <summary>
        /// Updates the title of a specified chat session if it exists.
        /// </summary>
        /// <param name="id">The unique identifier of the chat session to update.</param>
        /// <param name="title">The new title to assign to the chat session.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> indicating whether the operation was successful.
        /// Returns NoContent if the chat session is not found; returns Ok if the update is successful.
        /// </returns>
        [HttpPut("{id:guid}/{title:required}")]
        public async Task<IActionResult> GetChatSessionAsync([FromRoute] Guid id, [FromRoute] string title)
        {
            var found = await chatHistoryService.GetChatSessionAsync(id);

            if (found is null)
            {
                return NoContent();
            }

            found.Title = title;

            await chatHistoryService.UpsertChatSessionAsync(found);

            return Ok();
        }

        /// <summary>
        /// Deletes a chat session and its associated data, including conversation documents and files.
        /// </summary>
        /// <param name="id">The unique identifier of the chat session to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [HttpDelete("{id:guid}")]
        public async Task DeleteChatSessionAsync([FromRoute] Guid id)
        {
            var found = await chatHistoryService.GetChatSessionAsync(id);
            
            var conversationId = found!.Conversation.Id;
            var conversationArgs = ConversationDocumentCollectionArgs.Default;
            conversationArgs.SetConversationId(conversationId);

            await documentCollectionService.DeleteDocumentsAsync(conversationArgs);
            await fileStorageService.DeleteFilesByFolderAsync(conversationId);

            await chatHistoryService.DeleteChatSessionAsync(id);
        }
    }
}