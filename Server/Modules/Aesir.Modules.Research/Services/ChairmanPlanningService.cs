using System.Text;
using System.Text.RegularExpressions;
using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Service for Chairman to create a single unified research plan for all agents.
/// </summary>
public class ChairmanPlanningService : IChairmanPlanningService
{
    private readonly ILogger<ChairmanPlanningService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfigurationService _configurationService;
    private readonly IResearchProgressBroadcaster _progressBroadcaster;

    public ChairmanPlanningService(
        ILogger<ChairmanPlanningService> logger,
        IServiceProvider serviceProvider,
        IConfigurationService configurationService,
        IResearchProgressBroadcaster progressBroadcaster)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configurationService = configurationService;
        _progressBroadcaster = progressBroadcaster;
    }

    public async Task<Dictionary<Guid, string>> CreateUnifiedPlanAsync(
        ResearchSession session,
        ResearchAgent chairman,
        IReadOnlyList<ResearchAgent> teamAgents,
        string refinedQuery,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Chairman creating unified research plan for {Count} agents in session {SessionId}",
            teamAgents.Count, session.Id);

        await _progressBroadcaster.BroadcastProgressAsync(session.Id, new ResearchPhaseProgress
        {
            Phase = ResearchPhase.Planning,
            AgentRole = ResearchRole.Chairman,
            Message = "Chairman is creating unified research plan...",
            PercentComplete = 10
        }).ConfigureAwait(false);

        // Build the Chairman's planning prompt
        var prompt = BuildUnifiedPlanningPrompt(refinedQuery, teamAgents);

        // Execute Chairman's planning LLM call
        var planResponse = await ExecuteChairmanPlanningAsync(
            chairman, prompt, cancellationToken).ConfigureAwait(false);

        // Parse the response to extract sub-plans for each agent
        var agentPlans = ParseUnifiedPlan(planResponse, teamAgents);

        _logger.LogInformation(
            "Chairman created unified plan: {Count} agent sub-plans generated",
            agentPlans.Count);

        await _progressBroadcaster.BroadcastProgressAsync(session.Id, new ResearchPhaseProgress
        {
            Phase = ResearchPhase.Planning,
            AgentRole = ResearchRole.Chairman,
            Message = "Unified research plan created",
            PercentComplete = 100,
            IsComplete = true
        }).ConfigureAwait(false);

        return agentPlans;
    }

    private string BuildUnifiedPlanningPrompt(string query, IReadOnlyList<ResearchAgent> teamAgents)
    {
        var agentDescriptions = new StringBuilder();
        foreach (var agent in teamAgents)
        {
            agentDescriptions.AppendLine($"- **{agent.RoleName}** ({agent.Role}): {GetRoleDescription(agent.Role)}");
        }

        return $$"""
            You are the Chairman of a research team investigating: {{query}}

            Your team consists of:
            {{agentDescriptions}}

            ## Your Task
            Create ONE unified research plan that assigns specific tasks to each team member based on their roles.

            The plan should:
            1. Break down the research query into logical sub-topics
            2. Assign each sub-topic to the most appropriate team member
            3. Ensure comprehensive coverage without significant overlap
            4. Provide clear guidance for each agent's research focus

            ## Output Format
            Structure your response with clear sections for each team member:

            ### Deep Diver
            [Specific research tasks, questions to answer, and approach for exhaustive investigation]

            ### Synthesizer
            [Specific research tasks, questions to answer, and approach for pattern recognition]

            ### Devil's Advocate
            [Specific research tasks, questions to answer, and approach for critical analysis]

            ---

            Begin your unified research plan:
            """;
    }

    private async Task<string> ExecuteChairmanPlanningAsync(
        ResearchAgent chairman,
        string prompt,
        CancellationToken cancellationToken)
    {
        if (!chairman.InferenceEngineId.HasValue)
        {
            throw new InvalidOperationException("Chairman must have an inference engine configured");
        }

        var chatService = _serviceProvider.GetKeyedService<IChatService>(
            chairman.InferenceEngineId.Value.ToString());

        if (chatService == null)
        {
            throw new InvalidOperationException(
                $"No IChatService found for Chairman's inference engine {chairman.InferenceEngineId}");
        }

        var request = await CreateChatRequestAsync(chairman, prompt);
        var result = await chatService.ChatCompletionsAsync(request).ConfigureAwait(false);

        return result.AesirConversation?.Messages?
            .LastOrDefault(m => m.Role == "assistant")?.Content ?? string.Empty;
    }

    private Dictionary<Guid, string> ParseUnifiedPlan(string planResponse, IReadOnlyList<ResearchAgent> teamAgents)
    {
        var plans = new Dictionary<Guid, string>();

        // Parse the response to extract each agent's section
        foreach (var agent in teamAgents)
        {
            var rolePattern = $"### {agent.RoleName}";
            var startIndex = planResponse.IndexOf(rolePattern, StringComparison.OrdinalIgnoreCase);

            if (startIndex >= 0)
            {
                // Extract text from this role header until the next role header or end
                var nextIndex = planResponse.IndexOf("###", startIndex + rolePattern.Length);
                var endIndex = nextIndex > 0 ? nextIndex : planResponse.Length;

                var subPlan = planResponse.Substring(startIndex, endIndex - startIndex).Trim();
                plans[agent.TeamMemberId] = subPlan;

                _logger.LogDebug("Extracted sub-plan for {Role}: {Length} characters",
                    agent.Role, subPlan.Length);
            }
            else
            {
                // Fallback: try alternate formats like "**Deep Diver**" or role enum name
                var alternatePatterns = new[]
                {
                    $"**{agent.RoleName}**",
                    $"## {agent.RoleName}",
                    $"{agent.Role}:"
                };

                foreach (var altPattern in alternatePatterns)
                {
                    startIndex = planResponse.IndexOf(altPattern, StringComparison.OrdinalIgnoreCase);
                    if (startIndex >= 0)
                    {
                        // Extract until next section or end
                        var markers = new[] { "###", "##", "**Deep Diver**", "**Synthesizer**", "**Devil's Advocate**" };
                        var endIndex = planResponse.Length;
                        foreach (var marker in markers)
                        {
                            var markerIndex = planResponse.IndexOf(marker, startIndex + altPattern.Length);
                            if (markerIndex > 0 && markerIndex < endIndex)
                            {
                                endIndex = markerIndex;
                            }
                        }

                        var subPlan = planResponse.Substring(startIndex, endIndex - startIndex).Trim();
                        plans[agent.TeamMemberId] = subPlan;
                        _logger.LogDebug("Extracted sub-plan for {Role} using alternate pattern: {Length} characters",
                            agent.Role, subPlan.Length);
                        break;
                    }
                }

                // Final fallback: assign generic plan
                if (!plans.ContainsKey(agent.TeamMemberId))
                {
                    _logger.LogWarning("Could not find sub-plan for {Role} in Chairman's response", agent.Role);
                    plans[agent.TeamMemberId] = $"Research: {agent.RoleName} should investigate relevant aspects of the query using their specialized approach.";
                }
            }
        }

        return plans;
    }

    private async Task<AesirChatRequestBase> CreateChatRequestAsync(ResearchAgent chairman, string prompt)
    {
        var systemMessage = new AesirChatMessage
        {
            Role = "system",
            Content = chairman.Persona ?? "You are a research chairman coordinating a research team.",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var userMessage = new AesirChatMessage
        {
            Role = "user",
            Content = prompt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var conversation = new AesirConversation
        {
            Id = Guid.NewGuid().ToString(),
            Messages = [systemMessage, userMessage]
        };

        // Get thinking settings from base agent
        bool? enableThinking = null;
        ThinkValue? thinkValue = null;

        try
        {
            var baseAgent = await _configurationService.GetAgentAsync(chairman.BaseAgentId);
            enableThinking = baseAgent.AllowThinking;
            thinkValue = baseAgent.ThinkValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load Chairman base agent settings");
        }

        return new AesirChatRequestBase
        {
            Model = chairman.Model ?? "gpt-4",
            Temperature = chairman.Temperature,
            MaxTokens = chairman.MaxTokens ?? 8192,
            Conversation = conversation,
            User = "research-chairman-planning",
            Title = "Chairman Unified Planning",
            Tools = [], // Chairman doesn't need tools for planning
            EnableThinking = enableThinking,
            ThinkValue = thinkValue
        };
    }

    private static string GetRoleDescription(ResearchRole role)
    {
        return role switch
        {
            ResearchRole.DeepDiver => "Exhaustive investigation specialist - digs deep into sources",
            ResearchRole.Synthesizer => "Pattern recognition specialist - connects disparate findings",
            ResearchRole.DevilsAdvocate => "Critical analysis specialist - challenges assumptions and finds gaps",
            _ => "Research specialist"
        };
    }
}