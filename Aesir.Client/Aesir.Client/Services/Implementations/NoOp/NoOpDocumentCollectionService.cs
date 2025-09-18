using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Aesir.Client.Models;
using Avalonia.Platform.Storage;

namespace Aesir.Client.Services.Implementations.NoOp;

public class NoOpDocumentCollectionService:IDocumentCollectionService
{
    public async Task<IEnumerable<AesirDocument>> GetDocumentsAsync()
    {
        return await Task.FromResult(new  List<AesirDocument>());
    }

    public async Task<Stream> GetFileContentStreamAsync(string filename)
    {
        return await Task.FromResult<Stream>(new MemoryStream());
    }

    public async Task UploadConversationFileAsync(IStorageFile file, string conversationId)
    {
        await Task.CompletedTask;
    }

    public async Task DeleteUploadedConversationFileAsync(string fileName, string conversationId)
    {
        await Task.CompletedTask;
    }

    public async Task UploadGlobalFileAsync(IStorageFile file, string categoryId)
    {
        await Task.CompletedTask;
    }

    public async Task DeleteUploadedGlobalFileAsync(string fileName, string categoryId)
    {
        await Task.CompletedTask;
    }
}