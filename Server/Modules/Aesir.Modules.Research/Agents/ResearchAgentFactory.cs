using Aesir.Common.Models;
using Aesir.Modules.Research.Models;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Agents;

/// <summary>
/// Factory for creating research agents with team configuration overrides applied.
/// </summary>
public interface IResearchAgentFactory
{
    /// <summary>
    /// Creates a configured research agent from a team member.
    /// </summary>
    /// <param name="teamMember">The team member configuration.</param>
    /// <param name="baseAgent">The base agent configuration.</param>
    /// <returns>A fully configured research agent.</returns>
    ResearchAgent CreateAgent(ResearchTeamMember teamMember, AesirAgentBase baseAgent);

    /// <summary>
    /// Creates agents for all active members of a research team.
    /// </summary>
    /// <param name="team">The research team.</param>
    /// <param name="agents">Dictionary of agent configurations by ID.</param>
    /// <returns>List of configured research agents.</returns>
    IReadOnlyList<ResearchAgent> CreateAgentsForTeam(
        ResearchTeam team,
        IReadOnlyDictionary<Guid, AesirAgentBase> agents);
}

/// <summary>
/// Factory implementation for creating research agents.
/// </summary>
public class ResearchAgentFactory : IResearchAgentFactory
{
    private readonly ILogger<ResearchAgentFactory> _logger;

    public ResearchAgentFactory(ILogger<ResearchAgentFactory> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public ResearchAgent CreateAgent(ResearchTeamMember teamMember, AesirAgentBase baseAgent)
    {
        // Get default config for the role
        var roleConfig = ResearchRoleDefinitions.GetConfig(teamMember.Role);

        // Apply overrides from team member configuration
        var agent = new ResearchAgent
        {
            TeamMemberId = teamMember.Id,
            Role = teamMember.Role,
            RoleName = roleConfig.Name,

            // Base agent provides model configuration
            BaseAgentId = teamMember.AgentId,
            InferenceEngineId = baseAgent.ChatInferenceEngineId,
            Model = baseAgent.ChatModel,
            MaxTokens = baseAgent.ChatMaxTokens,

            // Temperature: use override, else role default
            Temperature = teamMember.OverrideTemperature ?? roleConfig.Temperature,

            // Persona: use override, else role default
            Persona = !string.IsNullOrWhiteSpace(teamMember.OverridePersona)
                ? teamMember.OverridePersona
                : roleConfig.Persona,

            // Planning prompt: use override, else role default
            PlanningPrompt = !string.IsNullOrWhiteSpace(teamMember.OverridePlanningPrompt)
                ? teamMember.OverridePlanningPrompt
                : roleConfig.PlanningPrompt,

            // Research prompt: use override, else role default
            ResearchPrompt = !string.IsNullOrWhiteSpace(teamMember.OverrideResearchPrompt)
                ? teamMember.OverrideResearchPrompt
                : roleConfig.ResearchPrompt,

            // Chairman-specific prompts (no override supported currently)
            ClarificationPrompt = roleConfig.ClarificationPrompt,
            SynthesisPrompt = roleConfig.SynthesisPrompt,

            // Thinking mode: use override if specified
            ThinkingMode = teamMember.OverrideThinkingMode,

            // Tools: use override if specified, else use base agent's tools
            ToolIds = teamMember.OverrideTools?.ToList()
        };

        _logger.LogDebug(
            "Created research agent for role {Role} with temperature {Temperature}",
            agent.Role,
            agent.Temperature);

        return agent;
    }

    /// <inheritdoc />
    public IReadOnlyList<ResearchAgent> CreateAgentsForTeam(
        ResearchTeam team,
        IReadOnlyDictionary<Guid, AesirAgentBase> agents)
    {
        var researchAgents = new List<ResearchAgent>();

        if (team.Members == null || team.Members.Count == 0)
        {
            _logger.LogWarning("Research team {TeamId} has no members", team.Id);
            return researchAgents;
        }

        foreach (var member in team.Members.Where(m => m.IsActive))
        {
            if (!agents.TryGetValue(member.AgentId, out var baseAgent))
            {
                _logger.LogWarning(
                    "Base agent {AgentId} not found for team member {MemberId}",
                    member.AgentId,
                    member.Id);
                continue;
            }

            var researchAgent = CreateAgent(member, baseAgent);
            researchAgents.Add(researchAgent);
        }

        _logger.LogInformation(
            "Created {Count} research agents for team {TeamId}",
            researchAgents.Count,
            team.Id);

        return researchAgents;
    }
}

/// <summary>
/// A fully configured research agent ready for execution.
/// </summary>
public class ResearchAgent
{
    /// <summary>
    /// The team member ID this agent is based on.
    /// </summary>
    public Guid TeamMemberId { get; set; }

    /// <summary>
    /// The research role.
    /// </summary>
    public ResearchRole Role { get; set; }

    /// <summary>
    /// Display name for the role.
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// The base agent ID providing model configuration.
    /// </summary>
    public Guid BaseAgentId { get; set; }

    /// <summary>
    /// The inference engine ID to use.
    /// </summary>
    public Guid? InferenceEngineId { get; set; }

    /// <summary>
    /// The model to use for inference.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Temperature for inference (0.0 - 2.0).
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// Maximum tokens for inference.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// The system persona prompt.
    /// </summary>
    public string Persona { get; set; } = string.Empty;

    /// <summary>
    /// Prompt template for planning phase.
    /// </summary>
    public string? PlanningPrompt { get; set; }

    /// <summary>
    /// Prompt template for research phase.
    /// </summary>
    public string? ResearchPrompt { get; set; }

    /// <summary>
    /// Prompt template for clarification (Chairman only).
    /// </summary>
    public string? ClarificationPrompt { get; set; }

    /// <summary>
    /// Prompt template for synthesis (Chairman only).
    /// </summary>
    public string? SynthesisPrompt { get; set; }

    /// <summary>
    /// Thinking mode override (null = use default).
    /// </summary>
    public string? ThinkingMode { get; set; }

    /// <summary>
    /// Tool IDs to use (null = use base agent's tools).
    /// </summary>
    public List<string>? ToolIds { get; set; }

    /// <summary>
    /// Gets whether this agent is the Chairman role.
    /// </summary>
    public bool IsChairman => Role == ResearchRole.Chairman;
}
