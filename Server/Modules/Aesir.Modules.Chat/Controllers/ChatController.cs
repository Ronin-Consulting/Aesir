using Aesir.Modules.Chat.Models;
using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Inference.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Aesir.Modules.Chat.Controllers
{
    /// <summary>
    /// Controller for managing agent-based chat completion requests and responses.
    /// All chat completions require an agent context for proper inference engine resolution.
    /// </summary>
    [ApiController]
    [Route("chat/completions")]
    [Produces("application/json")]
    public class ChatController(IServiceProvider serviceProvider, IConfigurationService configurationService) : ControllerBase
    {
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

            return ConvertStreamedResultsAsync(agentChatService.ChatCompletionsStreamedAsync(chatRequest));
        }

        /// <summary>
        /// Converts base streamed results to concrete Api.Server streamed results.
        /// </summary>
        private static async IAsyncEnumerable<AesirChatStreamedResult> ConvertStreamedResultsAsync(
            IAsyncEnumerable<AesirChatStreamedResultBase> baseResults)
        {
            await foreach (var baseResult in baseResults)
            {
                yield return new AesirChatStreamedResult
                {
                    Id = baseResult.Id,
                    ChatSessionId = baseResult.ChatSessionId,
                    Title = baseResult.Title,
                    ConversationId = baseResult.ConversationId,
                    Delta = baseResult.Delta,
                    IsThinking = baseResult.IsThinking,
                    EventType = baseResult.EventType,
                    ToolCall = baseResult.ToolCall
                };
            }
        }
    }
}
