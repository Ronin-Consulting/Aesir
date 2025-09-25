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

    public IList<string> MissingRequiredConfigurationReasons { get; }
}