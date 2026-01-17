using Aesir.Infrastructure.Concurrency;
using Aesir.Infrastructure.Documents;
using Aesir.Infrastructure.Modules;
using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Execution;
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

        // Register document export services (PDF, Word)
        services.AddDocumentExportServices();

        // Register concurrent execution infrastructure (reusable across modules)
        services.AddScoped(typeof(IConcurrentExecutor<,>), typeof(BatchedConcurrentExecutor<,>));

        // Register repositories
        services.AddScoped<IResearchTeamRepository, ResearchTeamRepository>();
        services.AddScoped<IResearchSessionRepository, ResearchSessionRepository>();

        // Register team configuration services
        services.AddScoped<IResearchTeamService, ResearchTeamService>();

        // Register agent orchestration services
        services.AddScoped<IResearchAgentFactory, ResearchAgentFactory>();
        services.AddScoped<IChatRequestBuilder, ChatRequestBuilder>();
        services.AddScoped<IClarificationService, ClarificationService>();
        services.AddScoped<IChairmanPlanningService, ChairmanPlanningService>();

        // Register parallel execution strategies with retry policies
        services.AddScoped<IPhaseExecutionStrategyFactory, PhaseExecutionStrategyFactory>();

        // Register anonymization and peer review services
        services.AddScoped<IAnonymizationService, AnonymizationService>();
        services.AddScoped<IScoringCalculator, ScoringCalculator>();
        services.AddScoped<IPeerReviewService, PeerReviewService>();

        // Register synthesis and report generation services
        services.AddScoped<IConfidenceCalculator, ConfidenceCalculator>();
        services.AddScoped<IReportGeneratorService, ReportGeneratorService>();

        // Register report exporter (depends on document export services)
        services.AddScoped<IResearchReportExporter, ResearchReportExporter>();

        // Register phase executor (depends on all phase services)
        services.AddScoped<IResearchPhaseExecutor, ResearchPhaseExecutor>();

        // Register orchestrator and progress broadcaster
        services.AddScoped<IResearchOrchestrator, ResearchOrchestrator>();
        services.AddScoped<IResearchProgressBroadcaster, ResearchProgressBroadcaster>();

        Log("Research services registered successfully");

        return Task.CompletedTask;
    }
}
