using System.IO;
using System.Threading.Tasks;

namespace Aesir.Client.Services;

public interface IDocumentCollectionService
{
    Task<Stream> GetFileContentStreamAsync(string filename);
    
    Task UploadConversationFileAsync(string filePath, string conversationId);
    
    Task UploadGlobalFileAsync(string filePath, string categoryId);
}