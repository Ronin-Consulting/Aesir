using Aesir.Infrastructure.Data;
using Aesir.Infrastructure.Modules;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Configuration.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Configuration;

/// <summary>
/// Configuration module that provides configuration management services for the AESIR system.
/// Handles agents, tools, inference engines, MCP servers, and general settings.
/// </summary>
public class ConfigurationModule : ModuleBase
{
    public ConfigurationModule(ILogger logger) : base(logger)
    {
    }

    public override string Name => "Configuration";

    public override string Version => "1.0.0";

    public override string Description => "Provides configuration management for agents, tools, inference engines, MCP servers, and general settings";

    public override Task RegisterServicesAsync(IServiceCollection services)
    {
        var factoryInstance = Infrastructure.Services.ConfigurationServiceFactory.Instance();

        factoryInstance!.DefaultConfigurationServiceFactory =
            (Func<ILoggerFactory, IDbContext, IConfiguration, ConfigurationService>?)
            ConfigurationServiceFactory;

        factoryInstance!.DefaultConfigurationReadinessServiceFactory =
            (Func<ILoggerFactory, IDbContext, IConfiguration, ConfigurationReadinessService>?)
            ConfigurationReadinessServiceFactory;

        // Register configuration readiness service as singleton (maintains boot-time state)
        // Using factory delegate to allow DI to provide the service provider
        services.AddSingleton<IConfigurationReadinessService>(sp => new ConfigurationReadinessService(sp));

        // Register main configuration service as singleton (manages all configuration)
        services.AddSingleton<IConfigurationService>((sp) => factoryInstance.CreateConfigurationService());

        return Task.CompletedTask;

        ConfigurationService ConfigurationServiceFactory(ILoggerFactory f, IDbContext d, IConfiguration i)
        {
            var logger = f.CreateLogger<ConfigurationService>();
            return new ConfigurationService(logger, d, i);
        }

        ConfigurationReadinessService ConfigurationReadinessServiceFactory(ILoggerFactory f, IDbContext d, IConfiguration i)
        {
            // Build a temporary service provider to get required services
            var tempServices = new ServiceCollection();
            tempServices.AddSingleton(f);
            tempServices.AddSingleton(d);
            tempServices.AddSingleton(i);
            var sp = tempServices.BuildServiceProvider();
            return new ConfigurationReadinessService(sp);
        }
    }

    public override void Initialize(IApplicationBuilder app)
    {
        // Configuration module uses controller-based routing
        // Controllers are automatically discovered via ASP.NET Core's convention
    }
}
