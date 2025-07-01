using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;

namespace Aesir.Api.Server.Services;


// This interface defines functionality for loading text content from a PDF file into a data store,
// allowing parallel processing and batching to support efficient and controlled uploads.
[Experimental("SKEXP0001")]
public interface IPdfDataLoaderService<TKey, TRecord> 
    where TKey : notnull
    where TRecord : AesirTextData<TKey>
{

    Task LoadPdfAsync(LoadPdfRequest request, CancellationToken cancellationToken);
}

public sealed class LoadPdfRequest
{
    public string? PdfLocalPath { get; set; }
    public string? PdfFileName { get; set; }
    public int BatchSize { get; set; } = 1;
    public int BetweenBatchDelayInMs { get; set; } = 100;
    
    public IDictionary<string, object>? Metadata { get; set; }
}