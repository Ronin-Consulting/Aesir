using System.Diagnostics.CodeAnalysis;
using Aesir.Infrastructure.Models;
using Aesir.Modules.Documents.Models;

namespace Aesir.Modules.Documents.Services.DocumentLoaders;

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