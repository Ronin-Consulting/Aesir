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
    public class ChatController(IChatService chatService, IConfigurationService configurationService) : ControllerBase
    {   
        /// <summary>
        /// Handles a chat completion request and returns the result asynchronously.
        /// </summary>
        /// <param name="request">The chat completion request containing conversation details and model parameters.</param>
        /// <returns>A task representing the asynchronous operation that returns the chat completion result.</returns>
        [HttpPost]
        public async Task<AesirChatResult> ChatCompletionsAsync([FromBody] AesirChatRequest request)
        {
            return await chatService.ChatCompletionsAsync(request);
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
        
        /// <summary>
        /// Handles an agent chat completion request and returns the result asynchronously.
        /// </summary>
        /// <param name="request">The agent hat completion request containing conversation details and agent parameters.</param>
        /// <returns>A task representing the asynchronous operation that returns the agent chat completion result.</returns>
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
                MaxTokens = agent.ChatMaxTokens ?? 32768,
                Model = agent.ChatModel,
                Temperature = agent.ChatTemperature ?? 0.1,
                Title = request.Title,
                TopP = agent.ChatTopP ?? 0.1,
                User = request.User
            };
            
            // TODO we will need to look up the right inference engine and tell the
            // TODO chat completion service which one to use (or maybe lookup the right chat completion service?)
            // TODO this should match what we register in Program.cs > Main
            
            return await chatService.ChatCompletionsAsync(chatRequest);
        }

        /// <summary>
        /// Processes an agent chat completion request and returns a streamed response with chunks of data.
        /// </summary>
        /// <param name="request">The agent chat completion request containing conversation data and agent parameters.</param>
        /// <returns>An async enumerable of <see cref="AesirChatStreamedResult"/> representing streamed agent chat completion results.</returns>
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
                MaxTokens = agent.ChatMaxTokens ?? 32768,
                Model = agent.ChatModel,
                Temperature = agent.ChatTemperature ?? 0.1,
                Title = request.Title,
                TopP = agent.ChatTopP ?? 0.1,
                User = request.User
            };
            
            // TODO we will need to look up the right inference engine and tell the
            // TODO chat completion service which one to use (or maybe lookup the right chat completion service?)
            // TODO this should match what we register in Program.cs > Main
            
            return chatService.ChatCompletionsStreamedAsync(chatRequest);
        }
    }
}
