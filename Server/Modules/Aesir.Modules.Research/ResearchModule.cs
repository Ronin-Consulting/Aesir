using Aesir.Infrastructure.Modules;
using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research;

/// <summary>
/// Research module providing multi-agent research orchestration with peer review and synthesis.
/// Enables complex research workflows with specialized agent roles, quality assurance through
/// peer review, and automated report generation.
/// </summary>
public class ResearchModule : ModuleBase
{
    public ResearchModule(ILogger logger) : base(logger)
    {
    }

    public override string Name => "Research";

    public override string Version => "1.0.0";

    public override string? Description => "Multi-agent research orchestration with peer review and synthesis";

    public override Task RegisterServicesAsync(IServiceCollection services)
    {
        Log("Registering research services...");

        // Register repositories
        services.AddScoped<IResearchTeamRepository, ResearchTeamRepository>();
        services.AddScoped<IResearchSessionRepository, ResearchSessionRepository>();

        // Register team configuration services
        services.AddScoped<IResearchTeamService, ResearchTeamService>();

        // Register agent orchestration services
        services.AddScoped<IResearchAgentFactory, ResearchAgentFactory>();
        services.AddScoped<IClarificationService, ClarificationService>();

        // Register anonymization and peer review services
        services.AddScoped<IAnonymizationService, AnonymizationService>();
        services.AddScoped<IScoringCalculator, ScoringCalculator>();
        services.AddScoped<IPeerReviewService, PeerReviewService>();

        // Register phase executor (depends on anonymization and peer review)
        services.AddScoped<IResearchPhaseExecutor, ResearchPhaseExecutor>();

        // Note: ResearchOrchestrator requires external dependencies (agent resolver, chat service resolver)
        // and should be registered by the API server with appropriate factories

        Log("Research services registered successfully");

        return Task.CompletedTask;
    }
}
