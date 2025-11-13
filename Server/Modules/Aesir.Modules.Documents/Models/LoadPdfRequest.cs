using Aesir.Infrastructure.Services;

namespace Aesir.Modules.Documents.Models;

/// <summary>
/// Represents a request to load a PDF file with associated metadata and configurable processing parameters.
/// </summary>
public sealed class LoadPdfRequest
{
    /// <summary>
    /// Gets or sets the file system path to the PDF document.
    /// </summary>
    public string? PdfLocalPath { get; set; }

    /// <summary>
    /// Gets or sets the name of the PDF file.
    /// </summary>
    public string? PdfFileName { get; set; }

    /// <summary>
    /// Gets or sets the size of the batch for processing PDF content.
    /// Determines the number of content items to process in a single batch operation.
    /// </summary>
    public int BatchSize { get; set; } = Math.Max(Environment.ProcessorCount / 2 + 1, 10);

    /// <summary>
    /// Gets or sets the delay, in milliseconds, between processing batches of PDF data during loading.
    /// </summary>
    public int BetweenBatchDelayInMs { get; set; } = 50;

    /// <summary>
    /// Gets or sets a collection of key-value pairs representing metadata associated with the PDF file.
    /// </summary>
    public IDictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the model location descriptor.
    /// </summary>
    /// <remarks>
    /// This property represents metadata identifying the location or descriptor of a model,
    /// including necessary details such as interface engine ID and model ID.
    /// It can be used to specify or retrieve information about the model's source or context.
    /// </remarks>
    public ModelLocationDescriptor? ModelLocation { get; set; }
}
