using FluentMigrator.Runner;
using Ollama;

namespace Aesir.Api.Server.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder MigrateDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var runner = scope.ServiceProvider.GetService<IMigrationRunner>();
        
        runner!.MigrateUp();
        
        return app;
    }

    public static IApplicationBuilder InitializeOllamaBackend(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var ollama = scope.ServiceProvider.GetService<OllamaApiClient>();

        try
        {
            var modelsResponse = ollama.Models.ListModelsAsync().Result;
        }
        catch (Exception e)
        {
            // ignore
        }

        return app;
    }
}