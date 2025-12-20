using Aesir.Client.Web.Infrastructure.Modules;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Chat.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aesir.Client.Web.Modules.Chat;

/// <summary>
/// Chat module for the AESIR client.
/// Provides chat functionality including conversation UI and history management.
/// </summary>
public class ChatModule : ClientModuleBase
{
    /// <inheritdoc />
    public override string Name => "Chat";

    /// <inheritdoc />
    public override string Version => "1.0.0";

    /// <inheritdoc />
    public override string Description => "Chat functionality with AI assistants.";

    /// <inheritdoc />
    public override void RegisterServices(IServiceCollection services)
    {
        // Register chat state service as singleton to persist across navigation
        services.AddSingleton<IChatStateService, ChatStateService>();

        // Register chat history service as scoped (depends on scoped IChatApiService)
        services.AddScoped<IChatHistoryService, ChatHistoryService>();

        // Note: IMarkdownService is now registered in Infrastructure (Program.cs) for shared access

        // Register citation link parser for parsing file:// citation URLs
        services.AddSingleton<ICitationLinkParser, CitationLinkParser>();

        // Register citation state service for managing citation viewer state
        services.AddSingleton<ICitationStateService, CitationStateService>();

        // Register tool call state service for managing tool call display during streaming
        services.AddScoped<IToolCallStateService, ToolCallStateService>();

        // Note: IAgentToolsService is now registered in Infrastructure (Program.cs) for shared access

        // Register documents view service for the Documents panel
        services.AddScoped<IDocumentsViewService, DocumentsViewService>();

        // Note: IDocumentApiService is registered in Program.cs with a typed HttpClient
        // that has the proper base URL configured (same as IApiClient)
    }

    /// <inheritdoc />
    public override void RegisterNavigation(INavigationRegistry registry)
    {
        // Chat page is the main entry point - uses ChatLayout
        registry.Register(new NavigationItem
        {
            Title = "Chat",
            Href = "/chat",
            Icon = "Chat",
            Priority = 10,
            Group = "Main"
        });
    }
}
