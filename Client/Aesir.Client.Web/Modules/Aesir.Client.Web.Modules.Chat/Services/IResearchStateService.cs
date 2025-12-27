using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Service for managing research state during a chat session.
/// Tracks active research sessions and provides progress updates.
/// </summary>
public interface IResearchStateService
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
    /// <returns>The created session.</returns>
    Task<ResearchSessionBase?> StartResearchAsync(
        string query,
        Guid teamId,
        ResearchModeBase mode = ResearchModeBase.Standard,
        List<Guid>? documentCollectionIds = null);

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
}
