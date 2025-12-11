using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Aesir.Modules.Inference.Services;

public abstract class BasePromptExecutionSettingsBuilder<TPromptExecutionSettings>(
    Kernel kernel,
    IConversationDocumentCollectionService? conversationDocumentCollectionService,
    IKernelPluginService kernelPluginService,
    ILogger logger)
    where TPromptExecutionSettings : PromptExecutionSettings
{
    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly Kernel Kernel = kernel;
    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly IConversationDocumentCollectionService? ConversationDocumentCollectionService = conversationDocumentCollectionService;

    protected readonly IKernelPluginService KernelPluginService = kernelPluginService;

    protected readonly ILogger Logger = logger;

    public async Task<PromptExecutionSettingsResult<TPromptExecutionSettings>> BuildAsync(AesirChatRequestBase request)
    {
        Logger.LogWarning("[PromptSettings] BuildAsync called for request with {ToolCount} tools", request.Tools?.Count ?? 0);
        Logger.LogWarning("[PromptSettings] Conversation has {MessageCount} messages", request.Conversation?.Messages?.Count ?? 0);

        var systemPromptVariables = new Dictionary<string, object>
        {
            ["currentDateTime"] = request.ClientDateTime,
            ["webSearchtoolsEnabled"] = false,
            ["docSearchToolsEnabled"] = false
        };

        var settings = CreatePromptExecutionSettings(request);

        if(request.EnableThinking ?? false)
            ConfigureForThinking(settings, request);

        await ConfigureBuiltInTools(settings, request, systemPromptVariables);
        await ConfigureExternalToolsAsync(settings, request, systemPromptVariables);

        return new PromptExecutionSettingsResult<TPromptExecutionSettings>()
        {
            Settings = settings,
            SystemPromptVariables = systemPromptVariables
        };
    }

    private async Task ConfigureBuiltInTools(TPromptExecutionSettings settings, AesirChatRequestBase request, Dictionary<string, object> systemPromptVariables)
    {
        var kernelPluginArgs = ConversationDocumentCollectionArgs.Default;

        var enableWebSearch = request.Tools.Any(t => t.IsWebSearchToolRequest);
        var hasRagTool = request.Tools.Any(t => t.IsRagToolRequest);
        var hasFileInMessages = request.Conversation.Messages.Any(m => m.HasFile());
        var enableDocumentSearch = hasRagTool && hasFileInMessages;
        var enableMcpTools = request.Tools.Any(t => t.IsMcpServerToolRequest);

        Logger.LogWarning("[PromptSettings] hasRagTool={HasRagTool}, hasFileInMessages={HasFileInMessages}", hasRagTool, hasFileInMessages);
        foreach (var msg in request.Conversation.Messages)
        {
            Logger.LogWarning("[PromptSettings]   Message Role={Role}, HasFile={HasFile}, FileName={FileName}", msg.Role, msg.HasFile(), msg.GetFileName() ?? "null");
        }
        foreach (var tool in request.Tools)
        {
            Logger.LogWarning("[PromptSettings]   Tool: IsRag={IsRag}, IsWeb={IsWeb}, IsMcp={IsMcp}", tool.IsRagToolRequest, tool.IsWebSearchToolRequest, tool.IsMcpServerToolRequest);
        }

        kernelPluginArgs["PluginName"] = "ChatTools";

        systemPromptVariables["webSearchtoolsEnabled"] = enableWebSearch;
        kernelPluginArgs.SetEnableWebSearch(enableWebSearch);

        if (enableDocumentSearch)
        {
            systemPromptVariables["docSearchToolsEnabled"] = true;
            kernelPluginArgs.SetEnableDocumentSearch(true);
        }

        if (enableWebSearch || enableDocumentSearch)
        {
            var conversationId = request.Conversation.Id;
            kernelPluginArgs.SetConversationId(conversationId);
        }

        if (enableMcpTools)
        {
            var mcpTools = request.Tools
                .Where(t => t.IsMcpServerToolRequest)
                .Select(at => new ConversationDocumentCollectionArgs.McpServerToolArg(at.McpServerName!, at.ToolName))
                .ToArray();
            kernelPluginArgs.SetMcpTools(mcpTools);
        }

        var plugin = await KernelPluginService.GetKernelPluginAsync(kernelPluginArgs);

        // Remove the existing plugin if it exists to avoid conflicts with conversations
        if (Kernel.Plugins.TryGetPlugin(plugin.Name, out var existingPlugin))
            Kernel.Plugins.Remove(existingPlugin);

        Kernel.Plugins.Add(plugin);

        // Log plugin configuration for debugging
        Logger.LogWarning("[PromptSettings] enableWebSearch={EnableWebSearch}, enableDocumentSearch={EnableDocumentSearch}, enableMcpTools={EnableMcpTools}", enableWebSearch, enableDocumentSearch, enableMcpTools);
        Logger.LogWarning("[PromptSettings] Plugin '{PluginName}' added with {FunctionCount} functions", plugin.Name, plugin.Count());
        foreach (var func in plugin)
        {
            Logger.LogWarning("[PromptSettings]   - Function: {FunctionName}", func.Name);
        }

        if (enableWebSearch || enableDocumentSearch || enableMcpTools)
        {
            settings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();
            Logger.LogWarning("[PromptSettings] FunctionChoiceBehavior set to Auto");
        }
        else
        {
            Logger.LogWarning("[PromptSettings] No tools enabled - FunctionChoiceBehavior NOT set");
        }
    }

    protected virtual void ConfigureForThinking(TPromptExecutionSettings settings, AesirChatRequestBase request)
    {
        // default is no op
    }

    protected virtual Task ConfigureExternalToolsAsync(TPromptExecutionSettings settings, AesirChatRequestBase request, Dictionary<string, object> systemPromptVariables)
    {
        // default is no op
        return Task.CompletedTask;
    }

    protected abstract TPromptExecutionSettings CreatePromptExecutionSettings(AesirChatRequestBase request);
}

public class PromptExecutionSettingsResult<TPromptExecutionSettings>
    where TPromptExecutionSettings : PromptExecutionSettings
{
    public required TPromptExecutionSettings Settings { get; set; }

    public required Dictionary<string, object> SystemPromptVariables { get; set; }
}
