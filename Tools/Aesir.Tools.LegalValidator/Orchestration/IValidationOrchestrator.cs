using Aesir.Tools.LegalValidator.Models;

namespace Aesir.Tools.LegalValidator.Orchestration;

/// <summary>
/// Orchestrates the complete validation workflow.
/// </summary>
public interface IValidationOrchestrator
{
    /// <summary>
    /// Runs a complete validation against the specified agents.
    /// </summary>
    /// <param name="agentIds">Agent IDs to test (null = all agents).</param>
    /// <param name="agentNamePattern">Optional pattern to filter agents by name.</param>
    /// <param name="categories">Optional category filter.</param>
    /// <param name="customQuestionsPath">Optional path to custom questions file.</param>
    /// <param name="customSystemPrompt">Optional custom system prompt to override.</param>
    /// <param name="concurrency">Number of concurrent requests per agent.</param>
    /// <param name="dryRun">If true, only shows what would be tested without running.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation report with all results.</returns>
    Task<ValidationReport> RunValidationAsync(
        IEnumerable<Guid>? agentIds = null,
        string? agentNamePattern = null,
        IEnumerable<QuestionCategory>? categories = null,
        string? customQuestionsPath = null,
        string? customSystemPrompt = null,
        int concurrency = 3,
        bool dryRun = false,
        CancellationToken ct = default);
}
