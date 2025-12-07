using Aesir.Client.Web.Infrastructure.Modules;
using Aesir.Client.Web.Modules.Wizard.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aesir.Client.Web.Modules.Wizard;

/// <summary>
/// Setup Wizard module for the AESIR client.
/// Provides first-time setup functionality for configuring AESIR.
/// </summary>
public class WizardModule : ClientModuleBase
{
    /// <inheritdoc />
    public override string Name => "Wizard";

    /// <inheritdoc />
    public override string Version => "1.0.0";

    /// <inheritdoc />
    public override string Description => "First-time setup wizard for AESIR configuration.";

    /// <inheritdoc />
    public override void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IWizardStateService, WizardStateService>();
    }

    /// <inheritdoc />
    public override void RegisterNavigation(INavigationRegistry registry)
    {
        // The wizard is accessed via redirect, not navigation
        // No navigation items registered
    }
}
