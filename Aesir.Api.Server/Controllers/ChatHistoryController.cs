using System.Text.Json;
using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Api.Server.Controllers
{
    [ApiController]
    [Route("chat/history")]
    [Produces("application/json")]
    public class ChatHistoryController : ControllerBase
    {
        private readonly ILogger<ChatHistoryController> _logger;
        private readonly IChatHistoryService _chatHistoryService;

        public ChatHistoryController(ILogger<ChatHistoryController> logger, IChatHistoryService chatHistoryService)
        {
            _logger = logger;
            _chatHistoryService = chatHistoryService;
        }

        [HttpGet("user/{userId}")]
        public async Task<IEnumerable<AesirChatSessionItem>> GetChatSessionsAsync([FromRoute] string userId)
        {
            var results = (await _chatHistoryService.GetChatSessionsAsync(userId))
                .Select(
                    chatSession => new AesirChatSessionItem()
                    {
                        Id = chatSession.Id,
                        Title = chatSession.Title,
                        UpdatedAt = chatSession.UpdatedAt,
                    }
                ).ToList();

            _logger.LogDebug("Found {Count} chat sessions for user {UserId}", results.Count, userId);
            _logger.LogDebug("Results = {Results}", JsonSerializer.Serialize(results));

            return results;
        }

        [HttpGet("user/{userId}/search/{searchTerm:required}")]
        public async Task<IEnumerable<AesirChatSessionItem>> SearchChatSessionsAsync(
            [FromRoute] string userId, [FromRoute] string searchTerm)
        {
            var results = (await _chatHistoryService.SearchChatSessionsAsync(searchTerm, userId))
                .Select(
                    chatSession => new AesirChatSessionItem()
                    {
                        Id = chatSession.Id,
                        Title = chatSession.Title,
                        UpdatedAt = chatSession.UpdatedAt,
                    }
                ).ToList();

            _logger.LogDebug("Found {Count} chat sessions for user {UserId} and search term {SearchTerm}", results.Count, userId, searchTerm);
            _logger.LogDebug("Results = {Results}", JsonSerializer.Serialize(results));

            return results;
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(AesirChatSession), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetChatSessionAsync([FromRoute] Guid id)
        {
            var found = await _chatHistoryService.GetChatSessionAsync(id);

            if (found is null)
            {
                return NoContent();
            }

            return Ok(found);
        }

        [HttpPut("{id:guid}/{title:required}")]
        public async Task<IActionResult> GetChatSessionAsync([FromRoute] Guid id, [FromRoute] string title)
        {
            var found = await _chatHistoryService.GetChatSessionAsync(id);

            if (found is null)
            {
                return NoContent();
            }

            found.Title = title;

            await _chatHistoryService.UpsertChatSessionAsync(found);

            return Ok();
        }

        [HttpDelete("{id:guid}")]
        public async Task DeleteChatSessionAsync([FromRoute] Guid id)
        {
            await _chatHistoryService.DeleteChatSessionAsync(id);
        }
    }
}