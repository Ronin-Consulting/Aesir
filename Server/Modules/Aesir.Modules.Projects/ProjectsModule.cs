using Aesir.Infrastructure.Modules;
using Aesir.Modules.Projects.Repositories;
using Aesir.Modules.Projects.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Projects;

/// <summary>
/// Projects module providing project management capabilities.
/// Projects allow users to create self-contained workspaces with their own
/// chat histories and knowledge bases.
/// </summary>
public class ProjectsModule : ModuleBase
{
    public ProjectsModule(ILogger logger) : base(logger)
    {
    }

    public override string Name => "Projects";
    public override string Version => "1.0.0";
    public override string? Description => "Provides project management for organizing chats and knowledge bases";

    public override Task RegisterServicesAsync(IServiceCollection services)
    {
        Log("Registering project services...");

        // Register repository
        services.AddScoped<IProjectRepository, ProjectRepository>();

        // Register service
        services.AddScoped<IProjectService, ProjectService>();

        Log("Project services registered successfully");

        return Task.CompletedTask;
    }
}
