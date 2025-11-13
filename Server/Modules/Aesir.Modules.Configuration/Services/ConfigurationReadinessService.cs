using Aesir.Infrastructure.Services;

namespace Aesir.Modules.Configuration.Services;

/// <summary>
/// Manage and report on the readiness status of configuration required for system initialization.
/// </summary>
public class ConfigurationReadinessService : IConfigurationReadinessService
{
    private readonly List<string> _missingRequiredConfigurationReasons = [];

    private readonly HashSet<Guid> _nonReadyInferenceEngines = [];

    /// <summary>
    /// Indicates whether the configuration was in a ready state during boot up.
    /// </summary>
    public bool IsReadyAtBoot => _missingRequiredConfigurationReasons.Count == 0;

    public IList<string> MissingRequiredConfigurationReasons => new List<string>(_missingRequiredConfigurationReasons);

    /// <summary>
    /// Adds a missing required configuration message indicating why the system can't start.
    /// </summary>
    /// <param name="reason">
    /// The reason or description of the missing configuration required for boot up
    /// </param>
    public void ReportMissingConfiguration(string reason)
    {
        _missingRequiredConfigurationReasons.Add(reason);
    }

    /// <summary>
    /// Marks an inference engine as not ready for use during system initialization.
    /// </summary>
    /// <param name="inferenceEngineId">
    /// The unique identifier of the inference engine to be marked as not ready.
    /// </param>
    public void MarkInferenceEngineNotReadyAtBoot(Guid inferenceEngineId)
    {
        _nonReadyInferenceEngines.Add(inferenceEngineId);
    }

    /// <summary>
    /// Determines whether the specified inference engine is ready at boot time.
    /// </summary>
    /// <param name="inferenceEngineId">
    /// The unique identifier of the inference engine to check readiness for at system boot.
    /// </param>
    /// <returns>
    /// True if the inference engine is ready at boot; otherwise, false.
    /// </returns>
    public bool IsInferenceEngineReadyAtBoot(Guid inferenceEngineId)
    {
        return !_nonReadyInferenceEngines.Contains(inferenceEngineId);
    }
}
