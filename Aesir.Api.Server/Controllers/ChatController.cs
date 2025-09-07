using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services;
using Aesir.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Api.Server.Controllers
{
    [ApiController]
    [Route("chat/completions")]
    [Produces("application/json")]
    public class ChatController(IChatService chatService, IConfigurationService configurationService) : ControllerBase
    {   
        [HttpPost]
        public async Task<AesirChatResult> ChatCompletionsAsync([FromBody] AesirChatRequest request)
        {
            return await chatService.ChatCompletionsAsync(request);
        }

        [HttpPost("streamed")]
        public IAsyncEnumerable<AesirChatStreamedResult> ChatCompletionsStreamedAsync([FromBody] AesirChatRequest request)
        {
            return chatService.ChatCompletionsStreamedAsync(request);
        }
        
        [HttpPost("agent")]
        public async Task<AesirChatResult> AgentChatCompletionsAsync([FromBody] AesirAgentChatRequestBase request)
        {
            var agent = await configurationService.GetAgentAsync(request.AgentId.Value);

            var chatRequest = new AesirChatRequest()
            {
                ChatSessionId = request.ChatSessionId,
                ChatSessionUpdatedAt = request.ChatSessionUpdatedAt,
                ClientDateTime = request.ClientDateTime,
                Conversation = request.Conversation,
                EnableThinking = true, // TODO should eventually be configuration on agent (it's an override of inference engine value)
                MaxTokens = 8192, // TODO should eventually be configuration on agent
                Model = agent.ChatModel,
                Temperature = 0.1, // TODO should eventually be configuration on agent
                Title = request.Title,
                TopP = 1.0, // TODO should eventually be configuration on agent
                User = request.User
            };
            
            // TODO we will need to look up the right inference engine and tell the
            // TODO chat completion service which one to use (or maybe lookup the right chat completion service?)
            // TODO this should match what we register in Program.cs > Main
            
            return await chatService.ChatCompletionsAsync(chatRequest);
        }

        [HttpPost("agent/streamed")]
        public async Task<IAsyncEnumerable<AesirChatStreamedResult>> AgentChatCompletionsStreamedAsync([FromBody] AesirAgentChatRequestBase request)
        {
            var agent = await configurationService.GetAgentAsync(request.AgentId.Value);
            
            var chatRequest = new AesirChatRequest()
            {
                ChatSessionId = request.ChatSessionId,
                ChatSessionUpdatedAt = request.ChatSessionUpdatedAt,
                ClientDateTime = request.ClientDateTime,
                Conversation = request.Conversation,
                EnableThinking = true,
                MaxTokens = 8192, // TODO should be configuration on agent
                Model = agent.ChatModel,
                Temperature = 0.1, // TODO should be configuration on agent
                Title = request.Title,
                TopP = 1.0, // TODO should be configuration on agent
                User = request.User
            };
            
            // TODO we will need to look up the right inference engine and tell the
            // TODO chat completion service which one to use (or maybe lookup the right chat completion service?)
            
            return chatService.ChatCompletionsStreamedAsync(chatRequest);
        }
    }
}
