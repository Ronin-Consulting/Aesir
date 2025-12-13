namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Service for notifying components when configuration changes occur.
/// This allows different parts of the application to react to settings changes.
/// </summary>
public interface IConfigurationChangedNotifier
{
    /// <summary>
    /// Event raised when an inference engine configuration is changed.
    /// </summary>
    event Action? OnInferenceEngineChanged;

    /// <summary>
    /// Event raised when an agent configuration is changed.
    /// </summary>
    event Action? OnAgentChanged;

    /// <summary>
    /// Notifies subscribers that an inference engine configuration has changed.
    /// </summary>
    void NotifyInferenceEngineChanged();

    /// <summary>
    /// Notifies subscribers that an agent configuration has changed.
    /// </summary>
    void NotifyAgentChanged();
}

/// <summary>
/// Implementation of <see cref="IConfigurationChangedNotifier"/>.
/// </summary>
public class ConfigurationChangedNotifier : IConfigurationChangedNotifier
{
    /// <inheritdoc />
    public event Action? OnInferenceEngineChanged;

    /// <inheritdoc />
    public event Action? OnAgentChanged;

    /// <inheritdoc />
    public void NotifyInferenceEngineChanged()
    {
        OnInferenceEngineChanged?.Invoke();
    }

    /// <inheritdoc />
    public void NotifyAgentChanged()
    {
        OnAgentChanged?.Invoke();
    }
}
