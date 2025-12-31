using Aesir.Tools.LegalValidator.Models;

namespace Aesir.Tools.LegalValidator.Services;

/// <summary>
/// Service for evaluating legal responses using Claude.
/// </summary>
public interface IClaudeEvaluator
{
    /// <summary>
    /// Evaluates a single response to a legal question.
    /// </summary>
    /// <param name="question">The original question.</param>
    /// <param name="response">The agent's response.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Evaluation result with scores and analysis.</returns>
    Task<EvaluationResult> EvaluateResponseAsync(
        LegalQuestion question,
        AgentResponse response,
        CancellationToken ct = default);

    /// <summary>
    /// Generates prompt adjustments based on evaluation results.
    /// </summary>
    /// <param name="evaluationsByAgent">Evaluation results grouped by agent.</param>
    /// <param name="currentSystemPrompt">Current system prompt (if available).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of recommended prompt adjustments.</returns>
    Task<IReadOnlyList<PromptAdjustment>> GeneratePromptAdjustmentsAsync(
        Dictionary<Guid, List<EvaluationResult>> evaluationsByAgent,
        string? currentSystemPrompt,
        CancellationToken ct = default);
}
