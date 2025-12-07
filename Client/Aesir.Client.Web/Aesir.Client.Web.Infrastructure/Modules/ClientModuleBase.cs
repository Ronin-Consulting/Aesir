using Microsoft.Extensions.DependencyInjection;

namespace Aesir.Client.Web.Infrastructure.Modules;

/// <summary>
/// Base class for client modules providing default implementations.
/// </summary>
public abstract class ClientModuleBase : IClientModule
{
    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string Version { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public virtual void RegisterServices(IServiceCollection services)
    {
        // Default implementation does nothing.
        // Override in derived classes to register module-specific services.
    }

    /// <inheritdoc />
    public virtual void RegisterNavigation(INavigationRegistry registry)
    {
        // Default implementation does nothing.
        // Override in derived classes to register navigation items.
    }
}
