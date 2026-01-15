using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Research.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Service for managing research state during a chat session.
/// Tracks active research sessions and provides progress updates with multi-agent support.
/// </summary>
public interface IResearchStateService : IDisposable
{
    /// <summary>
    /// The currently active research session, if any.
    /// </summary>
    ResearchSessionBase? ActiveSession { get; }

    /// <summary>
    /// Whether a research session is currently in progress.
    /// </summary>
    bool IsResearchInProgress { get; }

    /// <summary>
    /// The selected research team, if any.
    /// </summary>
    ResearchTeamBase? SelectedTeam { get; }

    /// <summary>
    /// Whether a research team is currently selected.
    /// </summary>
    bool IsTeamSelected { get; }

    /// <summary>
    /// The current progress message.
    /// </summary>
    string? CurrentProgressMessage { get; }

    /// <summary>
    /// The current progress percentage (0-100).
    /// </summary>
    int CurrentProgressPercent { get; }

    /// <summary>
    /// List of currently active agents with their activities.
    /// Empty when no agents are actively working (e.g., phase transitions).
    /// </summary>
    IReadOnlyList<ActiveAgentInfo> ActiveAgents { get; }

    /// <summary>
    /// Whether any agents are currently actively working.
    /// </summary>
    bool HasActiveAgents { get; }

    /// <summary>
    /// The current agent role performing work, if any.
    /// For backward compatibility - returns first active agent's role.
    /// Use ActiveAgents for multi-agent display.
    /// </summary>
    ResearchRoleBase? CurrentAgentRole { get; }

    /// <summary>
    /// The current agent activity message (e.g., "DeepDiver is researching...").
    /// For backward compatibility - returns first active agent's activity.
    /// Use ActiveAgents for multi-agent display.
    /// </summary>
    string? CurrentAgentActivity { get; }

    /// <summary>
    /// Whether an agent is currently actively working.
    /// Equivalent to HasActiveAgents.
    /// </summary>
    bool IsAgentActive { get; }

    /// <summary>
    /// Event raised when the active session changes.
    /// </summary>
    event Action? OnSessionChanged;

    /// <summary>
    /// Event raised when progress is updated.
    /// </summary>
    event Action<ResearchProgressBase>? OnProgressUpdate;

    /// <summary>
    /// Event raised when research completes.
    /// </summary>
    event Action<ResearchSessionBase>? OnResearchCompleted;

    /// <summary>
    /// Event raised when research fails.
    /// </summary>
    event Action<string>? OnResearchError;

    /// <summary>
    /// Event raised when the selected team changes.
    /// </summary>
    event Action? OnTeamChanged;

    /// <summary>
    /// Selects a research team.
    /// </summary>
    /// <param name="team">The team to select, or null to deselect.</param>
    void SelectTeam(ResearchTeamBase? team);

    /// <summary>
    /// Starts a research session.
    /// </summary>
    /// <param name="query">The research query.</param>
    /// <param name="teamId">The research team ID.</param>
    /// <param name="mode">The research mode.</param>
    /// <param name="documentCollectionIds">Optional document collection IDs.</param>
    /// <param name="conversationId">Optional ChatSession ID to link research to.</param>
    /// <returns>The created session.</returns>
    Task<ResearchSessionBase?> StartResearchAsync(
        string query,
        Guid teamId,
        ResearchModeBase mode = ResearchModeBase.Standard,
        List<Guid>? documentCollectionIds = null,
        Guid? conversationId = null);

    /// <summary>
    /// Submits clarification answers.
    /// </summary>
    /// <param name="answers">The answers to clarification questions.</param>
    /// <returns>The updated session.</returns>
    Task<ResearchSessionBase?> SubmitClarificationAsync(Dictionary<string, string> answers);

    /// <summary>
    /// Cancels the active research session.
    /// </summary>
    Task CancelResearchAsync();

    /// <summary>
    /// Refreshes the active session status.
    /// </summary>
    Task RefreshSessionAsync();

    /// <summary>
    /// Handles a progress update from SignalR.
    /// </summary>
    /// <param name="progress">The progress update.</param>
    void HandleProgressUpdate(ResearchProgressBase progress);

    /// <summary>
    /// Clears the active session.
    /// </summary>
    void ClearSession();

    /// <summary>
    /// Restores an active research session if one exists for the current user.
    /// Called on page load to resume viewing in-progress research.
    /// </summary>
    /// <returns>True if an active session was restored.</returns>
    Task<bool> RestoreActiveSessionAsync();

    /// <summary>
    /// Restores a research session for a specific conversation if one exists.
    /// Called when navigating to a conversation to resume viewing in-progress or completed research.
    /// </summary>
    /// <param name="conversationId">The conversation ID to restore session for.</param>
    /// <returns>True if a session was restored.</returns>
    Task<bool> RestoreSessionForConversationAsync(Guid conversationId);
}
