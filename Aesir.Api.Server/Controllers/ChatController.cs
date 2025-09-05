using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services;
using Aesir.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Api.Server.Controllers
{
    /// <summary>
    /// Controller for managing chat completion requests and responses.
    /// </summary>
    [ApiController]
    [Route("chat/completions")]
    [Produces("application/json")]
    public class ChatController(IChatService chatService) : ControllerBase
    {
        /// <summary>
        /// Handles a chat completion request and returns the result asynchronously.
        /// </summary>
        /// <param name="request">The chat completion request containing conversation details and model parameters.</param>
        /// <returns>A task representing the asynchronous operation that returns the chat completion result.</returns>
        [HttpPost]
        public Task<AesirChatResult> ChatCompletionsAsync([FromBody] AesirChatRequest request)
        {
            return chatService.ChatCompletionsAsync(request);
        }

        /// <summary>
        /// Processes a chat completion request and returns a streamed response with chunks of data.
        /// </summary>
        /// <param name="request">The chat completion request containing conversation data and model parameters.</param>
        /// <returns>An async enumerable of <see cref="AesirChatStreamedResult"/> representing streamed chat completion results.</returns>
        [HttpPost("streamed")]
        public IAsyncEnumerable<AesirChatStreamedResult> ChatCompletionsStreamedAsync([FromBody] AesirChatRequest request)
        {
            return chatService.ChatCompletionsStreamedAsync(request);
        }
    }
}
