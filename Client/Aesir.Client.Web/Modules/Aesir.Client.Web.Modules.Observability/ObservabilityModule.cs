using Aesir.Client.Web.Infrastructure.Modules;
using Aesir.Client.Web.Modules.Observability.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aesir.Client.Web.Modules.Observability;

/// <summary>
/// Observability module for the AESIR client.
/// Provides log viewing and analysis functionality for AI operations.
/// </summary>
public class ObservabilityModule : ClientModuleBase
{
    /// <inheritdoc />
    public override string Name => "Observability";

    /// <inheritdoc />
    public override string Version => "1.0.0";

    /// <inheritdoc />
    public override string Description => "AI operation log viewing and analysis.";

    /// <inheritdoc />
    public override void RegisterServices(IServiceCollection services)
    {
        // Register observability service as scoped (depends on scoped IApiClient)
        services.AddScoped<IObservabilityService, ObservabilityService>();
    }

    /// <inheritdoc />
    public override void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register(new NavigationItem
        {
            Title = "Observability",
            Href = "/settings/observability",
            Icon = "Timeline",
            Priority = 60,
            Group = "Settings"
        });
    }
}
