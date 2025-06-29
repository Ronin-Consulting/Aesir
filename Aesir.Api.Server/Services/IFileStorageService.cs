using Aesir.Api.Server.Models;

namespace Aesir.Api.Server.Services;

public interface IFileStorageService
{
    Task<int> UpsertFileAsync(string filename, string mimeType, byte[] content);
    Task<AesirFileInfo?> GetFileInfoAsync(Guid id);
    Task<AesirFileInfo?> GetFileInfoAsync(string filename);
    Task<IEnumerable<AesirFileInfo>> GetFilesAsync();
    Task<IEnumerable<AesirFileInfo>> GetFilesByFolderAsync(string folder);
    Task<bool> DeleteFileAsync(Guid id);
    Task<bool> DeleteFilesByFolderAsync(string folder);
    Task<(string FilePath, AesirFileInfo FileInfo)?> GetFileContentAsync(Guid id);
    Task<(string FilePath, AesirFileInfo FileInfo)?> GetFileContentAsync(string filename);
}