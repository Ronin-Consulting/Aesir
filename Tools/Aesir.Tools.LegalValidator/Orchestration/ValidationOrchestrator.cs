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

        _logger.LogInformation("Starting legal validation run");

        // 1. Get agents to test
        var agents = await GetAgentsToTestAsync(agentIds, agentNamePattern, ct).ConfigureAwait(false);

        if (agents.Count == 0)
        {
            _logger.LogWarning("No agents found to test");
            return CreateEmptyReport(stopwatch.Elapsed.TotalSeconds);
        }

        _logger.LogInformation("Testing {Count} agents: {Names}",
            agents.Count, string.Join(", ", agents.Select(a => a.Name)));

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

        _logger.LogInformation("Loaded {Count} questions", questions.Count);

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

        foreach (var agent in agents)
        {
            _logger.LogInformation("Testing agent: {Name} ({Id})", agent.Name, agent.Id);

            var agentResponses = await RunQuestionsForAgentAsync(
                agent, questions, customSystemPrompt, concurrency, ct).ConfigureAwait(false);

            responses.AddRange(agentResponses);

            // Evaluate responses
            var evaluations = await EvaluateResponsesAsync(questions, agentResponses, ct)
                .ConfigureAwait(false);

            resultsByAgent[agent.Id] = evaluations;

            _logger.LogInformation("Completed evaluation for agent {Name}: {Count} results",
                agent.Name, evaluations.Count);
        }

        // 5. Generate prompt adjustments
        var adjustments = await _claudeEvaluator.GeneratePromptAdjustmentsAsync(
            resultsByAgent, customSystemPrompt, ct).ConfigureAwait(false);

        // 6. Calculate summaries
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

                    _logger.LogDebug("Received response for question {QuestionId}", question.Id);
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
        CancellationToken ct)
    {
        var evaluations = new List<EvaluationResult>();
        var questionMap = questions.ToDictionary(q => q.Id);

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
