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
    public class ChatController(IServiceProvider serviceProvider, IConfigurationService configurationService) : ControllerBase
    {   
        /// <summary>
        /// Handles a chat completion request and returns the result asynchronously.
        /// </summary>
        /// <param name="request">The chat completion request containing conversation details and model parameters.</param>
        /// <returns>A task representing the asynchronous operation that returns the chat completion result.</returns>
        [HttpPost]
        public async Task<AesirChatResult> ChatCompletionsAsync([FromBody] AesirChatRequest request)
        {
            //return await chatService.ChatCompletionsAsync(request);
            
            // either remove this method and all the calls to it (currently on test code) or support this by
            // getting first inference engine, or by including an inference engine in request object
            throw new InvalidOperationException("Currently unsupported without an agent context");
        }

        /// <summary>
        /// Processes a chat completion request and returns a streamed response with chunks of data.
        /// </summary>
        /// <param name="request">The chat completion request containing conversation data and model parameters.</param>
        /// <returns>An async enumerable of <see cref="AesirChatStreamedResult"/> representing streamed chat completion results.</returns>
        [HttpPost("streamed")]
        public IAsyncEnumerable<AesirChatStreamedResult> ChatCompletionsStreamedAsync([FromBody] AesirChatRequest request)
        {
            //return chatService.ChatCompletionsStreamedAsync(request);
            
            // either remove this method and all the calls to it (currently on test code) or support this by
            // getting first inference engine, or by including an inference engine in request object
            throw new InvalidOperationException("Currently unsupported without an agent context");
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
            var tools = await configurationService.GetToolsUsedByAgentAsync(request.AgentId.Value);
            var mcpServers = await configurationService.GetMcpServersAsync();
            
            var filteredTools = request.Tools.Where(tr => 
                tools.Any(t => 
                    t.Name == tr.ToolName &&
                    (!tr.IsMcpServerToolRequest || 
                     (mcpServers.Any(mcp => mcp.Id == t.McpServerId && mcp.Name == tr.McpServerName)))
                )).ToList();
            
            var chatRequest = new AesirChatRequest()
            {
                ChatSessionId = request.ChatSessionId,
                ChatSessionUpdatedAt = request.ChatSessionUpdatedAt,
                ClientDateTime = request.ClientDateTime,
                Conversation = request.Conversation,
                EnableThinking = request.EnableThinking,
                MaxTokens = agent.ChatMaxTokens ?? 32768,
                Model = agent.ChatModel!,
                Temperature = agent.ChatTemperature ?? 0.1,
                Title = request.Title,
                TopP = agent.ChatTopP ?? 0.1,
                User = request.User,
                Tools = filteredTools,
                ThinkValue = request.ThinkValue
            };

            // Resolve the correct ChatService based on the agent's inference engine
            var agentChatService = serviceProvider.GetKeyedService<IChatService>(agent.ChatInferenceEngineId.ToString());
            if (agentChatService == null)
            {
                throw new InvalidOperationException($"No agent chat service found for inference engine ID: {agent.ChatInferenceEngineId}");
            }
            
            return await agentChatService.ChatCompletionsAsync(chatRequest);
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
            var tools = await configurationService.GetToolsUsedByAgentAsync(request.AgentId.Value);
            var mcpServers = await configurationService.GetMcpServersAsync();
            
            var filteredTools = request.Tools.Where(tr => 
                tools.Any(t => 
                    t.ToolName == tr.ToolName &&
                    (!tr.IsMcpServerToolRequest || 
                     (mcpServers.Any(mcp => mcp.Id == t.McpServerId && mcp.Name == tr.McpServerName)))
                )).ToList();
            
            var chatRequest = new AesirChatRequest()
            {
                ChatSessionId = request.ChatSessionId,
                ChatSessionUpdatedAt = request.ChatSessionUpdatedAt,
                ClientDateTime = request.ClientDateTime,
                Conversation = request.Conversation,
                EnableThinking = request.EnableThinking,
                MaxTokens = agent.ChatMaxTokens ?? 32768,
                Model = agent.ChatModel!,
                Temperature = agent.ChatTemperature ?? 0.1,
                Title = request.Title,
                TopP = agent.ChatTopP ?? 0.1,
                User = request.User,
                Tools = filteredTools,
                ThinkValue = request.ThinkValue
            };

            // Resolve the correct ChatService based on the agent's inference engine
            var agentChatService = serviceProvider.GetKeyedService<IChatService>(agent.ChatInferenceEngineId.ToString());
            if (agentChatService == null)
            {
                throw new InvalidOperationException($"No agent chat service found for inference engine ID: {agent.ChatInferenceEngineId}");
            }
            
            return agentChatService.ChatCompletionsStreamedAsync(chatRequest);
        }
    }
}
