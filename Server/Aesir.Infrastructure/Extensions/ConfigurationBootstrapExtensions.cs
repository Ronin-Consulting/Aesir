using Aesir.Infrastructure.Data;
using Aesir.Infrastructure.Services;
using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace Aesir.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods to configure and initialize the essential infrastructure of the Aesir application.
/// This includes database setup, dependency injection configuration, feature module integration,
/// and preparation of services necessary to orchestrate the application's functionality.
/// </summary>
public static class ConfigurationBootstrapExtensions
{
    /// Configures the Aesir application by initializing the database context, data source,
    /// module services, and configuration settings.
    /// This method prepares the application for further orchestration setup and module integration.
    /// <param name="services">
    /// The service collection to which dependencies will be added.
    /// </param>
    /// <param name="configuration">
    /// The application configuration that provides access to configuration settings.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation, which returns the configured service collection.
    /// </returns>
    public static async Task<IServiceCollection> ConfigureAesirInfrastructureAsync(this IServiceCollection services,
        IConfiguration configuration)
    {
        // Initialize Dapper column mapper for PascalCase to snake_case conversion
        DapperColumnMapper.Initialize();

        var npgsqlDataSource = CreateNpgsqlDataSource(configuration);
        services.AddSingleton(npgsqlDataSource);

        var dbContext = new PgDbContext(npgsqlDataSource);
        services.AddSingleton<IDbContext>(dbContext);

        ConfigurationServiceFactory.InitializeInstance(NullLoggerFactory.Instance, dbContext, configuration);

        // add the configuration module first so that it can be used to initialize the ConfigurationServiceFactory
        // with specific configuration services
        services.AddAesirConfigurationModule(configuration);
        
        // I dont like this here... it should be moved to the configuration module
        // the issue is a lot of the old migrations are just jammed into the infrastructure library
        await services.PrepareConfigurationAsync(configuration);
        
        services.AddAesirFeatureModules(configuration);
        
        return services;
    }

    /// Prepares the system configuration asynchronously based on the provided configuration.
    /// This includes handling database migrations and ensuring that configurations are ready
    /// for use, including both database-backed and file-based configurations.
    /// <param name="services">
    /// The service collection to which dependencies are added and configuration preparation is applied.
    /// </param>
    /// <param name="configuration">
    /// The application configuration used to set up and validate system readiness.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    private static async Task PrepareConfigurationAsync(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var configurationService = ConfigurationServiceFactory.Instance()!.CreateConfigurationService();
        var configurationReadinessService =
            ConfigurationServiceFactory.Instance()!.CreateConfigurationReadinessService();

        if (configurationService.DatabaseMode)
        {
            // Ensure database and tables exist before trying to load config
            EnsureDatabaseMigrations(configuration);

            // Check database configuration for "fully ready" vs booting into "setup only" state
            await configurationService.PrepareDatabaseConfigurationAsync(configurationReadinessService);
        }
        else
        {
            // Validate settings and add/fix up ids ...
            configurationService.PrepareFileConfigurationAsync();
        }
    }

    /// Ensures that all database migrations are applied to the configured database
    /// to guarantee the database schema meets current application requirements.
    /// The operation includes setup for migration services, execution of pending
    /// migrations, and logging of the results.
    /// <param name="configuration">
    /// The application configuration object that provides the necessary settings
    /// to connect to the database and perform migrations.
    /// </param>
    private static void EnsureDatabaseMigrations(IConfiguration configuration)
    {
        // Create a logger for migration discovery
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var migrationDiscoveryLogger = loggerFactory.CreateLogger("MigrationDiscovery");

        // Create a temporary service collection just for migrations
        var migrationServices = new ServiceCollection();

        migrationServices.RegisterMigratorServices(configuration, migrationDiscoveryLogger);

        using var migrationServiceProvider = migrationServices.BuildServiceProvider(false);
        using var scope = migrationServiceProvider.CreateScope();

        try
        {
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
            runner.MigrateUp();

            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IMigrationRunner>>();
            logger.LogInformation("Database migrations completed successfully");
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IMigrationRunner>>();
            logger.LogError(ex, "Failed to run database migrations");
            throw; // Re-throw to prevent starting with potentially missing tables
        }
    }

    /// <summary>
    /// Registers services required for managing database migrations, including the discovery of
    /// assemblies containing migration scripts and configuration of the migration runner.
    /// </summary>
    /// <param name="services">
    /// The service collection to which the migration services will be added.
    /// </param>
    /// <param name="configuration">
    /// The application configuration containing database connection settings and other migration-related configurations.
    /// </param>
    /// <param name="logger">The logger for migration discovery operations.</param>
    private static void RegisterMigratorServices(this IServiceCollection services, IConfiguration configuration, ILogger logger)
    {
        // Discover all assemblies to scan for migrations (Infrastructure + all modules)
        var assembliesToScan = new List<System.Reflection.Assembly>
        {
            // Always scan Infrastructure assembly
            typeof(PgDbContext).Assembly
        };

        // Discover and add all module assemblies
        var moduleAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.FullName != null && a.FullName.StartsWith("Aesir.Modules."))
            .ToList();

        // Also scan for module assemblies in the application directory (in case they haven't been loaded yet)
        var applicationPath = AppDomain.CurrentDomain.BaseDirectory;
        var dllFiles = Directory.GetFiles(applicationPath, "Aesir.Modules.*.dll", SearchOption.TopDirectoryOnly);

        foreach (var dllFile in dllFiles)
        {
            try
            {
                var assembly = System.Reflection.Assembly.LoadFrom(dllFile);
                if (!moduleAssemblies.Contains(assembly))
                {
                    moduleAssemblies.Add(assembly);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[MigrationDiscovery] Failed to load assembly {DllFile}", dllFile);
            }
        }

        assembliesToScan.AddRange(moduleAssemblies);

        logger.LogInformation("[MigrationDiscovery] Scanning {AssemblyCount} assemblies for migrations", assembliesToScan.Count);
        foreach (var assembly in assembliesToScan)
        {
            logger.LogDebug("[MigrationDiscovery] - {AssemblyName}", assembly.GetName().Name);
        }

        services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb =>
            {
                var runner = rb.AddPostgres()
                    .WithGlobalConnectionString(configuration.GetConnectionString("DefaultConnection"));

                // Scan all discovered assemblies for migrations
                foreach (var assembly in assembliesToScan)
                {
                    runner.ScanIn(assembly).For.Migrations();
                }
            })
            .AddLogging(lb => lb.AddConsole().SetMinimumLevel(LogLevel.Information));
    }

    /// Creates an instance of NpgsqlDataSource using the provided configuration settings.
    /// Configures connection pooling and command timeouts for optimal database performance.
    /// <param name="configuration">
    /// The application configuration containing the database connection string and other settings.
    /// This should include a "DefaultConnection" connection string.
    /// </param>
    /// <returns>
    /// A configured NpgsqlDataSource instance for interacting with the PostgreSQL database.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the "DefaultConnection" connection string is not found in the configuration.
    /// </exception>
    private static NpgsqlDataSource CreateNpgsqlDataSource(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new InvalidOperationException(
                                   "DefaultConnection connection string not configured");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString)
        {
            ConnectionStringBuilder =
            {
                // Configure connection pool parameters for optimal performance
                MaxPoolSize = 100,
                MinPoolSize = 10,
                ConnectionIdleLifetime = 300, // 5 minutes
                ConnectionPruningInterval = 10, // 10 seconds
                CommandTimeout = 30 // 30 seconds
            }
        };

        return dataSourceBuilder.Build();
    }
}