using Aesir.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aesir.Infrastructure.Modules;

/// <summary>
/// Defines the contract for AESIR modules that can be discovered and loaded at runtime.
/// Modules provide self-contained features with their own services, controllers, and data access.
/// </summary>
public interface IModule
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
    /// Gets the description of what this module provides.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the configuration for this module.
    /// This is set by the module system after discovery.
    /// </summary>
    IConfiguration? Configuration { get; set; }

    IConfigurationService? ConfigurationService { get; set; }
    
    IConfigurationReadinessService? ConfigurationReadinessService { get; set; }
    
    /// <summary>
    /// Registers services required by this module into the dependency injection container.
    /// This is called during application startup before the service provider is built.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    Task RegisterServicesAsync(IServiceCollection services);

    /// <summary>
    /// Initializes the module after the application pipeline has been configured.
    /// This is called after the service provider is built and can be used for
    /// startup tasks, data seeding, or other initialization logic.
    /// </summary>
    /// <param name="app">The application builder for accessing services and configuration.</param>
    void Initialize(IApplicationBuilder app);
}
