using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Aesir.Client.Models;
using Avalonia.Platform.Storage;

namespace Aesir.Client.Services.Implementations.NoOp;

/// <summary>
/// Provides a no-operation implementation of the <see cref="IDocumentCollectionService"/> interface.
/// </summary>
/// <remarks>
/// This implementation is used in scenarios where document collection functionality is not required
/// or available, such as during design-time or in testing environments. All methods return
/// empty results or complete successfully without performing any actual operations.
/// </remarks>
public class NoOpDocumentCollectionService:IDocumentCollectionService
{
    /// <summary>
    /// Asynchronously retrieves an empty collection of documents.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains an empty collection of <see cref="AesirDocument"/> objects.</returns>
    public async Task<IEnumerable<AesirDocument>> GetDocumentsAsync()
    {
        return await Task.FromResult(new  List<AesirDocument>());
    }

    /// <summary>
    /// Asynchronously retrieves an empty memory stream as file content.
    /// </summary>
    /// <param name="filename">The name of the file (ignored in this no-op implementation).</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an empty <see cref="MemoryStream"/>.</returns>
    public async Task<Stream> GetFileContentStreamAsync(string filename)
    {
        return await Task.FromResult<Stream>(new MemoryStream());
    }

    /// <summary>
    /// Asynchronously performs a no-operation file upload for conversation files.
    /// </summary>
    /// <param name="file">The file to upload (ignored in this no-op implementation).</param>
    /// <param name="conversationId">The conversation identifier (ignored in this no-op implementation).</param>
    /// <returns>A task that represents the asynchronous operation that completes immediately.</returns>
    public async Task UploadConversationFileAsync(IStorageFile file, string conversationId)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously performs a no-operation file deletion for conversation files.
    /// </summary>
    /// <param name="fileName">The name of the file to delete (ignored in this no-op implementation).</param>
    /// <param name="conversationId">The conversation identifier (ignored in this no-op implementation).</param>
    /// <returns>A task that represents the asynchronous operation that completes immediately.</returns>
    public async Task DeleteUploadedConversationFileAsync(string fileName, string conversationId)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously performs a no-operation file upload for global files.
    /// </summary>
    /// <param name="file">The file to upload (ignored in this no-op implementation).</param>
    /// <param name="categoryId">The category identifier (ignored in this no-op implementation).</param>
    /// <returns>A task that represents the asynchronous operation that completes immediately.</returns>
    public async Task UploadGlobalFileAsync(IStorageFile file, string categoryId)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously performs a no-operation file deletion for global files.
    /// </summary>
    /// <param name="fileName">The name of the file to delete (ignored in this no-op implementation).</param>
    /// <param name="categoryId">The category identifier (ignored in this no-op implementation).</param>
    /// <returns>A task that represents the asynchronous operation that completes immediately.</returns>
    public async Task DeleteUploadedGlobalFileAsync(string fileName, string categoryId)
    {
        await Task.CompletedTask;
    }
}
