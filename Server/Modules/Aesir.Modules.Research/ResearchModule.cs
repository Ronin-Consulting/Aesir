using Aesir.Infrastructure.Modules;
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

        // Register services
        services.AddScoped<IResearchTeamService, ResearchTeamService>();

        Log("Research services registered successfully");

        return Task.CompletedTask;
    }
}
