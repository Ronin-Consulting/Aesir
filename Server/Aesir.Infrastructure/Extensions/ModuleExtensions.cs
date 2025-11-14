using Aesir.Infrastructure.Modules;
using Aesir.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;

namespace Aesir.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for configuring and integrating modules within the ASP.NET Core application lifecycle.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// A private static list that holds instances of discovered modules implementing the <see cref="IModule"/> interface.
    /// These modules are dynamically discovered and loaded at runtime, facilitating modularity and extensibility
    /// within the application.
    /// </summary>
    private static readonly List<IModule> DiscoveredModules = [];

    /// <summary>
    /// Ensures that all modules have been discovered and loaded into the system.
    /// This method initializes the discovered modules collection only once
    /// and prevents duplicate discovery operations.
    /// </summary>
    /// <param name="loggerFactory">The logger factory for creating loggers.</param>
    private static void EnsureModulesDiscovered(ILoggerFactory loggerFactory)
    {
        if (DiscoveredModules.Count > 0)
            return;

        // Discover all modules
        var modules = ModuleDiscovery.DiscoverModules(loggerFactory).ToList();
        DiscoveredModules.Clear();
        DiscoveredModules.AddRange(modules);
    }

    /// <summary>
    /// Adds and initializes the Aesir Configuration Module if it exists among the discovered modules.
    /// This ensures that any required configuration services for the specified module are registered.
    /// </summary>
    /// <param name="services">The service collection used to register services.</param>
    /// <param name="configuration">The application configuration object.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAesirConfigurationModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Get or create ILoggerFactory from services
        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("ModuleExtensions");

        EnsureModulesDiscovered(loggerFactory);

        // run the configuration module first if it exists
        var module = DiscoveredModules.FirstOrDefault(m => m.Name == "Configuration");
        if (module != null)
        {
            try
            {
                module.Configuration = configuration;

                logger.LogInformation("Registering configuration services for module: {ModuleName} v{Version}",
                    module.Name, module.Version);
                module.RegisterServicesAsync(services);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to register configuration services for module {ModuleName}", module.Name);
                throw;
            }
        }

        return services;
    }

    /// <summary>
    /// Registers feature modules within the Aesir system. This method discovers all modules,
    /// validates their configuration setup, and registers their services into the provided
    /// dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to which module services will be added.</param>
    /// <param name="configuration">The configuration object used to initialize module settings.</param>
    /// <returns>The updated service collection after module registration.</returns>
    public static IServiceCollection AddAesirFeatureModules(this IServiceCollection services, IConfiguration configuration)
    {
        // Get or create ILoggerFactory from services
        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("ModuleExtensions");

        EnsureModulesDiscovered(loggerFactory);

        // if this has not been initialized, we should get error.
        var configurationServiceFactory = ConfigurationServiceFactory.Instance();

        var configurationService = configurationServiceFactory!.CreateConfigurationService();
        var configurationReadinessService = configurationServiceFactory.CreateConfigurationReadinessService();

        // Register each module's services NOT configuration because it should have been configured first
        foreach (var module in DiscoveredModules.Where(m => m.Name != "Configuration"))
        {
            try
            {
                module.ConfigurationService = configurationService;
                module.ConfigurationReadinessService = configurationReadinessService;

                module.Configuration = configuration;

                logger.LogInformation("Registering services for module: {ModuleName} v{Version}",
                    module.Name, module.Version);
                module.RegisterServicesAsync(services);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to register services for module {ModuleName}", module.Name);
                throw;
            }
        }

        logger.LogInformation("Successfully registered {ModuleCount} module(s)", DiscoveredModules.Count);

        return services;
    }

    /// <summary>
    /// Discovers and retrieves all module assemblies.
    /// The returned assemblies can be used for modular application scenarios,
    /// such as registering them with ASP.NET Core's ApplicationPartManager.
    /// </summary>
    /// <returns>A collection of discovered module assemblies.</returns>
    public static IEnumerable<System.Reflection.Assembly> GetModuleAssemblies()
    {
        // Create a logger factory for module discovery
        using var loggerFactory = LoggerFactory.Create(builder => {
        {
            //builder.ClearProviders(); // Clear default providers like Console
            //builder.SetMinimumLevel(LogLevel.Trace); // Set desired minimum logging level
            builder.AddNLog();
        } });
        return ModuleDiscovery.DiscoverModuleAssemblies(loggerFactory);
    }

    /// <summary>
    /// Initializes all discovered and registered modules in the application.
    /// This method should be used to ensure any module-specific setup is completed
    /// during application startup.
    /// </summary>
    /// <param name="app">The application builder used for configuring the app's request pipeline.</param>
    /// <returns>The configured application builder for chaining further pipeline configurations.</returns>
    public static IApplicationBuilder UseModules(this IApplicationBuilder app)
    {
        // Get logger from application services
        var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("ModuleExtensions");

        logger.LogInformation("Initializing modules...");

        foreach (var module in DiscoveredModules)
        {
            try
            {
                logger.LogInformation("Initializing module: {ModuleName}", module.Name);
                module.Initialize(app);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize module {ModuleName}", module.Name);
            }
        }

        logger.LogInformation("Successfully initialized {ModuleCount} module(s)", DiscoveredModules.Count);
        return app;
    }

    /// <summary>
    /// Retrieves the collection of modules that have been discovered.
    /// </summary>
    /// <returns>A read-only collection of discovered modules.</returns>
    public static IReadOnlyCollection<IModule> GetDiscoveredModules()
    {
        return DiscoveredModules.AsReadOnly();
    }
}
