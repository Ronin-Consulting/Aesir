using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;

namespace Aesir.Api.Server.Services;

/// <summary>
/// Defines a service for processing and importing text content from PDF files into a structured data format,
/// enabling efficient handling of records with support for concurrency and batch operations.
/// </summary>
/// <typeparam name="TKey">Represents the unique identifier type for the data records.</typeparam>
/// <typeparam name="TRecord">Specifies the type of the data record capturing extracted text content.</typeparam>
[Experimental("SKEXP0001")]
public interface IPdfDataLoaderService<TKey, TRecord>
    where TKey : notnull
    where TRecord : AesirTextData<TKey>
{
    /// <summary>
    /// Loads text content from a PDF file into the data store.
    /// </summary>
    /// <param name="request">The PDF loading request containing file path, batch settings, and metadata.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LoadPdfAsync(LoadPdfRequest request, CancellationToken cancellationToken);
}

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
    public int BatchSize { get; set; } = Math.Min(Environment.ProcessorCount / 2 + 1, 4);

    /// <summary>
    /// Gets or sets the delay, in milliseconds, between processing batches of PDF data during loading.
    /// </summary>
    public int BetweenBatchDelayInMs { get; set; } = 100;

    /// <summary>
    /// Gets or sets a collection of key-value pairs representing metadata associated with the PDF file.
    /// </summary>
    public IDictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Represents the raw content extracted from a processed source, including text, images, and associated metadata.
/// </summary>
public sealed class RawContent
{
    /// <summary>
    /// Gets the raw text content.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// Gets or initializes the raw binary content of an image, represented as a read-only memory block of bytes.
    /// </summary>
    public ReadOnlyMemory<byte>? Image { get; init; }

    /// <summary>
    /// Gets the page number associated with the content.
    /// </summary>
    public int PageNumber { get; init; }
}