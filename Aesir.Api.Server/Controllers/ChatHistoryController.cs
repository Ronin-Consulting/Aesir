using System.Text.Json;
using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Api.Server.Controllers
{
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