using Aesir.Tools.LegalValidator.Models;

namespace Aesir.Tools.LegalValidator.Services;

/// <summary>
/// Service for generating validation reports in various formats.
/// </summary>
public interface IReportGenerator
{
    /// <summary>
    /// Generates a JSON report.
    /// </summary>
    Task<string> GenerateJsonReportAsync(ValidationReport report, CancellationToken ct = default);

    /// <summary>
    /// Generates a Markdown report.
    /// </summary>
    Task<string> GenerateMarkdownReportAsync(ValidationReport report, CancellationToken ct = default);

    /// <summary>
    /// Generates Claude Code instructions for prompt improvements.
    /// </summary>
    Task<string> GenerateClaudeCodeInstructionsAsync(ValidationReport report, CancellationToken ct = default);

    /// <summary>
    /// Calculates summary statistics for an agent.
    /// </summary>
    AgentSummary CalculateAgentSummary(
        Guid agentId,
        string agentName,
        IReadOnlyList<EvaluationResult> results,
        IReadOnlyList<AgentResponse> responses);
}
