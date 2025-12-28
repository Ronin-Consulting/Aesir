using Aesir.Client.Web.Infrastructure.Modules;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Research.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aesir.Client.Web.Modules.Research;

/// <summary>
/// Research module for the AESIR client.
/// Provides multi-agent research orchestration UI with real-time progress tracking.
/// </summary>
public class ResearchModule : ClientModuleBase
{
    /// <inheritdoc />
    public override string Name => "Research";

    /// <inheritdoc />
    public override string Version => "1.0.0";

    /// <inheritdoc />
    public override string Description => "Multi-agent research orchestration with peer review and synthesis.";

    /// <inheritdoc />
    public override void RegisterServices(IServiceCollection services)
    {
        // Register SignalR service for real-time updates (singleton for connection reuse)
        services.AddSingleton<IResearchSignalRService, ResearchSignalRService>();
    }

    /// <inheritdoc />
    public override void RegisterNavigation(INavigationRegistry registry)
    {
        // Research is accessed via the Chat page agent selector
        // No dedicated navigation item needed for now
        // Future: Could add a Research History page
    }
}
