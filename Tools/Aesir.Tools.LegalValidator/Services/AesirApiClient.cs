using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Aesir.Common.Models;
using Aesir.Common.Prompts;
using Aesir.Tools.LegalValidator.Configuration;
using Aesir.Tools.LegalValidator.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aesir.Tools.LegalValidator.Services;

/// <summary>
/// Implementation of AESIR API client.
/// </summary>
public class AesirApiClient : IAesirApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AesirApiClient> _logger;
    private readonly LegalValidatorOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public AesirApiClient(
        HttpClient httpClient,
        ILogger<AesirApiClient> logger,
        IOptions<LegalValidatorOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<AgentInfo>> GetAgentsAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching agents from AESIR API");

        var response = await _httpClient.GetAsync("/configuration/agents", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var agents = await response.Content.ReadFromJsonAsync<List<AgentInfo>>(JsonOptions, ct)
            .ConfigureAwait(false);

        _logger.LogInformation("Retrieved {Count} agents from AESIR API", agents?.Count ?? 0);
        return agents ?? [];
    }

    public async Task<AgentInfo?> GetAgentAsync(Guid agentId, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching agent {AgentId} from AESIR API", agentId);

        var response = await _httpClient.GetAsync($"/configuration/agents/{agentId}", ct)
            .ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AgentInfo>(JsonOptions, ct)
            .ConfigureAwait(false);
    }

    public async Task<AgentResponse> SendQuestionAsync(
        Guid agentId,
        LegalQuestion question,
        string? customSystemPrompt = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Sending question {QuestionId} to agent {AgentId}", question.Id, agentId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Get agent info for the name
            var agentInfo = await GetAgentAsync(agentId, ct).ConfigureAwait(false);
            var agentName = agentInfo?.Name ?? agentId.ToString();

            // Build the request
            var request = new AesirAgentChatRequest
            {
                AgentId = agentId,
                ChatSessionId = null,
                Title = $"Legal Validation: {question.Id}",
                User = "legal-validator",
                ClientDateTime = DateTimeOffset.Now.ToString("F"),
                ChatSessionUpdatedAt = DateTimeOffset.Now,
                Conversation = new AesirConversation
                {
                    Id = Guid.NewGuid().ToString(),
                    Messages =
                    [
                        AesirChatMessage.NewUserMessage(question.Question)
                    ]
                },
                EnableThinking = false,
                Tools = []
            };

            // Apply custom system prompt if provided
            if (!string.IsNullOrEmpty(customSystemPrompt))
            {
                request.ChatPromptPersona = PromptPersona.Custom;
                request.ChatCustomPromptContent = customSystemPrompt;
            }

            var response = await _httpClient.PostAsJsonAsync(
                "/chat/completions/agent",
                request,
                JsonOptions,
                ct).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AesirChatResult>(JsonOptions, ct)
                .ConfigureAwait(false);

            stopwatch.Stop();

            // Extract assistant response
            var assistantMessage = result?.AesirConversation?.Messages
                .LastOrDefault(m => m.Role == "assistant");

            return new AgentResponse
            {
                AgentId = agentId,
                AgentName = agentName,
                QuestionId = question.Id,
                QuestionText = question.Question,
                Response = assistantMessage?.Content ?? string.Empty,
                ResponseTimeSeconds = stopwatch.Elapsed.TotalSeconds,
                Timestamp = DateTimeOffset.Now,
                PromptTokens = result?.PromptTokens ?? 0,
                CompletionTokens = result?.CompletionTokens ?? 0
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error sending question {QuestionId} to agent {AgentId}",
                question.Id, agentId);

            return new AgentResponse
            {
                AgentId = agentId,
                AgentName = agentId.ToString(),
                QuestionId = question.Id,
                QuestionText = question.Question,
                Response = string.Empty,
                ResponseTimeSeconds = stopwatch.Elapsed.TotalSeconds,
                Timestamp = DateTimeOffset.Now,
                Error = ex.Message
            };
        }
    }
}

/// <summary>
/// Request model for agent chat completions.
/// </summary>
internal class AesirAgentChatRequest : AesirChatRequestBase
{
    [System.Text.Json.Serialization.JsonPropertyName("agent_id")]
    public Guid AgentId { get; set; }
}
