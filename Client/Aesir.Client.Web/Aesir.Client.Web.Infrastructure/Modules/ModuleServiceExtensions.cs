using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Aesir.Client.Web.Infrastructure.Modules;

/// <summary>
/// Extension methods for registering client modules.
/// </summary>
public static class ModuleServiceExtensions
{
    /// <summary>
    /// Adds the module infrastructure services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddModuleInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<INavigationRegistry, NavigationRegistry>();
        return services;
    }

    /// <summary>
    /// Registers a specific module.
    /// </summary>
    /// <typeparam name="TModule">The module type to register.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddModule<TModule>(this IServiceCollection services)
        where TModule : IClientModule, new()
    {
        var module = new TModule();
        module.RegisterServices(services);
        services.AddSingleton<IClientModule>(module);
        return services;
    }

    /// <summary>
    /// Discovers and registers all modules from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for modules.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddModulesFromAssemblies(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        var moduleTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IClientModule).IsAssignableFrom(t)
                        && t is { IsInterface: false, IsAbstract: false });

        foreach (var moduleType in moduleTypes)
        {
            if (Activator.CreateInstance(moduleType) is IClientModule module)
            {
                module.RegisterServices(services);
                services.AddSingleton<IClientModule>(module);
            }
        }

        return services;
    }

    /// <summary>
    /// Initializes all registered modules' navigation.
    /// Call this after building the service provider.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The service provider for chaining.</returns>
    public static IServiceProvider InitializeModuleNavigation(this IServiceProvider provider)
    {
        var registry = provider.GetRequiredService<INavigationRegistry>();
        var modules = provider.GetServices<IClientModule>();

        foreach (var module in modules)
        {
            module.RegisterNavigation(registry);
        }

        return provider;
    }
}
