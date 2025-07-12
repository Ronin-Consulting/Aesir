using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;

namespace Aesir.Api.Server.Services;

/// <summary>
/// Provides functionality for loading text content from PDF files into a data store with support for parallel processing and batching.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify records.</typeparam>
/// <typeparam name="TRecord">The type of the text data record.</typeparam>
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
/// Represents a request to load a PDF file with specified processing parameters.
/// </summary>
public sealed class LoadPdfRequest
{
    /// <summary>
    /// Gets or sets the local path to the PDF file.
    /// </summary>
    public string? PdfLocalPath { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the PDF file.
    /// </summary>
    public string? PdfFileName { get; set; }
    
    /// <summary>
    /// Gets or sets the batch size for processing. Defaults to half the processor count with a minimum of 1.
    /// </summary>
    public int BatchSize { get; set; } =  Math.Max(1, Environment.ProcessorCount / 2);
    
    /// <summary>
    /// Gets or sets the delay in milliseconds between batches. Defaults to 100ms.
    /// </summary>
    public int BetweenBatchDelayInMs { get; set; } = 100;
    
    /// <summary>
    /// Gets or sets optional metadata to associate with the PDF content.
    /// </summary>
    public IDictionary<string, object>? Metadata { get; set; }
}

public sealed class RawContent
{
    public string? Text { get; init; }

    public ReadOnlyMemory<byte>? Image { get; init; }

    public int PageNumber { get; init; }
}

public class UniqueKeyGenerator<TKey>(Func<TKey> generator)
    where TKey : notnull
{
    /// <summary>
    /// Generate a unique key.
    /// </summary>
    /// <returns>The unique key that was generated.</returns>
    public TKey GenerateKey() => generator();
}