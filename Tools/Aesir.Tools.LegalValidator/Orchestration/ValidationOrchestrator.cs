using System.Diagnostics;
using System.Text.RegularExpressions;
using Aesir.Tools.LegalValidator.Configuration;
using Aesir.Tools.LegalValidator.Models;
using Aesir.Tools.LegalValidator.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aesir.Tools.LegalValidator.Orchestration;

/// <summary>
/// Implementation of the validation orchestration workflow.
/// </summary>
public partial class ValidationOrchestrator : IValidationOrchestrator
{
    private readonly IAesirApiClient _aesirClient;
    private readonly IClaudeEvaluator _claudeEvaluator;
    private readonly IQuestionLoader _questionLoader;
    private readonly IReportGenerator _reportGenerator;
    private readonly ILogger<ValidationOrchestrator> _logger;
    private readonly LegalValidatorOptions _options;

    // Progress tracking fields
    private int _totalQuestionsCompleted;
    private int _totalQuestionsToRun;

    public ValidationOrchestrator(
        IAesirApiClient aesirClient,
        IClaudeEvaluator claudeEvaluator,
        IQuestionLoader questionLoader,
        IReportGenerator reportGenerator,
        ILogger<ValidationOrchestrator> logger,
        IOptions<LegalValidatorOptions> options)
    {
        _aesirClient = aesirClient;
        _claudeEvaluator = claudeEvaluator;
        _questionLoader = questionLoader;
        _reportGenerator = reportGenerator;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<ValidationReport> RunValidationAsync(
        IEnumerable<Guid>? agentIds = null,
        string? agentNamePattern = null,
        IEnumerable<QuestionCategory>? categories = null,
        string? customQuestionsPath = null,
        string? customSystemPrompt = null,
        int concurrency = 3,
        bool dryRun = false,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting Legal Validation Run");
        _logger.LogInformation("═══════════════════════════════════════════════════════════════");

        // 1. Get agents to test
        var agents = await GetAgentsToTestAsync(agentIds, agentNamePattern, ct).ConfigureAwait(false);

        if (agents.Count == 0)
        {
            _logger.LogWarning("No agents found to test");
            return CreateEmptyReport(stopwatch.Elapsed.TotalSeconds);
        }

        // 2. Load questions
        IReadOnlyList<LegalQuestion> questions;

        if (!string.IsNullOrEmpty(customQuestionsPath))
        {
            questions = await _questionLoader.LoadQuestionsFromFileAsync(
                customQuestionsPath, categories, ct).ConfigureAwait(false);
        }
        else
        {
            questions = await _questionLoader.LoadQuestionsAsync(categories, ct).ConfigureAwait(false);
        }

        _logger.LogInformation("[Phase 2/6] Question Loading: Loaded {Count} questions from {Source}",
            questions.Count, !string.IsNullOrEmpty(customQuestionsPath) ? "custom file" : "embedded resource");

        if (questions.Count == 0)
        {
            _logger.LogWarning("No questions to test");
            return CreateEmptyReport(stopwatch.Elapsed.TotalSeconds);
        }

        // 3. Dry run - just show what would be tested
        if (dryRun)
        {
            return CreateDryRunReport(agents, questions, stopwatch.Elapsed.TotalSeconds);
        }

        // 4. Run validation
        var responses = new List<AgentResponse>();
        var resultsByAgent = new Dictionary<Guid, List<EvaluationResult>>();

        // Initialize progress tracking
        _totalQuestionsToRun = agents.Count * questions.Count;
        _totalQuestionsCompleted = 0;

        _logger.LogInformation("[Phase 3/6] Question Execution: Running {Total} questions across {AgentCount} agents",
            _totalQuestionsToRun, agents.Count);

        var agentIndex = 0;
        foreach (var agent in agents)
        {
            agentIndex++;
            _logger.LogInformation("  Agent {AgentNum}/{AgentTotal}: {AgentName} - Starting {QuestionCount} questions",
                agentIndex, agents.Count, agent.Name, questions.Count);

            var agentResponses = await RunQuestionsForAgentAsync(
                agent, questions, customSystemPrompt, concurrency, ct).ConfigureAwait(false);

            responses.AddRange(agentResponses);

            // Evaluate responses for this agent
            _logger.LogInformation("[Phase 4/6] Response Evaluation: Evaluating {Count} responses for {AgentName}",
                agentResponses.Count, agent.Name);

            var evaluations = await EvaluateResponsesAsync(questions, agentResponses, agent.Name, ct)
                .ConfigureAwait(false);

            resultsByAgent[agent.Id] = evaluations;

            _logger.LogInformation("  Completed evaluation for {AgentName}: {Count} results",
                agent.Name, evaluations.Count);
        }

        // 5. Generate prompt adjustments
        var allIssues = resultsByAgent.Values.SelectMany(e => e).SelectMany(e => e.IssuesFound).ToList();
        _logger.LogInformation("[Phase 5/6] Prompt Adjustments: Analyzing {IssueCount} issues to generate recommendations",
            allIssues.Count);

        var adjustments = await _claudeEvaluator.GeneratePromptAdjustmentsAsync(
            resultsByAgent, customSystemPrompt, ct).ConfigureAwait(false);

        _logger.LogInformation("  Generated {Count} prompt adjustment recommendations", adjustments.Count());

        // 6. Calculate summaries
        _logger.LogInformation("[Phase 6/6] Summary: Calculating final results");

        var summaries = new Dictionary<Guid, AgentSummary>();
        foreach (var agent in agents)
        {
            var agentResponses = responses.Where(r => r.AgentId == agent.Id).ToList();
            var agentResults = resultsByAgent.TryGetValue(agent.Id, out var results)
                ? results : [];

            summaries[agent.Id] = _reportGenerator.CalculateAgentSummary(
                agent.Id, agent.Name, agentResults, agentResponses);
        }

        stopwatch.Stop();

        var totalEvaluations = resultsByAgent.Values.Sum(r => r.Count);
        _logger.LogInformation("Validation Complete: {AgentCount} agents, {QuestionCount} questions, {EvalCount} evaluations in {Duration:F1}s",
            agents.Count, questions.Count, totalEvaluations, stopwatch.Elapsed.TotalSeconds);

        // 7. Build report
        return new ValidationReport
        {
            Timestamp = DateTimeOffset.Now,
            AgentsTested = agents.Select(a => a.Name).ToList(),
            TotalQuestions = questions.Count,
            CategoriesTested = questions.Select(q => q.Category).Distinct().ToList(),
            ResultsByAgent = resultsByAgent,
            AgentSummaries = summaries,
            RawResponses = responses,
            PromptAdjustments = adjustments.ToList(),
            SystemPromptUsed = customSystemPrompt,
            ValidationDurationSeconds = stopwatch.Elapsed.TotalSeconds
        };
    }

    private async Task<List<AgentInfo>> GetAgentsToTestAsync(
        IEnumerable<Guid>? agentIds,
        string? agentNamePattern,
        CancellationToken ct)
    {
        var allAgents = await _aesirClient.GetAgentsAsync(ct).ConfigureAwait(false);

        var agents = allAgents.ToList();
        var totalCount = agents.Count;

        // Filter by IDs
        if (agentIds != null)
        {
            var idSet = agentIds.ToHashSet();
            agents = agents.Where(a => idSet.Contains(a.Id)).ToList();
        }

        // Filter by name pattern
        if (!string.IsNullOrEmpty(agentNamePattern))
        {
            var pattern = WildcardToRegex(agentNamePattern);
            agents = agents.Where(a => pattern.IsMatch(a.Name)).ToList();
        }

        _logger.LogInformation("[Phase 1/6] Agent Discovery: Found {Total} agents, {Matched} matched filter criteria",
            totalCount, agents.Count);

        return agents;
    }

    private async Task<List<AgentResponse>> RunQuestionsForAgentAsync(
        AgentInfo agent,
        IReadOnlyList<LegalQuestion> questions,
        string? customSystemPrompt,
        int concurrency,
        CancellationToken ct)
    {
        var responses = new List<AgentResponse>();
        var semaphore = new SemaphoreSlim(concurrency);
        var tasks = new List<Task<AgentResponse>>();

        foreach (var question in questions)
        {
            await semaphore.WaitAsync(ct).ConfigureAwait(false);

            var task = Task.Run(async () =>
            {
                try
                {
                    var response = await _aesirClient.SendQuestionAsync(
                        agent.Id, question, customSystemPrompt, ct).ConfigureAwait(false);

                    // Log progress
                    var completed = Interlocked.Increment(ref _totalQuestionsCompleted);
                    _logger.LogInformation("    [{Completed}/{Total}] ({Percent:F1}%) {QuestionId} completed in {Time:F1}s",
                        completed, _totalQuestionsToRun, completed * 100.0 / _totalQuestionsToRun,
                        question.Id, response.ResponseTimeSeconds);

                    return response;
                }
                finally
                {
                    semaphore.Release();
                }
            }, ct);

            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        responses.AddRange(results);

        return responses;
    }

    private async Task<List<EvaluationResult>> EvaluateResponsesAsync(
        IReadOnlyList<LegalQuestion> questions,
        List<AgentResponse> responses,
        string agentName,
        CancellationToken ct)
    {
        var evaluations = new List<EvaluationResult>();
        var questionMap = questions.ToDictionary(q => q.Id);
        var total = responses.Count;
        var completed = 0;

        foreach (var response in responses)
        {
            if (!questionMap.TryGetValue(response.QuestionId, out var question))
            {
                _logger.LogWarning("Question {Id} not found for evaluation", response.QuestionId);
                continue;
            }

            var evaluation = await _claudeEvaluator.EvaluateResponseAsync(question, response, ct)
                .ConfigureAwait(false);

            evaluations.Add(evaluation);

            completed++;
            _logger.LogInformation("    [{Completed}/{Total}] ({Percent:F1}%) Evaluated {QuestionId}",
                completed, total, completed * 100.0 / total, response.QuestionId);
        }

        return evaluations;
    }

    private static ValidationReport CreateEmptyReport(double durationSeconds)
    {
        return new ValidationReport
        {
            Timestamp = DateTimeOffset.Now,
            ValidationDurationSeconds = durationSeconds
        };
    }

    private static ValidationReport CreateDryRunReport(
        List<AgentInfo> agents,
        IReadOnlyList<LegalQuestion> questions,
        double durationSeconds)
    {
        return new ValidationReport
        {
            Timestamp = DateTimeOffset.Now,
            AgentsTested = agents.Select(a => $"{a.Name} (dry-run)").ToList(),
            TotalQuestions = questions.Count,
            CategoriesTested = questions.Select(q => q.Category).Distinct().ToList(),
            ValidationDurationSeconds = durationSeconds
        };
    }

    private static Regex WildcardToRegex(string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return new Regex(regexPattern, RegexOptions.IgnoreCase);
    }
}
