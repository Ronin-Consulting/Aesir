namespace Aesir.Infrastructure.Services;

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
    public IList<string> MissingRequiredConfigurationReasons { get; }

    /// <summary>
    /// Adds a missing required configuration message indicating why the system can't start.
    /// </summary>
    /// <param name="reason">
    /// The reason or description of the missing configuration required for boot up
    /// </param>
    void ReportMissingConfiguration(string reason);

    /// <summary>
    /// Marks an inference engine as not ready for use during system initialization.
    /// </summary>
    /// <param name="inferenceEngineId">
    /// The unique identifier of the inference engine to be marked as not ready.
    /// </param>
    void MarkInferenceEngineNotReadyAtBoot(Guid inferenceEngineId);

    /// <summary>
    /// Determines whether the specified inference engine is ready at boot time.
    /// </summary>
    /// <param name="inferenceEngineId">
    /// The unique identifier of the inference engine to check readiness for at system boot.
    /// </param>
    /// <returns>
    /// True if the inference engine is ready at boot; otherwise, false.
    /// </returns>
    bool IsInferenceEngineReadyAtBoot(Guid inferenceEngineId);
}
