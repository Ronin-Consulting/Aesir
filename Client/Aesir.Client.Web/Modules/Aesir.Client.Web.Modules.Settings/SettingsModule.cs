using Aesir.Client.Web.Infrastructure.Modules;
using Aesir.Client.Web.Modules.Settings.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aesir.Client.Web.Modules.Settings;

/// <summary>
/// Settings module for the AESIR client.
/// Provides administration functionality for configuring inference engines,
/// MCP servers, tools, and agents.
/// </summary>
public class SettingsModule : ClientModuleBase
{
    /// <inheritdoc />
    public override string Name => "Settings";

    /// <inheritdoc />
    public override string Version => "1.0.0";

    /// <inheritdoc />
    public override string Description => "Administration and configuration of AESIR components.";

    /// <inheritdoc />
    public override void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IInferenceEngineService, InferenceEngineService>();
        services.AddScoped<IMcpServerService, McpServerService>();
        services.AddScoped<IToolService, ToolService>();
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IGeneralSettingsService, GeneralSettingsService>();
    }

    /// <inheritdoc />
    public override void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register(new NavigationItem
        {
            Title = "Settings",
            Href = "/settings",
            Icon = "Settings",
            Priority = 100,
            Group = "Settings"
        });

        registry.Register(new NavigationItem
        {
            Title = "General",
            Href = "/settings/general",
            Icon = "Tune",
            Priority = 105,
            Group = "Settings"
        });

        registry.Register(new NavigationItem
        {
            Title = "Inference Engines",
            Href = "/settings/inference-engines",
            Icon = "Memory",
            Priority = 110,
            Group = "Settings"
        });

        registry.Register(new NavigationItem
        {
            Title = "MCP Servers",
            Href = "/settings/mcp-servers",
            Icon = "Dns",
            Priority = 120,
            Group = "Settings"
        });

        registry.Register(new NavigationItem
        {
            Title = "Tools",
            Href = "/settings/tools",
            Icon = "Build",
            Priority = 130,
            Group = "Settings"
        });

        registry.Register(new NavigationItem
        {
            Title = "Agents",
            Href = "/settings/agents",
            Icon = "SmartToy",
            Priority = 140,
            Group = "Settings"
        });
    }
}
