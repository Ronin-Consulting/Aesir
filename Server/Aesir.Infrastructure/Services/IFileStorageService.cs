using Aesir.Infrastructure.Models;

namespace Aesir.Infrastructure.Services;

/// <summary>
/// Provides file storage functionality for managing files and their metadata.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Inserts a new file or updates an existing file.
    /// </summary>
    /// <param name="filename">The name of the file.</param>
    /// <param name="mimeType">The MIME type of the file.</param>
    /// <param name="content">The file content as a byte array.</param>
    /// <returns>A task representing the asynchronous operation that returns the file identifier.</returns>
    Task<int> UpsertFileAsync(string filename, string mimeType, byte[] content);

    /// <summary>
    /// Retrieves file information by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the file.</param>
    /// <returns>A task representing the asynchronous operation that returns the file information or null if not found.</returns>
    Task<AesirFileInfo?> GetFileInfoAsync(Guid id);

    /// <summary>
    /// Retrieves file information by its filename.
    /// </summary>
    /// <param name="filename">The name of the file.</param>
    /// <returns>A task representing the asynchronous operation that returns the file information or null if not found.</returns>
    Task<AesirFileInfo?> GetFileInfoAsync(string filename);

    /// <summary>
    /// Retrieves information for all stored files.
    /// </summary>
    /// <returns>A task representing the asynchronous operation that returns a collection of file information.</returns>
    Task<IEnumerable<AesirFileInfo>> GetFilesAsync();

    /// <summary>
    /// Retrieves information for files in a specific folder.
    /// </summary>
    /// <param name="folder">The folder path.</param>
    /// <returns>A task representing the asynchronous operation that returns a collection of file information.</returns>
    Task<IEnumerable<AesirFileInfo>> GetFilesByFolderAsync(string folder);

    /// <summary>
    /// Deletes a file by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the file to delete.</param>
    /// <returns>A task representing the asynchronous operation that returns true if the file was deleted successfully.</returns>
    Task<bool> DeleteFileAsync(Guid id);

    /// <summary>
    /// Deletes all files in a specific folder.
    /// </summary>
    /// <param name="folder">The folder path.</param>
    /// <returns>A task representing the asynchronous operation that returns true if the files were deleted successfully.</returns>
    Task<bool> DeleteFilesByFolderAsync(string folder);

    /// <summary>
    /// Retrieves the file content and information by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the file.</param>
    /// <returns>A task representing the asynchronous operation that returns the temporary file handle and information or null if not found.</returns>
    Task<(TempFileHandle TempFile, AesirFileInfo FileInfo)?> GetFileContentAsync(Guid id);

    /// <summary>
    /// Retrieves the file content and information by its filename.
    /// </summary>
    /// <param name="filename">The name of the file.</param>
    /// <returns>A task representing the asynchronous operation that returns the temporary file handle and information or null if not found.</returns>
    Task<(TempFileHandle TempFile, AesirFileInfo FileInfo)?> GetFileContentAsync(string filename);

    /// <summary>
    /// Inserts a new file or updates an existing file, including optional thumbnail data.
    /// </summary>
    /// <param name="filename">The name of the file.</param>
    /// <param name="mimeType">The MIME type of the file.</param>
    /// <param name="content">The file content as a byte array.</param>
    /// <param name="thumbnailContent">The thumbnail image content as a byte array, or null if no thumbnail.</param>
    /// <param name="thumbnailMimeType">The MIME type of the thumbnail, or null if no thumbnail.</param>
    /// <returns>A task representing the asynchronous operation that returns the number of affected rows.</returns>
    Task<int> UpsertFileWithThumbnailAsync(string filename, string mimeType, byte[] content,
        byte[]? thumbnailContent, string? thumbnailMimeType);

    /// <summary>
    /// Retrieves the thumbnail content for a file by its filename.
    /// </summary>
    /// <param name="filename">The name of the file.</param>
    /// <returns>A task representing the asynchronous operation that returns the thumbnail bytes and MIME type, or null if not found or no thumbnail exists.</returns>
    Task<(byte[] Content, string MimeType)?> GetFileThumbnailAsync(string filename);

    /// <summary>
    /// Updates the thumbnail for an existing file.
    /// </summary>
    /// <param name="filename">The name of the file.</param>
    /// <param name="thumbnailContent">The thumbnail image content as a byte array.</param>
    /// <param name="thumbnailMimeType">The MIME type of the thumbnail.</param>
    /// <returns>A task representing the asynchronous operation that returns true if the thumbnail was updated.</returns>
    Task<bool> UpdateFileThumbnailAsync(string filename, byte[] thumbnailContent, string thumbnailMimeType);

    /// <summary>
    /// Retrieves files that belong to the specified conversation IDs.
    /// Used to filter files by user's conversations for security.
    /// </summary>
    /// <param name="conversationIds">The conversation IDs to filter by.</param>
    /// <returns>A task representing the asynchronous operation that returns a collection of file information.</returns>
    Task<IEnumerable<AesirFileInfo>> GetFilesByConversationIdsAsync(IEnumerable<Guid> conversationIds);
}
