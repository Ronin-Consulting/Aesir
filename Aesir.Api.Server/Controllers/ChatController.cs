using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Api.Server.Controllers
{
    [ApiController]
    [Route("chat/completions")]
    [Produces("application/json")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        
        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public Task<AesirChatResult> ChatCompletionsAsync([FromBody] AesirChatRequest request)
        {
            return _chatService.ChatCompletionsAsync(request);
        }

        [HttpPost("streamed")]
        public IAsyncEnumerable<AesirChatStreamedResult> ChatCompletionsStreamedAsync([FromBody] AesirChatRequest request)
        {
            return _chatService.ChatCompletionsStreamedAsync(request);
        }
    }
}
