using Aesir.Api.Server.Services;
using FluentMigrator.Runner;

namespace Aesir.Api.Server.Extensions;

/// <summary>
/// Provides extension methods for configuring the application builder with database migrations and backend validation.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Applies pending database migrations using FluentMigrator.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for method chaining.</returns>
    public static IApplicationBuilder MigrateDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var runner = scope.ServiceProvider.GetService<IMigrationRunner>();

        runner!.MigrateUp();

        return app;
    }
}
