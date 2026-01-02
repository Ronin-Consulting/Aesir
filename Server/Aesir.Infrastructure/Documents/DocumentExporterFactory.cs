using Microsoft.Extensions.DependencyInjection;

namespace Aesir.Infrastructure.Documents;

/// <summary>
/// Factory for creating document exporters based on format.
/// </summary>
public class DocumentExporterFactory : IDocumentExporterFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<DocumentFormat, Type> _exporterTypes;

    public DocumentExporterFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _exporterTypes = new Dictionary<DocumentFormat, Type>
        {
            { DocumentFormat.Pdf, typeof(PdfExporter) },
            { DocumentFormat.Word, typeof(WordExporter) }
        };
    }

    /// <inheritdoc />
    public IDocumentExporter GetExporter(DocumentFormat format)
    {
        if (!_exporterTypes.TryGetValue(format, out var exporterType))
        {
            throw new NotSupportedException($"Document format '{format}' is not supported.");
        }

        return (IDocumentExporter)_serviceProvider.GetRequiredService(exporterType);
    }

    /// <inheritdoc />
    public IEnumerable<IDocumentExporter> GetAllExporters()
    {
        foreach (var exporterType in _exporterTypes.Values)
        {
            yield return (IDocumentExporter)_serviceProvider.GetRequiredService(exporterType);
        }
    }
}
