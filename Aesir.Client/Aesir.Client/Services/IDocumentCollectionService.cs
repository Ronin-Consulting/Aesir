using System.IO;
using System.Threading.Tasks;

namespace Aesir.Client.Services;

/// <summary>
/// Represents a service for managing document collections, enabling file operations such as retrieval, upload, and deletion
/// for conversation-specific and global contexts.
/// </summary>
public interface IDocumentCollectionService
{
    /// <summary>
    /// Retrieves the content of a specified file from the server as a stream.
    /// </summary>
    /// <param name="filename">The name of the file whose content is to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="Stream"/> with the file content.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file is not found on the server.</exception>
    /// <exception cref="Exception">Thrown if there is an error during the file retrieval process.</exception>
    Task<Stream> GetFileContentStreamAsync(string filename);

    /// <summary>
    /// Asynchronously uploads a file associated with a specific conversation to a designated storage location.
    /// </summary>
    /// <param name="filePath">The file path of the file to be uploaded.</param>
    /// <param name="conversationId">The identifier of the conversation to which the file belongs.</param>
    /// <returns>A task that represents the asynchronous file upload operation.</returns>
    Task UploadConversationFileAsync(string filePath, string conversationId);

    /// <summary>
    /// Deletes an uploaded conversation file associated with a specific conversation.
    /// </summary>
    /// <param name="fileName">The name of the file to be deleted.</param>
    /// <param name="conversationId">The unique identifier of the conversation associated with the file.</param>
    /// <returns>A task representing the asynchronous operation of deleting the file.</returns>
    Task DeleteUploadedConversationFileAsync(string fileName, string conversationId);

    /// <summary>
    /// Uploads a file globally to the specified category.
    /// </summary>
    /// <param name="filePath">The path of the file to be uploaded.</param>
    /// <param name="categoryId">The identifier of the category to which the file will be associated.</param>
    /// <returns>A task that represents the asynchronous upload operation.</returns>
    Task UploadGlobalFileAsync(string filePath, string categoryId);

    /// <summary>
    /// Deletes a previously uploaded global file associated with a specific category.
    /// </summary>
    /// <param name="fileName">The name of the file to be deleted.</param>
    /// <param name="categoryId">The unique identifier of the category associated with the file.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteUploadedGlobalFileAsync(string fileName, string categoryId);
}