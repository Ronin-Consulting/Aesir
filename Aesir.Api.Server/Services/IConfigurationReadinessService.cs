namespace Aesir.Api.Server.Services;

/// <summary>
/// Manage and report on the readiness status of configuration required for system initialization.
/// </summary>
public interface IConfigurationReadinessService
{
    /// <summary>
    /// Indicates whether the configuration was in a ready state during boot up.
    /// </summary>
    public bool IsReadyAtBoot { get; }

    /// <summary>
    /// Gets the collection of specific reasons why the configuration is not ready.
    /// This provides detailed information about what configuration elements are missing or invalid.
    /// </summary>
    /// <returns>
    /// A collection of human-readable strings describing configuration issues.
    /// Returns an empty collection when the system is ready.
    /// </returns>
    public IEnumerable<string> MissingRequiredConfigurationReasons { get; }
}
