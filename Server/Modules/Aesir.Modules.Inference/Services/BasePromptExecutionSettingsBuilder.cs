using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Aesir.Modules.Inference.Services;

public abstract class BasePromptExecutionSettingsBuilder<TPromptExecutionSettings>(
    Kernel kernel,
    IConversationDocumentCollectionService? conversationDocumentCollectionService,
    IGlobalDocumentCollectionService? globalDocumentCollectionService,
    IKernelPluginService kernelPluginService,
    ILogger logger)
    where TPromptExecutionSettings : PromptExecutionSettings
{
    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly Kernel Kernel = kernel;
    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly IConversationDocumentCollectionService? ConversationDocumentCollectionService = conversationDocumentCollectionService;
    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly IGlobalDocumentCollectionService? GlobalDocumentCollectionService = globalDocumentCollectionService;

    protected readonly IKernelPluginService KernelPluginService = kernelPluginService;

    protected readonly ILogger Logger = logger;

    public async Task<PromptExecutionSettingsResult<TPromptExecutionSettings>> BuildAsync(AesirChatRequestBase request)
    {
        Logger.LogDebug("[PromptSettings] Building settings: Model={Model}, EnableThinking={EnableThinking}, ThinkValue={ThinkValue}, Tools={ToolCount}, Messages={MessageCount}",
            request.Model, request.EnableThinking, request.ThinkValue, request.Tools?.Count ?? 0, request.Conversation?.Messages?.Count ?? 0);

        var systemPromptVariables = new Dictionary<string, object>
        {
            ["currentDateTime"] = request.ClientDateTime,
            ["webSearchtoolsEnabled"] = false,
            ["docSearchToolsEnabled"] = false
        };

        var settings = CreatePromptExecutionSettings(request);

        if(request.EnableThinking ?? false)
        {
            Logger.LogDebug("[PromptSettings] Thinking enabled - configuring");
            ConfigureForThinking(settings, request);
        }
        else
        {
            Logger.LogDebug("[PromptSettings] Thinking not enabled");
        }

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

        Logger.LogDebug("[PromptSettings] hasRagTool={HasRagTool}, hasFileInMessages={HasFileInMessages}", hasRagTool, hasFileInMessages);
        foreach (var msg in request.Conversation.Messages)
        {
            Logger.LogDebug("[PromptSettings]   Message Role={Role}, HasFile={HasFile}, FileName={FileName}", msg.Role, msg.HasFile(), msg.GetFileName() ?? "null");
        }
        foreach (var tool in request.Tools)
        {
            Logger.LogDebug("[PromptSettings]   Tool: IsRag={IsRag}, IsWeb={IsWeb}, IsMcp={IsMcp}", tool.IsRagToolRequest, tool.IsWebSearchToolRequest, tool.IsMcpServerToolRequest);
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
        Logger.LogDebug("[PromptSettings] enableWebSearch={EnableWebSearch}, enableDocumentSearch={EnableDocumentSearch}, enableMcpTools={EnableMcpTools}", enableWebSearch, enableDocumentSearch, enableMcpTools);
        Logger.LogDebug("[PromptSettings] Plugin '{PluginName}' added with {FunctionCount} functions", plugin.Name, plugin.Count());
        foreach (var func in plugin)
        {
            Logger.LogDebug("[PromptSettings]   - Function: {FunctionName}", func.Name);
        }

        // Configure project document search if a project is associated with this request
        var enableProjectDocuments = false;
        if (request.ProjectId.HasValue && GlobalDocumentCollectionService != null)
        {
            enableProjectDocuments = await ConfigureProjectDocumentsAsync(request.ProjectId.Value);
        }

        if (enableWebSearch || enableDocumentSearch || enableMcpTools || enableProjectDocuments)
        {
            settings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();
            Logger.LogDebug("[PromptSettings] FunctionChoiceBehavior set to Auto");
        }
        else
        {
            Logger.LogDebug("[PromptSettings] No tools enabled - FunctionChoiceBehavior NOT set");
        }
    }

    /// <summary>
    /// Configures project document search as a kernel plugin when a project ID is provided.
    /// Uses GlobalDocumentCollectionService with the ProjectId as the CategoryId.
    /// </summary>
    /// <param name="projectId">The project identifier to use as the category for document search.</param>
    /// <returns>True if project documents were configured, false otherwise.</returns>
    private async Task<bool> ConfigureProjectDocumentsAsync(Guid projectId)
    {
        try
        {
            Logger.LogDebug("[PromptSettings] Configuring project document search for ProjectId={ProjectId}", projectId);

            // Create args for global document collection with ProjectId as CategoryId
            var projectDocArgs = new Dictionary<string, object>
            {
                ["DocumentCollectionType"] = DocumentCollectionType.Global,
                ["CategoryId"] = projectId.ToString(),
                ["PluginName"] = "ProjectDocuments",
                ["PluginDescription"] = "Search documents in the project knowledge base"
            };

            // Get project document functions from the global document collection service
            var projectDocFunctions = await GlobalDocumentCollectionService!.GetKernelPluginFunctionsAsync(projectDocArgs);

            if (projectDocFunctions.Count > 0)
            {
                // Create plugin from the functions
                var projectDocPlugin = KernelPluginFactory.CreateFromFunctions(
                    "ProjectDocuments",
                    "Search documents in the project knowledge base",
                    projectDocFunctions.ToArray());

                // Remove existing project plugin if it exists
                if (Kernel.Plugins.TryGetPlugin(projectDocPlugin.Name, out var existingProjectPlugin))
                    Kernel.Plugins.Remove(existingProjectPlugin);

                Kernel.Plugins.Add(projectDocPlugin);

                Logger.LogDebug("[PromptSettings] Project document plugin added with {FunctionCount} functions", projectDocFunctions.Count);
                foreach (var func in projectDocFunctions)
                {
                    Logger.LogDebug("[PromptSettings]   - Project Function: {FunctionName}", func.Name);
                }

                return true;
            }

            Logger.LogDebug("[PromptSettings] No project document functions available for ProjectId={ProjectId}", projectId);
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[PromptSettings] Failed to configure project documents for ProjectId={ProjectId}", projectId);
            return false;
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
