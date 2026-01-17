using System.Text.Json;
using Aesir.Modules.Chat.Models;
using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Chat.Controllers
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
                        IsStarred = chatSession.IsStarred,
                        HasResearchInProgress = (chatSession as AesirChatSession)?.HasResearchInProgress ?? false,
                    }
                ).ToList();

            logger.LogDebug("Found {Count} chat sessions for user {UserId}", results.Count, userId);
            logger.LogDebug("Results = {Results}", JsonSerializer.Serialize(results));

            return results;
        }

        /// <summary>
        /// Retrieves a collection of chat session items for a specified file.
        /// </summary>
        /// <param name="fileName">The name of the file whose chat sessions are being requested.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of <see cref="AesirChatSessionItem"/> representing the chat sessions.</returns>
        [HttpGet("file/{fileName}")]
        public async Task<IEnumerable<AesirChatSessionItem>> GetChatSessionsByFileAsync([FromRoute] string fileName)
        {
            var results = (await chatHistoryService.GetChatSessionsByFilenameAsync(fileName))
                .Select(
                    chatSession => new AesirChatSessionItem()
                    {
                        Id = chatSession.Id,
                        Title = chatSession.Title,
                        UpdatedAt = chatSession.UpdatedAt,
                        IsStarred = chatSession.IsStarred,
                        HasResearchInProgress = (chatSession as AesirChatSession)?.HasResearchInProgress ?? false,
                    }
                ).ToList();

            logger.LogDebug("Found {Count} chat sessions for file {fileName}", results.Count, fileName);
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
                        IsStarred = chatSession.IsStarred,
                        HasResearchInProgress = (chatSession as AesirChatSession)?.HasResearchInProgress ?? false,
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
        /// Updates the starred status of a specified chat session.
        /// </summary>
        /// <param name="id">The unique identifier of the chat session to update.</param>
        /// <param name="isStarred">The new starred status.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> indicating whether the operation was successful.
        /// Returns Ok if the update is successful.
        /// </returns>
        [HttpPut("{id:guid}/star/{isStarred:bool}")]
        public async Task<IActionResult> UpdateStarredAsync([FromRoute] Guid id, [FromRoute] bool isStarred)
        {
            await chatHistoryService.UpdateIsStarredAsync(id, isStarred);
            logger.LogInformation("Updated starred status for session {SessionId} to {IsStarred}", id, isStarred);
            return Ok();
        }

        /// <summary>
        /// Deletes a chat session and all associated data in a two-phase process:
        /// Phase 1: Delete Qdrant vectors (non-transactional, separate database)
        /// Phase 2: Delete PostgreSQL data in single transaction (files, chat session, cascade to research)
        /// </summary>
        /// <remarks>
        /// CASCADE DELETE CHAIN (automatic via PostgreSQL FK constraints):
        /// When the chat session is deleted, the following are automatically deleted:
        ///   - aesir_research_session (via FK on chat_session_id with CASCADE)
        ///   - aesir_research_submission (via FK on session_id with CASCADE)
        ///   - aesir_research_peer_review (via FK on session_id and submission_id with CASCADE)
        ///   - aesir_research_report (via FK on session_id with CASCADE)
        ///
        /// TRANSACTION SAFETY:
        /// - If Qdrant deletion fails, we abort before any PostgreSQL changes
        /// - If PostgreSQL transaction fails, all database changes are rolled back
        /// - Orphaned Qdrant vectors (if PostgreSQL fails after Qdrant succeeds) are acceptable
        ///   and can be cleaned up asynchronously if needed
        /// </remarks>
        /// <param name="id">The unique identifier of the chat session to delete.</param>
        /// <returns>An IActionResult indicating the result of the operation.</returns>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteChatSessionAsync([FromRoute] Guid id)
        {
            // Verify chat session exists and get conversation ID for cleanup
            var chatSession = await chatHistoryService.GetChatSessionAsync(id);

            if (chatSession is null)
            {
                logger.LogWarning("Chat session {ChatSessionId} not found for deletion", id);
                return NotFound(new { message = "Chat session not found" });
            }

            var conversationId = chatSession.Conversation.Id;

            logger.LogInformation(
                "Starting deletion of chat session {ChatSessionId} with conversation {ConversationId}",
                id, conversationId);

            // ═══════════════════════════════════════════════════════════════════════════════
            // PHASE 1: Delete Qdrant vectors (non-transactional)
            // ═══════════════════════════════════════════════════════════════════════════════
            // Qdrant is a separate vector database and cannot participate in PostgreSQL
            // transactions. We delete vectors first because:
            //   1. Qdrant deletes are idempotent (safe to retry)
            //   2. If this fails, we abort before any PostgreSQL changes
            //   3. If PostgreSQL fails later, orphaned vectors are acceptable
            //      (they can be cleaned up asynchronously if needed)
            try
            {
                var conversationArgs = ConversationDocumentCollectionArgs.Default;
                conversationArgs.SetConversationId(conversationId);

                await documentCollectionService.DeleteDocumentsAsync(conversationArgs);

                logger.LogDebug(
                    "Successfully deleted Qdrant vectors for conversation {ConversationId}",
                    conversationId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to delete Qdrant vectors for conversation {ConversationId}. Aborting deletion.",
                    conversationId);

                return StatusCode(500, new
                {
                    message = "Failed to delete conversation documents from vector database",
                    conversationId
                });
            }

            // ═══════════════════════════════════════════════════════════════════════════════
            // PHASE 2: Delete PostgreSQL data (single atomic transaction)
            // ═══════════════════════════════════════════════════════════════════════════════
            // All PostgreSQL operations are wrapped in a single transaction:
            //   - File storage records (aesir_file_storage)
            //   - Chat session (aesir_chat_session)
            //   - Research sessions automatically CASCADE deleted via FK constraint
            //   - Research children (submissions, reviews, reports) CASCADE deleted via FKs
            //
            // If ANY operation fails, the transaction is rolled back and no PostgreSQL
            // data is deleted.
            try
            {
                await chatHistoryService.DeleteChatSessionWithRelatedDataAsync(id, conversationId);

                logger.LogInformation(
                    "Successfully deleted chat session {ChatSessionId} and all related data",
                    id);

                return Ok(new { message = "Chat session deleted successfully" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to delete chat session {ChatSessionId}. " +
                    "PostgreSQL transaction rolled back. " +
                    "Qdrant vectors for conversation {ConversationId} may be orphaned.",
                    id, conversationId);

                return StatusCode(500, new
                {
                    message = "Failed to delete chat session from database",
                    chatSessionId = id,
                    note = "Vector database cleanup succeeded but relational data deletion failed"
                });
            }
        }
    }
}