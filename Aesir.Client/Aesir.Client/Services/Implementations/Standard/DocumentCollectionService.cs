using System;
using System.IO;
using System.Threading.Tasks;
using Aesir.Common.FileTypes;
using Avalonia.Platform.Storage;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

/// Provides functionality for managing document-related operations, including handling file uploads,
/// downloads, and deletions for specific contexts such as conversations or global categories.
/// Leverages Flurl for HTTP operations, integrates with logging for error tracking,
/// and adheres to configuration-defined settings for service behavior.
public class DocumentCollectionService(
    ILogger<DocumentCollectionService> logger,
    IConfiguration configuration,
    IFlurlClientCache flurlClientCache)
    : IDocumentCollectionService
{
    /// <summary>
    /// Defines the maximum file size, in bytes, allowed for file uploads or processing within the application.
    /// </summary>
    /// <remarks>
    /// This value serves as an upper limit to prevent excessively large files from being uploaded or processed,
    /// ensuring efficient resource usage and system stability. Exceeding this limit will result in validation
    /// errors or an <see cref="InvalidOperationException"/>.
    /// </remarks>
    private const long MaxFileSizeBytes = 104857600; // 100MB

    /// <summary>
    /// Specifies the allowed file extensions for files being validated or uploaded.
    /// Only files with extensions listed in this array are permitted for processing by the service.
    /// </summary>
    /// <remarks>
    /// This array defines the file format restrictions applicable to file operations within the service.
    /// Uses the centralized FileTypeManager to ensure consistency across all projects.
    /// </remarks>
    private static readonly string[] AllowedFileExtensions = FileTypeManager.DocumentProcessingExtensions;

    /// <summary>
    /// Represents the Flurl client instance used for executing HTTP requests to the Document Collection service.
    /// </summary>
    /// <remarks>
    /// This client is configured with a base URL derived from the application's configuration settings.
    /// It is utilized to perform operations such as uploading, deleting, and retrieving file content.
    /// The instance is managed through an <see cref="IFlurlClientCache"/> to enable efficient reuse of the HTTP client across requests.
    /// </remarks>
    private readonly IFlurlClient _flurlClient = flurlClientCache
        .GetOrAdd("DocumentCollectionClient",
            configuration.GetValue<string>("Inference:DocumentCollections"));

    /// <summary>
    /// Retrieves the content of a specified file from the server as a stream.
    /// </summary>
    /// <param name="filename">The name of the file whose content is to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a <see cref="Stream"/> representing the file content.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the server does not locate the specified file.</exception>
    /// <exception cref="Exception">Thrown when an error occurs while retrieving the file content from the server.</exception>
    public async Task<Stream> GetFileContentStreamAsync(string filename)
    {
        try
        {
            //file/{filename}/content
            var response = (await _flurlClient.Request()
                .AppendPathSegment("file")
                .AppendPathSegment(filename, true)
                .AppendPathSegment("content")
                .GetAsync());

            return await response.GetStreamAsync();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 404)
        {
            throw new FileNotFoundException($"File '{filename}' not found on server.");
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw new Exception($"Failed to get file stream for '{filename}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Validates the specified file for upload, ensuring it meets the necessary requirements
    /// such as file existence, size, and allowed file extension.
    /// </summary>
    /// <param name="file">The file to validate.</param>
    /// <param name="identifier">A unique identifier associated with the upload operation (e.g., conversation ID or category ID).</param>
    /// <param name="parameterName">The name of the parameter that represents the identifier.</param>
    /// <exception cref="ArgumentException">Thrown if the identifier is null, empty, or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist at the specified path.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the file size exceeds the allowed maximum size.</exception>
    /// <exception cref="NotSupportedException">Thrown if the file format is not supported.</exception>
    private async Task ValidateUploadFileAsync(IStorageFile file, string identifier, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException($"{parameterName} is required for file upload", nameof(identifier));
        }
        
        try
        {
            await using var stream = await file.OpenReadAsync();
        }
        catch (Exception e)
        {
            throw new FileNotFoundException($"File could not be opened: {file.Name}");
        }

        var fileProperties = await file.GetBasicPropertiesAsync();

        if (fileProperties.Size > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"File size exceeds 100MB limit: {file.Name}");
        }

        var fileExtension = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!Array.Exists(AllowedFileExtensions, ext => ext == fileExtension))
        {
            throw new NotSupportedException(
                $"Only allowed file types ({string.Join(", ", AllowedFileExtensions)}) are supported: {file.Name}");
        }
    }

    /// <summary>
    /// Uploads a file located at the specified file path to a designated destination defined by the given path segments.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <param name="pathSegments">An array of path segments specifying the destination endpoint for the file upload.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the HTTP response returned from the server.</returns>
    /// <exception cref="System.IO.FileNotFoundException">Thrown if the file at <paramref name="filePath"/> does not exist.</exception>
    /// <exception cref="System.Exception">Thrown if the file upload fails with a non-success HTTP status code.</exception>
    private async Task<IFlurlResponse> UploadFileAsync(IStorageFile file, params string[] pathSegments)
    {
        await using var fileStream = await file.OpenReadAsync();
        var fileName = Path.GetFileName(file.Name);
        var contentType = GetContentTypeForFile(fileName);

        var response = await _flurlClient
            .Request(pathSegments)
            .PostMultipartAsync(mp =>
                mp.AddFile("file", fileStream, fileName, contentType));

        if (!response.ResponseMessage.IsSuccessStatusCode)
        {
            throw new Exception($"File upload failed with status code: {response.ResponseMessage.StatusCode}");
        }

        return response;
    }

    /// <summary>
    /// Deletes a file from the server using the specified path segments.
    /// </summary>
    /// <param name="pathSegments">An array of path segments that specify the location of the file to be deleted on the server.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains an <see cref="IFlurlResponse"/> that represents the HTTP response for the delete operation.</returns>
    /// <exception cref="Exception">Thrown if the HTTP response status indicates a failure to delete the file.</exception>
    private async Task<IFlurlResponse> DeleteFileAsync(params string[] pathSegments)
    {
        var response = await _flurlClient
            .Request(pathSegments)
            .DeleteAsync();

        if (!response.ResponseMessage.IsSuccessStatusCode)
        {
            throw new Exception($"Delete file failed with status code: {response.ResponseMessage.StatusCode}");
        }

        return response;
    }

    /// <summary>
    /// Executes a given asynchronous operation with exception handling. Logs errors based on the type of exception
    /// and rethrows them with additional context when necessary.
    /// </summary>
    /// <param name="operation">The asynchronous operation to be executed.</param>
    /// <param name="operationName">The name of the operation, used for error logging and exception messages.</param>
    /// <param name="filePath">The file path associated with the operation, used for additional context in error reporting.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="FlurlHttpException">Thrown when an HTTP error occurs during the execution of the operation.</exception>
    /// <exception cref="Exception">Thrown when an unexpected error occurs, with additional context for the operation and file path.</exception>
    private async Task ExecuteWithExceptionHandlingAsync(Func<Task> operation, string operationName, string filePath)
    {
        try
        {
            await operation();
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw new Exception($"Failed to {operationName} '{filePath}': {ex.Message}", ex);
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is FileNotFoundException ||
                                     ex is InvalidOperationException || ex is NotSupportedException))
        {
            logger.LogError(ex, "Error {OperationName}", operationName);
            throw new Exception($"Unexpected error {operationName} '{filePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Asynchronously uploads a file associated with a specific conversation to the intended storage location.
    /// </summary>
    /// <param name="file">The file that is to be uploaded.</param>
    /// <param name="conversationId">The unique identifier of the conversation to which the file is associated.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the provided file path or conversation ID is invalid.</exception>
    /// <exception cref="Exception">Thrown if an error occurs during the file upload process.</exception>
    public async Task UploadConversationFileAsync(IStorageFile file, string conversationId)
    {
        await ExecuteWithExceptionHandlingAsync(async () =>
        {
            await ValidateUploadFileAsync(file, conversationId, nameof(conversationId));
            await UploadFileAsync(file, "conversations", conversationId, "upload", "file");
        }, "upload file", file.Name);
    }

    /// <summary>
    /// Deletes an uploaded conversation file associated with a specific conversation.
    /// </summary>
    /// <param name="fileName">The name of the file to be deleted.</param>
    /// <param name="conversationId">The unique identifier of the conversation associated with the file.</param>
    /// <returns>A task representing the asynchronous operation of deleting the file.</returns>
    /// <exception cref="Exception">Thrown if an error occurs during the deletion process.</exception>
    public async Task DeleteUploadedConversationFileAsync(string fileName, string conversationId)
    {
        await ExecuteWithExceptionHandlingAsync(
            async () => { await DeleteFileAsync("conversations", conversationId, "files", fileName); }, "delete file",
            fileName);
    }

    /// <summary>
    /// Uploads a file globally to the specified category.
    /// </summary>
    /// <param name="file">The file to be uploaded.</param>
    /// <param name="categoryId">The identifier of the category to which the file will be associated.</param>
    /// <returns>A task that represents the asynchronous upload operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the file path or category ID is null or empty.</exception>
    /// <exception cref="IOException">Thrown if an I/O error occurs during file upload.</exception>
    /// <exception cref="Exception">Thrown if there is an unexpected error during the upload process.</exception>
    public async Task UploadGlobalFileAsync(IStorageFile file, string categoryId)
    {
        await ExecuteWithExceptionHandlingAsync(async () =>
        {
            await ValidateUploadFileAsync(file, categoryId, nameof(categoryId));
            await UploadFileAsync(file, "globals", categoryId, "upload", "file");
        }, "upload file", file.Name);
    }

    /// <summary>
    /// Deletes a previously uploaded global file associated with a specific category.
    /// </summary>
    /// <param name="fileName">The name of the file to be deleted.</param>
    /// <param name="categoryId">The unique identifier of the category associated with the file.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="Exception">Thrown if an error occurs while attempting to delete the file.</exception>
    public async Task DeleteUploadedGlobalFileAsync(string fileName, string categoryId)
    {
        await ExecuteWithExceptionHandlingAsync(
            async () => { await DeleteFileAsync("globals", categoryId, "files", fileName); }, "delete file", fileName);
    }
    
    /// <summary>
    /// Determines the content type for a file based on its extension.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>The MIME content type for the file based on its extension.</returns>
    /// <remarks>
    /// Uses the centralized FileTypeManager to determine MIME types, ensuring consistency across all projects.
    /// Falls back to "application/octet-stream" if the content type cannot be determined.
    /// </remarks>
    private string GetContentTypeForFile(string filePath) => FileTypeManager.GetMimeType(filePath);
}