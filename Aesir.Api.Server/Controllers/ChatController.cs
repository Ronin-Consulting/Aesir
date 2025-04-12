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
        private readonly IPdfDataLoader _pdfDataLoader;

        public ChatController(IChatService chatService, IPdfDataLoader pdfDataLoader)
        {
            _chatService = chatService;
            _pdfDataLoader = pdfDataLoader;
        }
        
        [HttpPost]
        public Task<AesirChatResult>  ChatCompletionsAsync([FromBody]AesirChatRequest request)
        {
            return _chatService.ChatCompletionsAsync(request);
        }
        
        [HttpPost("streamed")]
        public IAsyncEnumerable<AesirChatStreamedResult> ChatCompletionsStreamedAsync([FromBody]AesirChatRequest request)
        {
            return _chatService.ChatCompletionsStreamedAsync(request);
        }
        
        [HttpGet("load/test/data")]
        public async Task<IActionResult>  LoadTestDataAsync()
        {
            await _pdfDataLoader.LoadPdf("Assets/MissionPlan-OU812.pdf",2, 100, CancellationToken.None);
            
            return Ok();
        }
    }
}
