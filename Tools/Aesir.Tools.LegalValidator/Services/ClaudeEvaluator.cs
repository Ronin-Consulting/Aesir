using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aesir.Tools.LegalValidator.Configuration;
using Aesir.Tools.LegalValidator.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aesir.Tools.LegalValidator.Services;

/// <summary>
/// Implementation of Claude-based response evaluation.
/// </summary>
public partial class ClaudeEvaluator : IClaudeEvaluator
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClaudeEvaluator> _logger;
    private readonly ClaudeOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public ClaudeEvaluator(
        HttpClient httpClient,
        ILogger<ClaudeEvaluator> logger,
        IOptions<LegalValidatorOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value.Claude;
    }

    public async Task<EvaluationResult> EvaluateResponseAsync(
        LegalQuestion question,
        AgentResponse response,
        CancellationToken ct = default)
    {
        if (response.HasError)
        {
            _logger.LogWarning("Skipping evaluation for question {QuestionId} due to error: {Error}",
                question.Id, response.Error);

            return new EvaluationResult
            {
                QuestionId = question.Id,
                AgentId = response.AgentId,
                AgentName = response.AgentName,
                OverallGrade = OverallGrade.F,
                AccuracyScore = 0,
                CompletenessScore = 0,
                ClarityScore = 0,
                SourceQualityScore = 0,
                DisclaimerPresent = false,
                IssuesFound = [$"Error during response generation: {response.Error}"],
                EvaluationNotes = "Response could not be evaluated due to an error."
            };
        }

        var prompt = BuildEvaluationPrompt(question, response);

        try
        {
            var result = await CallClaudeAsync(prompt, 1500, ct).ConfigureAwait(false);
            return ParseEvaluationResult(result, question.Id, response.AgentId, response.AgentName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating response for question {QuestionId}", question.Id);

            return new EvaluationResult
            {
                QuestionId = question.Id,
                AgentId = response.AgentId,
                AgentName = response.AgentName,
                OverallGrade = OverallGrade.F,
                AccuracyScore = 0,
                CompletenessScore = 0,
                ClarityScore = 0,
                SourceQualityScore = 0,
                DisclaimerPresent = false,
                IssuesFound = [$"Evaluation error: {ex.Message}"],
                EvaluationNotes = "Evaluation failed."
            };
        }
    }

    public async Task<IReadOnlyList<PromptAdjustment>> GeneratePromptAdjustmentsAsync(
        Dictionary<Guid, List<EvaluationResult>> evaluationsByAgent,
        string? currentSystemPrompt,
        CancellationToken ct = default)
    {
        var prompt = BuildPromptAdjustmentPrompt(evaluationsByAgent, currentSystemPrompt);

        try
        {
            var result = await CallClaudeAsync(prompt, 4000, ct).ConfigureAwait(false);
            return ParsePromptAdjustments(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating prompt adjustments");
            return [];
        }
    }

    private string BuildEvaluationPrompt(LegalQuestion question, AgentResponse response)
    {
        var expectedElements = string.Join("\n", question.ExpectedElements.Select(e => $"- {e}"));

        return $"""
            You are evaluating a legal AI assistant's response to a legal question.

            ## Question Information
            - ID: {question.Id}
            - Category: {question.Category}
            - Difficulty: {question.Difficulty}
            - Question: {question.Question}

            ## Expected Elements
            The response should address these key points:
            {expectedElements}

            ## Response to Evaluate
            {response.Response}

            ## Evaluation Instructions
            Evaluate the response on the following dimensions (score 1-10 each):

            1. **Accuracy** (1-10): Is the legal information correct? Are citations accurate?
            2. **Completeness** (1-10): Does it cover the expected elements?
            3. **Clarity** (1-10): Is it well-organized and readable?
            4. **Source Quality** (1-10): Are citations authoritative and relevant?
            5. **Disclaimer Present** (true/false): Does it include appropriate legal disclaimers?

            Assign an overall grade: A+, A, A-, B+, B, B-, C+, C, C-, D, or F

            Respond with a JSON object in this exact format:
            ```json
            {"{"}
              "overall_grade": "A",
              "accuracy_score": 8,
              "completeness_score": 7,
              "clarity_score": 9,
              "source_quality_score": 8,
              "disclaimer_present": true,
              "issues_found": ["Issue 1", "Issue 2"],
              "strengths": ["Strength 1", "Strength 2"],
              "evaluation_notes": "Brief summary of the evaluation"
            {"}"}
            ```
            """;
    }

    private string BuildPromptAdjustmentPrompt(
        Dictionary<Guid, List<EvaluationResult>> evaluationsByAgent,
        string? currentSystemPrompt)
    {
        var issuesSummary = new List<string>();

        foreach (var (agentId, results) in evaluationsByAgent)
        {
            foreach (var result in results.Where(r => r.IssuesFound.Count > 0))
            {
                issuesSummary.AddRange(result.IssuesFound.Select(i =>
                    $"[{result.QuestionId}] {i}"));
            }
        }

        var promptPreview = currentSystemPrompt?.Length > 5000
            ? currentSystemPrompt[..5000] + "..."
            : currentSystemPrompt ?? "(No system prompt provided)";

        var issuesText = string.Join("\n", issuesSummary.Take(50));

        return $"""
            You are analyzing evaluation results from a legal AI validation test to recommend system prompt improvements.

            ## Issues Found Across All Evaluations
            {issuesText}

            ## Current System Prompt (first 5000 chars)
            {promptPreview}

            ## Instructions
            Based on the issues found, generate 5-10 specific, actionable prompt adjustments.

            For each adjustment, provide:
            1. Priority: "critical", "high", "medium", or "low"
            2. Category: e.g., "citation_accuracy", "disclaimer_consistency", "scope_precision"
            3. Issue description
            4. Current behavior
            5. Desired behavior
            6. Suggested prompt text to add
            7. Implementation instructions

            Respond with a JSON array:
            ```json
            [
              {"{"}"priority": "critical", "category": "citation_accuracy", "issue_description": "Description", "current_behavior": "Current", "desired_behavior": "Desired", "suggested_prompt_addition": "Text", "affected_question_ids": ["FD001"], "implementation_instructions": "Instructions"{"}"}
            ]
            ```
            """;
    }

    private async Task<string> CallClaudeAsync(string prompt, int maxTokens, CancellationToken ct)
    {
        var request = new
        {
            model = _options.Model,
            max_tokens = maxTokens,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            "https://api.anthropic.com/v1/messages",
            request,
            JsonOptions,
            ct).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ClaudeResponse>(JsonOptions, ct)
            .ConfigureAwait(false);

        return result?.Content?.FirstOrDefault()?.Text ?? string.Empty;
    }

    private EvaluationResult ParseEvaluationResult(
        string response,
        string questionId,
        Guid agentId,
        string agentName)
    {
        try
        {
            var json = ExtractJson(response);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new EvaluationResult
            {
                QuestionId = questionId,
                AgentId = agentId,
                AgentName = agentName,
                OverallGrade = ParseGrade(root.GetProperty("overall_grade").GetString()),
                AccuracyScore = root.GetProperty("accuracy_score").GetInt32(),
                CompletenessScore = root.GetProperty("completeness_score").GetInt32(),
                ClarityScore = root.GetProperty("clarity_score").GetInt32(),
                SourceQualityScore = root.GetProperty("source_quality_score").GetInt32(),
                DisclaimerPresent = root.GetProperty("disclaimer_present").GetBoolean(),
                IssuesFound = ParseStringArray(root, "issues_found"),
                Strengths = ParseStringArray(root, "strengths"),
                EvaluationNotes = root.TryGetProperty("evaluation_notes", out var notes)
                    ? notes.GetString() ?? string.Empty
                    : string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse evaluation result JSON");

            return new EvaluationResult
            {
                QuestionId = questionId,
                AgentId = agentId,
                AgentName = agentName,
                OverallGrade = OverallGrade.C,
                AccuracyScore = 5,
                CompletenessScore = 5,
                ClarityScore = 5,
                SourceQualityScore = 5,
                DisclaimerPresent = false,
                IssuesFound = ["Failed to parse evaluation response"],
                EvaluationNotes = $"Parse error: {ex.Message}"
            };
        }
    }

    private IReadOnlyList<PromptAdjustment> ParsePromptAdjustments(string response)
    {
        try
        {
            var json = ExtractJson(response);
            return JsonSerializer.Deserialize<List<PromptAdjustment>>(json, JsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse prompt adjustments JSON");
            return [];
        }
    }

    private static string ExtractJson(string response)
    {
        // Try to extract JSON from markdown code blocks
        var match = JsonBlockRegex().Match(response);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        // Try plain JSON extraction
        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');

        if (start >= 0 && end > start)
        {
            return response[start..(end + 1)];
        }

        // Try array
        start = response.IndexOf('[');
        end = response.LastIndexOf(']');

        if (start >= 0 && end > start)
        {
            return response[start..(end + 1)];
        }

        return response;
    }

    private static OverallGrade ParseGrade(string? grade)
    {
        return grade?.Trim().ToUpperInvariant() switch
        {
            "A+" => OverallGrade.APlus,
            "A" => OverallGrade.A,
            "A-" => OverallGrade.AMinus,
            "B+" => OverallGrade.BPlus,
            "B" => OverallGrade.B,
            "B-" => OverallGrade.BMinus,
            "C+" => OverallGrade.CPlus,
            "C" => OverallGrade.C,
            "C-" => OverallGrade.CMinus,
            "D" => OverallGrade.D,
            _ => OverallGrade.F
        };
    }

    private static List<string> ParseStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var prop))
            return [];

        return prop.EnumerateArray()
            .Select(e => e.GetString() ?? string.Empty)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }

    [GeneratedRegex(@"```(?:json)?\s*([\s\S]*?)```", RegexOptions.Compiled)]
    private static partial Regex JsonBlockRegex();
}

internal class ClaudeResponse
{
    public List<ClaudeContent>? Content { get; set; }
}

internal class ClaudeContent
{
    public string? Type { get; set; }
    public string? Text { get; set; }
}
