using Aesir.Api.Server.Models;
using Microsoft.SemanticKernel;

namespace Aesir.Api.Server.Services.Implementations.Standard;

public abstract class BasePromptExecutionSettingsBuilder<TPromptExecutionSettings>(
    Kernel kernel,
    IConversationDocumentCollectionService conversationDocumentCollectionService)
    where TPromptExecutionSettings : PromptExecutionSettings
{
    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly Kernel Kernel = kernel;
    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly IConversationDocumentCollectionService ConversationDocumentCollectionService = conversationDocumentCollectionService;

    public async Task<PromptExecutionSettingsResult<TPromptExecutionSettings>> BuildAsync(AesirChatRequest request)
    {
        var systemPromptVariables = new Dictionary<string, object>
        {
            ["currentDateTime"] = request.ClientDateTime,
            ["webSearchtoolsEnabled"] = false,
            ["docSearchToolsEnabled"] = false
        };
        
        var settings = CreatePromptExecutionSettings(request);
        
        if(request.EnableThinking ?? false)
            ConfigureForThinking(settings, request);
        
        ConfigureBuiltInTools(settings, request, systemPromptVariables);
        await ConfigureExternalToolsAsync(settings, request, systemPromptVariables);
        
        return new PromptExecutionSettingsResult<TPromptExecutionSettings>()
        {
            Settings = settings,
            SystemPromptVariables = systemPromptVariables
        };
    }

    private void ConfigureBuiltInTools(TPromptExecutionSettings settings, AesirChatRequest request, Dictionary<string, object> systemPromptVariables)
    {
        var kernelPluginArgs = ConversationDocumentCollectionArgs.Default;
        
        var enableWebSearch = request.Tools.Any(t => t.IsWebSearchToolRequest);
        var enableDocumentSearch = request.Conversation.Messages.Any(m => m.HasFile());
        
        systemPromptVariables["webSearchtoolsEnabled"] = enableWebSearch;
        kernelPluginArgs.SetEnableWebSearch(enableWebSearch);
        
        if (enableDocumentSearch)
        {
            systemPromptVariables["docSearchToolsEnabled"] = true;
            kernelPluginArgs.SetEnableDocumentSearch(true);
        }
        
        if (enableWebSearch || enableDocumentSearch)
        {
            settings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();

            var conversationId = request.Conversation.Id;
            
            kernelPluginArgs.SetConversationId(conversationId);

            var plugin = ConversationDocumentCollectionService.GetKernelPlugin(kernelPluginArgs);
            
            // Remove the existing plugin if it exists to avoid conflicts with conversations
            if(Kernel.Plugins.TryGetPlugin(plugin.Name, out var existingPlugin))
                Kernel.Plugins.Remove(existingPlugin);    
                
            Kernel.Plugins.Add(plugin);
        }
    }

    protected virtual void ConfigureForThinking(TPromptExecutionSettings settings, AesirChatRequest request)
    {
        // default is no op
    }
    
    protected virtual Task ConfigureExternalToolsAsync(TPromptExecutionSettings settings, AesirChatRequest request, Dictionary<string, object> systemPromptVariables)
    {
        // default is no op
        return Task.CompletedTask;
    }
    
    protected abstract TPromptExecutionSettings CreatePromptExecutionSettings(AesirChatRequest request);
}

public class PromptExecutionSettingsResult<TPromptExecutionSettings> 
    where TPromptExecutionSettings : PromptExecutionSettings
{
    public required TPromptExecutionSettings Settings { get; set; }
    
    public required Dictionary<string, object> SystemPromptVariables { get; set; }
}