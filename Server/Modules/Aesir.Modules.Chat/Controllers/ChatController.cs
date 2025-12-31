using System.Runtime.CompilerServices;
using Aesir.Modules.Chat.Models;
using Aesir.Common.Models;
using Aesir.Infrastructure.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Inference.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("Aesir.Modules.Chat.Tests")]

namespace Aesir.Modules.Chat.Controllers
{
    /// <summary>
    /// Controller for managing agent-based chat completion requests and responses.
    /// All chat completions require an agent context for proper inference engine resolution.
    /// </summary>
    [ApiController]
    [Route("chat/completions")]
    [Produces("application/json")]
    public class ChatController(
        IServiceProvider serviceProvider,
        IConfigurationService configurationService,
        ILogger<ChatController> logger) : ControllerBase
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

            // Apply inference engine master switch for thinking
            var (effectiveEnableThinking, effectiveThinkValue) = await ApplyInferenceEngineMasterSwitchAsync(
                agent, request.EnableThinking, request.ThinkValue);

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
                EnableThinking = effectiveEnableThinking,
                MaxTokens = agent.ChatMaxTokens ?? 32768,
                Model = agent.ChatModel!,
                Temperature = agent.ChatTemperature ?? 0.1,
                Title = request.Title,
                TopP = agent.ChatTopP ?? 0.1,
                User = request.User,
                Tools = filteredTools,
                ThinkValue = effectiveThinkValue,
                ChatPromptPersona = agent.ChatPromptPersona,
                ChatCustomPromptContent = agent.ChatCustomPromptContent
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

            // Apply inference engine master switch for thinking
            var (effectiveEnableThinking, effectiveThinkValue) = await ApplyInferenceEngineMasterSwitchAsync(
                agent, request.EnableThinking, request.ThinkValue);

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
                EnableThinking = effectiveEnableThinking,
                MaxTokens = agent.ChatMaxTokens ?? 32768,
                Model = agent.ChatModel!,
                Temperature = agent.ChatTemperature ?? 0.1,
                Title = request.Title,
                TopP = agent.ChatTopP ?? 0.1,
                User = request.User,
                Tools = filteredTools,
                ThinkValue = effectiveThinkValue,
                ChatPromptPersona = agent.ChatPromptPersona,
                ChatCustomPromptContent = agent.ChatCustomPromptContent
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

        /// <summary>
        /// Applies the inference engine's master thinking switch.
        /// If the engine has EnableChatModelThinking set to false, thinking is disabled regardless of request settings.
        /// This only applies to Ollama inference engines.
        /// </summary>
        /// <param name="agent">The agent containing the inference engine reference.</param>
        /// <param name="requestEnableThinking">The thinking setting from the client request.</param>
        /// <param name="requestThinkValue">The think value from the client request.</param>
        /// <returns>A tuple with the effective EnableThinking and ThinkValue after applying master switch.</returns>
        internal async Task<(bool? enableThinking, ThinkValue? thinkValue)> ApplyInferenceEngineMasterSwitchAsync(
            AesirAgent agent,
            bool? requestEnableThinking,
            ThinkValue? requestThinkValue)
        {
            logger.LogDebug("[Master Switch] Input: RequestEnableThinking={RequestEnableThinking}, RequestThinkValue={RequestThinkValue}",
                requestEnableThinking, requestThinkValue);

            // Only check if thinking is requested
            if (!(requestEnableThinking ?? false))
            {
                logger.LogDebug("[Master Switch] Thinking not requested by client, passing through unchanged");
                return (requestEnableThinking, requestThinkValue);
            }

            // Fetch the inference engine configuration
            var inferenceEngine = await configurationService.GetInferenceEngineAsync(agent.ChatInferenceEngineId!.Value);

            // Only apply master switch for Ollama engines
            if (inferenceEngine.Type != InferenceEngineType.Ollama)
            {
                logger.LogDebug("[Master Switch] Non-Ollama engine ({EngineType}), passing through unchanged", inferenceEngine.Type);
                return (requestEnableThinking, requestThinkValue);
            }

            // Check the master switch setting
            string? thinkingValue = null;
            var hasConfig = inferenceEngine.Configuration?.TryGetValue("EnableChatModelThinking", out thinkingValue) == true;
            var engineEnableThinking = hasConfig && bool.TryParse(thinkingValue, out var thinking) && thinking;

            logger.LogDebug("[Master Switch] Ollama engine config: EnableChatModelThinking={ConfigValue}, Parsed={EngineEnableThinking}",
                thinkingValue ?? "(not set)", engineEnableThinking);

            // If master switch is OFF, disable thinking
            if (!engineEnableThinking)
            {
                logger.LogDebug("[Master Switch] Master switch is OFF - forcing thinking disabled");
                return (false, null);
            }

            // Master switch is ON, allow request settings through
            logger.LogDebug("[Master Switch] Master switch is ON - allowing request settings through");
            return (requestEnableThinking, requestThinkValue);
        }
    }
}
