using System.Text.Json.Serialization;

namespace Aesir.Tools.LegalValidator.Models;

/// <summary>
/// Represents a recommended prompt adjustment based on evaluation results.
/// </summary>
public class PromptAdjustment
{
    /// <summary>
    /// Priority level of the adjustment.
    /// </summary>
    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "medium";

    /// <summary>
    /// Category of the issue (e.g., "citation_accuracy", "disclaimer", "scope_precision").
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Description of the issue being addressed.
    /// </summary>
    [JsonPropertyName("issue_description")]
    public string IssueDescription { get; set; } = string.Empty;

    /// <summary>
    /// Description of the current problematic behavior.
    /// </summary>
    [JsonPropertyName("current_behavior")]
    public string CurrentBehavior { get; set; } = string.Empty;

    /// <summary>
    /// Description of the desired behavior after adjustment.
    /// </summary>
    [JsonPropertyName("desired_behavior")]
    public string DesiredBehavior { get; set; } = string.Empty;

    /// <summary>
    /// Suggested text to add to the system prompt.
    /// </summary>
    [JsonPropertyName("suggested_prompt_addition")]
    public string SuggestedPromptAddition { get; set; } = string.Empty;

    /// <summary>
    /// Question IDs affected by this issue.
    /// </summary>
    [JsonPropertyName("affected_question_ids")]
    public List<string> AffectedQuestionIds { get; set; } = [];

    /// <summary>
    /// Instructions for implementing the adjustment.
    /// </summary>
    [JsonPropertyName("implementation_instructions")]
    public string ImplementationInstructions { get; set; } = string.Empty;
}
