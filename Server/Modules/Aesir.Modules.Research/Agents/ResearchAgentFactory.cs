using Aesir.Common.Models;
using Aesir.Common.Prompts;
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

        // Build merged persona: domain expertise (base agent) + research methodology (role)
        var mergedPersona = BuildMergedPersona(baseAgent, roleConfig);

        // Apply temperature override from team member, use role defaults for other settings
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

            // Temperature: use override (Creativity setting), else role default
            Temperature = teamMember.OverrideTemperature ?? roleConfig.Temperature,

            // Merged persona combines domain expertise with research role methodology
            Persona = mergedPersona,

            // Role-specific prompts (no overrides - uses role defaults)
            PlanningPrompt = roleConfig.PlanningPrompt,
            ResearchPrompt = roleConfig.ResearchPrompt,
            ClarificationPrompt = roleConfig.ClarificationPrompt,
            SynthesisPrompt = roleConfig.SynthesisPrompt
        };

        _logger.LogDebug(
            "Created research agent for role {Role} with temperature {Temperature}, merged persona length: {PersonaLength}",
            agent.Role,
            agent.Temperature,
            mergedPersona.Length);

        return agent;
    }

    /// <summary>
    /// Builds a merged persona combining base agent domain expertise with research role methodology.
    /// </summary>
    /// <remarks>
    /// The merged persona follows a layered architecture:
    /// 1. Domain Expertise (base agent) - establishes identity, terminology, and constraints
    /// 2. Research Role (role config) - defines research methodology and approach
    ///
    /// This ensures that domain-specific constraints (e.g., legal confidentiality, military precision)
    /// are established before task-specific research instructions.
    /// </remarks>
    private string BuildMergedPersona(AesirAgentBase baseAgent, ResearchAgentConfig roleConfig)
    {
        // Get the base agent's domain expertise prompt
        var domainExpertisePrompt = GetDomainExpertisePrompt(baseAgent);

        // If no domain expertise configured, use research role persona only
        if (string.IsNullOrWhiteSpace(domainExpertisePrompt))
        {
            _logger.LogDebug(
                "No domain expertise configured for agent {AgentId}, using research role only",
                baseAgent.Id);
            return roleConfig.Persona;
        }

        // Merge: Domain expertise first, then research role methodology
        var mergedPersona = $"""
            {domainExpertisePrompt}

            ---

            ## Research Role: {roleConfig.Name}

            {roleConfig.Persona}
            """;

        _logger.LogDebug(
            "Merged persona for agent {AgentId}: domain expertise ({DomainLength} chars) + {RoleName} ({RoleLength} chars)",
            baseAgent.Id,
            domainExpertisePrompt.Length,
            roleConfig.Name,
            roleConfig.Persona.Length);

        return mergedPersona;
    }

    /// <summary>
    /// Gets the domain expertise prompt from the base agent's configured persona.
    /// </summary>
    private string GetDomainExpertisePrompt(AesirAgentBase baseAgent)
    {
        // No persona configured - no domain expertise
        if (baseAgent.ChatPromptPersona == null || baseAgent.ChatPromptPersona == PromptPersona.Default)
        {
            return string.Empty;
        }

        // Custom persona - use the custom prompt content directly
        if (baseAgent.ChatPromptPersona == PromptPersona.Custom)
        {
            return baseAgent.ChatCustomPromptContent ?? string.Empty;
        }

        // Standard personas (Legal, Military, Business, Ocr) - resolve via DefaultPromptProvider
        try
        {
            var promptTemplate = DefaultPromptProvider.Instance.GetSystemPrompt(baseAgent.ChatPromptPersona.Value);
            return promptTemplate.Content;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to resolve domain expertise prompt for persona {Persona}, falling back to empty",
                baseAgent.ChatPromptPersona);
            return string.Empty;
        }
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
    /// Gets whether this agent is the Chairman role.
    /// </summary>
    public bool IsChairman => Role == ResearchRole.Chairman;
}
