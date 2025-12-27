namespace Aesir.Modules.Research.Agents;

/// <summary>
/// Provides prompt templates for various research phases.
/// </summary>
public static class ResearchPromptTemplates
{
    /// <summary>
    /// Template for peer review evaluation.
    /// Placeholders: {{SUBMISSION_ID}}, {{QUERY}}, {{SUBMISSION_CONTENT}}
    /// </summary>
    public static string PeerReviewPrompt => """
        You are evaluating research submission "{{SUBMISSION_ID}}" on the topic:

        {{QUERY}}

        ## Submission Content

        {{SUBMISSION_CONTENT}}

        ## Evaluation Criteria

        Rate each criterion from 1-10:

        ### 1. Depth (How thoroughly was the topic explored?)
        Score: [1-10]
        Justification: [2-3 sentences]

        ### 2. Accuracy (How well are claims supported by evidence?)
        Score: [1-10]
        Justification: [2-3 sentences]

        ### 3. Source Quality (Are sources authoritative and relevant?)
        Score: [1-10]
        Justification: [2-3 sentences]

        ### 4. Novelty (Were unique or unexpected insights uncovered?)
        Score: [1-10]
        Justification: [2-3 sentences]

        ### 5. Coherence (Is the argument well-structured and clear?)
        Score: [1-10]
        Justification: [2-3 sentences]

        ## Qualitative Assessment

        ### Key Strengths
        [List 2-3 main strengths]

        ### Areas for Improvement
        [List 2-3 areas that could be improved]

        ### Additional Insights
        [Any insights the reviewer would add based on their perspective]

        ## JSON Summary

        Return your scores in the following JSON format at the end:
        ```json
        {
            "depth": <score>,
            "accuracy": <score>,
            "source_quality": <score>,
            "novelty": <score>,
            "coherence": <score>,
            "weighted_average": <calculated_average>
        }
        ```
        """;

    /// <summary>
    /// Template for anonymizing a submission before peer review.
    /// Placeholders: {{SUBMISSION_CONTENT}}, {{AGENT_PATTERNS}}
    /// </summary>
    public static string AnonymizationPrompt => """
        Anonymize the following research submission by replacing any identifying information:

        ## Submission Content

        {{SUBMISSION_CONTENT}}

        ## Patterns to Remove

        Replace the following patterns with anonymous identifiers:
        {{AGENT_PATTERNS}}

        Rules:
        1. Replace agent names with "Researcher A", "Researcher B", etc.
        2. Keep all research content intact
        3. Preserve formatting and structure
        4. Remove any self-references or role-specific language
        5. Maintain citations and sources

        Return the anonymized submission.
        """;

    /// <summary>
    /// Template for de-anonymizing a submission after peer review.
    /// Placeholders: {{ANONYMIZED_CONTENT}}, {{MAPPING}}
    /// </summary>
    public static string DeAnonymizationPrompt => """
        Restore the original agent identities in the following anonymized content:

        ## Anonymized Content

        {{ANONYMIZED_CONTENT}}

        ## Identity Mapping

        {{MAPPING}}

        Return the content with original identities restored.
        """;

    /// <summary>
    /// Template for refining the research query with clarification answers.
    /// Placeholders: {{ORIGINAL_QUERY}}, {{CLARIFICATION_QA}}
    /// </summary>
    public static string QueryRefinementPrompt => """
        Refine the following research query based on the clarification answers provided:

        ## Original Query

        {{ORIGINAL_QUERY}}

        ## Clarification Q&A

        {{CLARIFICATION_QA}}

        ## Task

        Produce a refined, focused research query that incorporates:
        1. The original intent
        2. The scope refinements from clarifications
        3. Any specific constraints or priorities mentioned

        Return a single, well-formed research query (2-4 sentences max).
        """;

    /// <summary>
    /// Template for generating a research plan summary.
    /// Placeholders: {{AGENT_PLANS}}
    /// </summary>
    public static string PlanSummaryPrompt => """
        Summarize the following research plans from multiple agents:

        ## Agent Plans

        {{AGENT_PLANS}}

        ## Task

        Create a brief summary (3-5 sentences) of the collective research approach:
        - What aspects will be covered
        - Key areas of focus for each approach
        - Any notable differences in methodology

        This summary will be shown to the user while research is in progress.
        """;

    /// <summary>
    /// Template for generating progress updates.
    /// Placeholders: {{PHASE}}, {{AGENT_STATUS}}, {{ELAPSED_TIME}}
    /// </summary>
    public static string ProgressUpdatePrompt => """
        Generate a brief, user-friendly progress update for the current research:

        ## Current Phase

        {{PHASE}}

        ## Agent Status

        {{AGENT_STATUS}}

        ## Elapsed Time

        {{ELAPSED_TIME}}

        ## Task

        Write a 1-2 sentence progress update suitable for display in a chat interface.
        Be concise and informative. Use present continuous tense.
        """;

    /// <summary>
    /// Applies template variables to a prompt string.
    /// </summary>
    /// <param name="template">The prompt template with {{PLACEHOLDER}} markers.</param>
    /// <param name="variables">Dictionary of variable names to values.</param>
    /// <returns>The prompt with variables substituted.</returns>
    public static string ApplyVariables(string template, Dictionary<string, string> variables)
    {
        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{{{key}}}}}", value);
        }
        return result;
    }

    /// <summary>
    /// Applies template variables to a prompt string using an anonymous object.
    /// </summary>
    /// <param name="template">The prompt template with {{PLACEHOLDER}} markers.</param>
    /// <param name="variables">Anonymous object with properties matching placeholder names.</param>
    /// <returns>The prompt with variables substituted.</returns>
    public static string ApplyVariables(string template, object variables)
    {
        var result = template;
        var properties = variables.GetType().GetProperties();
        foreach (var prop in properties)
        {
            var value = prop.GetValue(variables)?.ToString() ?? string.Empty;
            result = result.Replace($"{{{{{prop.Name.ToUpperInvariant()}}}}}", value);
        }
        return result;
    }
}
