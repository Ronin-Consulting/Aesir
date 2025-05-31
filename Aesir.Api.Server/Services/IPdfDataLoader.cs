namespace Aesir.Api.Server.Services;


// This interface defines functionality for loading text content from a PDF file into a data store,
// allowing parallel processing and batching to support efficient and controlled uploads.
public interface IPdfDataLoader
{
    /// <summary>
    /// Load the text from a PDF file into the data store.
    /// </summary>
    /// <param name="pdfPath">The pdf file to load.</param>
    /// <param name="batchSize">Maximum number of parallel threads to generate embeddings and upload records.</param>
    /// <param name="betweenBatchDelayInMs">The number of milliseconds to delay between batches to avoid throttling.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>An async task that completes when the loading is complete.</returns>
    Task LoadPdfAsync(string pdfPath, int batchSize, int betweenBatchDelayInMs, CancellationToken cancellationToken);
}