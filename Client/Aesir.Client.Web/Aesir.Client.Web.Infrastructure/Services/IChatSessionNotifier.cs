namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Service for notifying components when chat sessions are created or updated.
/// This allows different parts of the application to react to session changes
/// without creating module dependencies.
/// </summary>
public interface IChatSessionNotifier
{
    /// <summary>
    /// Event raised when a new chat session is created.
    /// </summary>
    event Action<Guid>? OnSessionCreated;

    /// <summary>
    /// Notifies subscribers that a new chat session was created.
    /// </summary>
    void NotifySessionCreated(Guid sessionId);
}

/// <summary>
/// Implementation of <see cref="IChatSessionNotifier"/>.
/// </summary>
public class ChatSessionNotifier : IChatSessionNotifier
{
    /// <inheritdoc />
    public event Action<Guid>? OnSessionCreated;

    /// <inheritdoc />
    public void NotifySessionCreated(Guid sessionId)
    {
        OnSessionCreated?.Invoke(sessionId);
    }
}
