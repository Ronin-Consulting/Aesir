using Aesir.Api.Server.Services;
using FluentMigrator.Runner;

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
        var modelsService = scope.ServiceProvider.GetService<IModelsService>();

        try
        {
            _ = modelsService!.GetModelsAsync().Result;
        }
        catch (Exception)
        {
        }

        return app;
    }
}
