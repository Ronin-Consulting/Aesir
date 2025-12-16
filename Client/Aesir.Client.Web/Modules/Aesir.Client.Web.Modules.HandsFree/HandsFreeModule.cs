using Aesir.Client.Web.Infrastructure.Modules;
using Aesir.Client.Web.Modules.HandsFree.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aesir.Client.Web.Modules.HandsFree;

/// <summary>
/// HandsFree module for the AESIR client.
/// Provides push-to-talk voice interaction with AI assistants.
/// </summary>
public class HandsFreeModule : ClientModuleBase
{
    /// <inheritdoc />
    public override string Name => "HandsFree";

    /// <inheritdoc />
    public override string Version => "1.0.0";

    /// <inheritdoc />
    public override string Description => "Hands-free voice interaction with AI assistants.";

    /// <inheritdoc />
    public override void RegisterServices(IServiceCollection services)
    {
        // Register SignalR speech service for hub connections
        services.AddScoped<ISignalRSpeechService, SignalRSpeechService>();

        // Register hands-free service as scoped (manages state per user session)
        services.AddScoped<IHandsFreeService, HandsFreeService>();
    }

    /// <inheritdoc />
    public override void RegisterNavigation(INavigationRegistry registry)
    {
        // HandsFree page is accessed from the chat interface via a button
        // Not shown in main navigation - just needs a route registered
        // Note: We don't register a navigation item since it's accessed via button in chat
    }
}
