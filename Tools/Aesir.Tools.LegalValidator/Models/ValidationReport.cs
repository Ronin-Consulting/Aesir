using System.Text.Json.Serialization;

namespace Aesir.Tools.LegalValidator.Models;

/// <summary>
/// Complete validation report containing all results and analysis.
/// </summary>
public class ValidationReport
{
    /// <summary>
    /// Timestamp when the validation was run.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Names of agents that were tested.
    /// </summary>
    [JsonPropertyName("agents_tested")]
    public List<string> AgentsTested { get; set; } = [];

    /// <summary>
    /// Total number of questions used in validation.
    /// </summary>
    [JsonPropertyName("total_questions")]
    public int TotalQuestions { get; set; }

    /// <summary>
    /// Categories of questions that were tested.
    /// </summary>
    [JsonPropertyName("categories_tested")]
    public List<QuestionCategory> CategoriesTested { get; set; } = [];

    /// <summary>
    /// Evaluation results grouped by agent ID.
    /// </summary>
    [JsonPropertyName("results_by_agent")]
    public Dictionary<Guid, List<EvaluationResult>> ResultsByAgent { get; set; } = new();

    /// <summary>
    /// Summary statistics for each agent.
    /// </summary>
    [JsonPropertyName("agent_summaries")]
    public Dictionary<Guid, AgentSummary> AgentSummaries { get; set; } = new();

    /// <summary>
    /// Raw responses from agents (for debugging/analysis).
    /// </summary>
    [JsonPropertyName("raw_responses")]
    public List<AgentResponse> RawResponses { get; set; } = [];

    /// <summary>
    /// Recommended prompt adjustments based on evaluation.
    /// </summary>
    [JsonPropertyName("prompt_adjustments")]
    public List<PromptAdjustment> PromptAdjustments { get; set; } = [];

    /// <summary>
    /// System prompt used for testing (if custom prompt was provided).
    /// </summary>
    [JsonPropertyName("system_prompt_used")]
    public string? SystemPromptUsed { get; set; }

    /// <summary>
    /// Duration of the validation run in seconds.
    /// </summary>
    [JsonPropertyName("validation_duration_seconds")]
    public double ValidationDurationSeconds { get; set; }
}
