using System.Threading.Tasks;

namespace Aesir.Client.Services;

public interface IFileUploadService
{
    Task<bool> UploadFileAsync(string filePath, string conversationId);
}