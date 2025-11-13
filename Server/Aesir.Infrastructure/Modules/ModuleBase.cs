using Aesir.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Aesir.Infrastructure.Modules;

/// <summary>
/// Base class for AESIR modules that provides common functionality and default implementations.
/// Inherit from this class to create a new module.
/// </summary>
public abstract class ModuleBase : IModule
{
    /// <summary>
    /// Logger for module operations.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Initializes a new instance of the ModuleBase class.
    /// </summary>
    /// <param name="logger">The logger instance for this module.</param>
    protected ModuleBase(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string Version { get; }

    /// <inheritdoc />
    public virtual string? Description => null;

    /// <inheritdoc />
    public IConfiguration? Configuration { get; set; }

    public IConfigurationService? ConfigurationService { get; set; }

    public IConfigurationReadinessService? ConfigurationReadinessService { get; set; }

    public IKernelBuilder? KernelBuilder { get; set; }
    
    /// <inheritdoc />
    public abstract Task RegisterServicesAsync(IServiceCollection services);

    /// <inheritdoc />
    public virtual void Initialize(IApplicationBuilder app)
    {
        // Default implementation does nothing
        // Override in derived classes if initialization is needed
    }

    /// <summary>
    /// Gets a strongly-typed configuration section for this module.
    /// </summary>
    /// <typeparam name="T">The type of configuration object to bind to.</typeparam>
    /// <param name="sectionName">The name of the configuration section. If null, uses the module name.</param>
    /// <returns>The configuration object, or null if the section doesn't exist.</returns>
    protected T? GetConfiguration<T>(string? sectionName = null) where T : class, new()
    {
        if (Configuration == null)
            return null;

        var section = Configuration.GetSection(sectionName ?? Name);
        if (!section.Exists())
            return null;

        var config = new T();
        section.Bind(config);
        return config;
    }

    /// <summary>
    /// Logs an informational message during module operations.
    /// </summary>
    /// <param name="message">The message to log.</param>
    protected void Log(string message)
    {
        Logger.LogInformation("[{ModuleName}] {Message}", Name, message);
    }
}
