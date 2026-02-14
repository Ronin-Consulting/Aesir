using Aesir.Common.Models;
using Aesir.Common.Prompts;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Research.Agents;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Builds chat requests for research agents with consistent configuration
/// for thinking settings, tools, and model defaults.
/// </summary>
public class ChatRequestBuilder : IChatRequestBuilder
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ChatRequestBuilder> _logger;

    /// <summary>
    /// Default model to use when agent doesn't specify one.
    /// </summary>
    private const string DefaultModel = "gpt-4";

    /// <summary>
    /// Default max tokens when agent doesn't specify.
    /// </summary>
    private const int DefaultMaxTokens = 8192;

    public ChatRequestBuilder(
        IConfigurationService configurationService,
        ILogger<ChatRequestBuilder> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AesirChatRequestBase> BuildAsync(
        ResearchAgent agent,
        string systemPrompt,
        string userPrompt,
        ChatRequestOptions options)
    {
        // Build conversation messages list
        var messages = new List<AesirChatMessage>();

        // 1. Add system message first (sets the agent persona/role)
        var systemMessage = new AesirChatMessage
        {
            Role = "system",
            Content = systemPrompt,
            CreatedAt = DateTimeOffset.UtcNow
        };
        messages.Add(systemMessage);

        // 2. Add prior conversation history if provided (context from previous chat)
        // This comes AFTER system but BEFORE the research task prompt
        // Note: The chat service's summarization reducer will handle long histories automatically
        if (options.PriorConversationHistory?.Count > 0)
        {
            var historyToInclude = FilterSystemMessages(options.PriorConversationHistory);
            if (historyToInclude.Count > 0)
            {
                messages.AddRange(historyToInclude);
                _logger.LogDebug(
                    "Including {Count} prior conversation messages for agent {Role}",
                    historyToInclude.Count, agent.Role);
            }
        }

        // 3. Add user message (the research prompt/task)
        var userMessage = new AesirChatMessage
        {
            Role = "user",
            Content = userPrompt,
            CreatedAt = DateTimeOffset.UtcNow
        };
        messages.Add(userMessage);

        var conversation = new AesirConversation
        {
            Id = Guid.NewGuid().ToString(),
            Messages = messages
        };

        // Get thinking settings and optionally tools from base agent
        bool? enableThinking = null;
        ThinkValue? thinkValue = null;
        var tools = new List<ToolRequest>();

        try
        {
            var baseAgent = await _configurationService
                .GetAgentAsync(agent.BaseAgentId)
                .ConfigureAwait(false);

            enableThinking = baseAgent.AllowThinking;
            thinkValue = baseAgent.ThinkValue;

            // Load tools if requested
            if (options.IncludeTools)
            {
                tools = await LoadToolsForAgentAsync(agent.BaseAgentId).ConfigureAwait(false);
                _logger.LogDebug(
                    "Loaded {ToolCount} tools for agent {Role}, thinking={EnableThinking}",
                    tools.Count, agent.Role, enableThinking);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load configuration for agent {Role}", agent.Role);
        }

        // Build the request
        // Research agent requests should not create chat history entries
        var request = new AesirChatRequestBase
        {
            Model = agent.Model ?? DefaultModel,
            Temperature = options.TemperatureOverride ?? agent.Temperature,
            MaxTokens = options.MaxTokensOverride ?? agent.MaxTokens ?? DefaultMaxTokens,
            Conversation = conversation,
            User = options.User,
            Title = options.Title,
            Tools = tools,
            EnableThinking = enableThinking,
            ThinkValue = thinkValue,
            ShouldPersistChatSession = false
        };

        // Apply custom persona if requested
        if (options.UseCustomPersona)
        {
            request.ChatPromptPersona = PromptPersona.Custom;
            request.ChatCustomPromptContent = options.CustomPersonaContent ?? agent.Persona;
        }

        return request;
    }

    /// <summary>
    /// Loads tools configured for an agent from the configuration service.
    /// </summary>
    private async Task<List<ToolRequest>> LoadToolsForAgentAsync(Guid baseAgentId)
    {
        var tools = new List<ToolRequest>();

        var agentTools = await _configurationService
            .GetToolsUsedByAgentAsync(baseAgentId)
            .ConfigureAwait(false);

        var mcpServers = await _configurationService
            .GetMcpServersAsync()
            .ConfigureAwait(false);

        var mcpServerList = mcpServers.ToList();

        foreach (var tool in agentTools)
        {
            string? mcpServerName = null;

            // Check if this is an MCP server tool
            if (tool.McpServerId.HasValue)
            {
                var mcpServer = mcpServerList.FirstOrDefault(m => m.Id == tool.McpServerId);
                if (mcpServer != null)
                {
                    mcpServerName = mcpServer.Name;
                }
            }

            tools.Add(new ToolRequest
            {
                ToolName = tool.ToolName ?? "",
                McpServerName = mcpServerName
            });
        }

        return tools;
    }

    /// <summary>
    /// Filters out system messages from the conversation history.
    /// The chat service's summarization reducer handles length limits.
    /// </summary>
    /// <param name="history">The full conversation history.</param>
    /// <returns>The filtered history without system messages.</returns>
    private static List<AesirChatMessage> FilterSystemMessages(IReadOnlyList<AesirChatMessage> history)
    {
        return history
            .Where(m => m.Role != "system")
            .ToList();
    }
}
