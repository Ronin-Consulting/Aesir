using Aesir.Client.Web.Infrastructure.Modules;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Projects.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aesir.Client.Web.Modules.Projects;

/// <summary>
/// Projects module for the AESIR client.
/// Provides project management functionality including creating, editing,
/// and managing project-specific knowledge bases and chat contexts.
/// </summary>
public class ProjectsModule : ClientModuleBase
{
    /// <inheritdoc />
    public override string Name => "Projects";

    /// <inheritdoc />
    public override string Version => "1.0.0";

    /// <inheritdoc />
    public override string Description => "Project management for organizing chats and knowledge bases.";

    /// <inheritdoc />
    public override void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IProjectApiService, ProjectApiService>();
    }

    /// <inheritdoc />
    public override void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register(new NavigationItem
        {
            Title = "Projects",
            Href = "/projects",
            Icon = "FolderSpecial",
            Priority = 20, // After Chat (10), before Settings (100)
            Group = "Main"
        });
    }
}
