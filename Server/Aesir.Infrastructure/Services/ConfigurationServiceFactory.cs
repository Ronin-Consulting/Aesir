using Aesir.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Infrastructure.Services;

/// <summary>
/// Factory class for creating and managing instances of configuration-related services.
/// </summary>
/// <remarks>
/// The <see cref="ConfigurationServiceFactory"/> class provides a way to create instances of
/// <see cref="IConfigurationService"/> and <see cref="IConfigurationReadinessService"/> using customizable factory methods.
/// This class is designed to be a singleton that is initialized once in the application's lifecycle
/// and accessed via the <see cref="Instance"/> method.
/// </remarks>
public sealed class ConfigurationServiceFactory
{
    /// <summary>
    /// Holds the singleton instance of the <see cref="ConfigurationServiceFactory"/> class, ensuring a single
    /// instance is shared across the application. The instance is initialized and accessed through thread-safe
    /// mechanisms provided by the associated methods.
    /// </summary>
    private static ConfigurationServiceFactory? _instance;

    /// <summary>
    /// Represents a synchronization mechanism used to ensure thread-safe access
    /// to critical sections of code within the ConfigurationServiceFactory class.
    /// </summary>
    private static readonly Lock Lock = new();

    /// <summary>
    /// Provides an instance of <see cref="Microsoft.Extensions.Logging.ILoggerFactory"/> used to create
    /// logger instances for logging operations throughout the configuration service factory.
    /// </summary>
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Represents the database context used for managing database operations and connections.
    /// </summary>
    /// <remarks>
    /// This field is an instance of the <see cref="IDbContext"/> interface, which provides
    /// access to the underlying database connection for performing CRUD operations or custom queries.
    /// It serves as a dependency for various configuration services and ensures consistent
    /// interaction with the data storage layer.
    /// </remarks>
    private readonly IDbContext _dbContext;

    /// <summary>
    /// Represents an instance of the configuration settings tied to the application's configuration.
    /// </summary>
    /// <remarks>
    /// This variable is used to provide access to the application's configuration data, as defined by the
    /// implementation of <see cref="IConfiguration"/>. It is utilized within the factory to create and
    /// manage service instances that depend on application settings.
    /// </remarks>
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Gets or sets the default factory function for creating instances of
    /// <see cref="IConfigurationService"/>.
    /// </summary>
    /// <remarks>
    /// This property allows the customization of the initialization process for
    /// <see cref="IConfigurationService"/> instances. A custom implementation can be
    /// assigned to this property to define specific creation logic, leveraging the provided dependencies
    /// such as <see cref="ILoggerFactory"/>, <see cref="IDbContext"/>, and <see cref="IConfiguration"/>.
    /// If this property is not set, attempting to create an <see cref="IConfigurationService"/>
    /// through the factory will result in an <see cref="InvalidOperationException"/>.
    /// </remarks>
    public Func<ILoggerFactory, IDbContext, IConfiguration, IConfigurationService>? DefaultConfigurationServiceFactory
    {
        get;
        set;
    }

    /// <summary>
    /// Represents a factory delegate for creating instances of <see cref="IConfigurationReadinessService"/>.
    /// </summary>
    /// <remarks>
    /// This property allows customization of the creation of the <see cref="IConfigurationReadinessService"/> instance
    /// by assigning a delegate that accepts <see cref="ILoggerFactory"/>, <see cref="IDbContext"/>, and <see cref="IConfiguration"/>
    /// as parameters and returns an instance of <see cref="IConfigurationReadinessService"/>.
    /// If not explicitly set, attempting to create an <see cref="IConfigurationReadinessService"/> will result in an exception.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown by the <see cref="ConfigurationServiceFactory.CreateConfigurationReadinessService"/> method if this property is not set.
    /// </exception>
    public Func<ILoggerFactory, IDbContext, IConfiguration, IConfigurationReadinessService>?
        DefaultConfigurationReadinessServiceFactory
    {
        get;
        set;
    }

    /// <summary>
    /// A factory class responsible for creating configuration-related services.
    /// Ensures a single instance through the Singleton pattern and allows for custom service factories to be defined.
    /// </summary>
    private ConfigurationServiceFactory(ILoggerFactory loggerFactory,
        IDbContext dbContext, IConfiguration configuration
    )
    {
        _loggerFactory = loggerFactory;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    /// <summary>
    /// Initializes the singleton instance of the ConfigurationServiceFactory with the provided logger factory, database context, and configuration.
    /// </summary>
    /// <param name="loggerFactory">The logger factory used for creating and managing loggers.</param>
    /// <param name="dbContext">The database context that provides access to database connections.</param>
    /// <param name="configuration">The application configuration settings.</param>
    public static void InitializeInstance(
        ILoggerFactory loggerFactory, IDbContext dbContext, IConfiguration configuration
    )
    {
        if (_instance == null)
        {
            lock (Lock)
            {
                _instance ??= new ConfigurationServiceFactory(loggerFactory, dbContext, configuration);
            }
        }
    }

    /// <summary>
    /// Gets the singleton instance of the ConfigurationServiceFactory. Throws an InvalidOperationException
    /// if the instance has not been initialized.
    /// </summary>
    /// <returns>The singleton instance of ConfigurationServiceFactory.</returns>
    public static ConfigurationServiceFactory? Instance()
    {
        return _instance ?? throw new InvalidOperationException();
    }

    /// <summary>
    /// Creates and returns an instance of <see cref="IConfigurationService"/> using the default factory logic.
    /// </summary>
    /// <returns>An instance of <see cref="IConfigurationService"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the default configuration service factory is not set.
    /// </exception>
    public IConfigurationService CreateConfigurationService()
    {
        if (DefaultConfigurationServiceFactory == null)
            throw new InvalidOperationException("Default configuration service factory not set");
            
        return DefaultConfigurationServiceFactory(_loggerFactory,_dbContext, _configuration);
    }

    /// Creates an instance of the `IConfigurationReadinessService` using the configured default factory method.
    /// Throws an `InvalidOperationException` if the default configuration readiness service factory is not set.
    /// <returns>
    /// An instance of `IConfigurationReadinessService`.
    /// </returns>
    public IConfigurationReadinessService CreateConfigurationReadinessService()
    {
        if (DefaultConfigurationReadinessServiceFactory == null)
            throw new InvalidOperationException("Default configuration readiness service factory not set");
        
        return DefaultConfigurationReadinessServiceFactory(_loggerFactory,_dbContext, _configuration);
    }
}