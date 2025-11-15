using Aesir.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Aesir.Infrastructure.Modules;

/// <summary>
/// Represents a contract for a module within the AESIR framework.
/// Modules serve as encapsulated units of functionality, adhering to defined configuration
/// and dependency injection patterns, and allowing for runtime discovery and integration.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Gets the name of the module.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of the module.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets a brief description of the module.
    /// This can provide additional context or metadata about the module's functionality.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets or sets the configuration instance for the module,
    /// providing access to application settings and configuration values.
    /// </summary>
    IConfiguration? Configuration { get; set; }

    /// <summary>
    /// Provides an abstraction for managing configuration-related operations
    /// within a module, enabling interaction with general settings, inference
    /// engines, agents, tools, and server configurations.
    /// </summary>
    IConfigurationService? ConfigurationService { get; set; }

    /// <summary>
    /// Gets or sets the service that manages and reports on the readiness status of essential configuration during system initialization.
    /// </summary>
    IConfigurationReadinessService? ConfigurationReadinessService { get; set; }

    /// <summary>
    /// Gets or sets the kernel builder used to configure and customize the kernel's behavior.
    /// This property provides a way to define and modify the kernel's execution pipeline,
    /// allowing modules to adapt the kernel to meet specific requirements.
    /// </summary>
    public IKernelBuilder? KernelBuilder { get; set; }

    /// <summary>
    /// Registers services required by this module into the dependency injection container.
    /// This is called during application startup before the service provider is built.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <returns>A task that represents the asynchronous operation of registering services.</returns>
    Task RegisterServicesAsync(IServiceCollection services);

    /// <summary>
    /// Initializes the module after the application pipeline has been configured.
    /// This method is invoked after the service provider has been built and the application pipeline is ready.
    /// It provides an opportunity to perform startup tasks, configure application-level features, or execute
    /// other initialization logic specific to the module.
    /// </summary>
    /// <param name="app">The application builder instance used to access services and configure the module.</param>
    void Initialize(IApplicationBuilder app);
}
