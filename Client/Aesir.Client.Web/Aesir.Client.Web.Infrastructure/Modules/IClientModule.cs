using Microsoft.Extensions.DependencyInjection;

namespace Aesir.Client.Web.Infrastructure.Modules;

/// <summary>
/// Interface for client-side modules that provide services and navigation.
/// Modules are discovered at runtime for service registration,
/// but require explicit project references for component visibility.
/// </summary>
public interface IClientModule
{
    /// <summary>
    /// Gets the unique name of the module.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of the module.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets a description of the module's functionality.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Registers the module's services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    void RegisterServices(IServiceCollection services);

    /// <summary>
    /// Registers the module's navigation items with the navigation registry.
    /// </summary>
    /// <param name="registry">The navigation registry to register items with.</param>
    void RegisterNavigation(INavigationRegistry registry);
}
