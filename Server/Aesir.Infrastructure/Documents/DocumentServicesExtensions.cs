using Microsoft.Extensions.DependencyInjection;

namespace Aesir.Infrastructure.Documents;

/// <summary>
/// Extension methods for registering document export services.
/// </summary>
public static class DocumentServicesExtensions
{
    /// <summary>
    /// Adds document export services to the service collection.
    /// This includes PDF and Word exporters.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDocumentExportServices(this IServiceCollection services)
    {
        // Register individual exporters
        services.AddSingleton<PdfExporter>();
        services.AddSingleton<WordExporter>();

        // Register the factory
        services.AddSingleton<IDocumentExporterFactory, DocumentExporterFactory>();

        return services;
    }
}
