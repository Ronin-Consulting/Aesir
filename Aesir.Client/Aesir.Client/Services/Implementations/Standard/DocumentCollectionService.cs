using System;
using System.IO;
using System.Threading.Tasks;
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
    /// Currently, the allowed file extensions are ".pdf" and ".png".
    /// Attempts to process files with extensions not included in this array will result in an error.
    /// </remarks>
    private static readonly string[] AllowedFileExtensions = [".pdf", ".png"];

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
    /// <param name="filePath">The full path of the file to validate.</param>
    /// <param name="identifier">A unique identifier associated with the upload operation (e.g., conversation ID or category ID).</param>
    /// <param name="parameterName">The name of the parameter that represents the identifier.</param>
    /// <exception cref="ArgumentException">Thrown if the identifier is null, empty, or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist at the specified path.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the file size exceeds the allowed maximum size.</exception>
    /// <exception cref="NotSupportedException">Thrown if the file format is not supported.</exception>
    private void ValidateUploadFile(string filePath, string identifier, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException($"{parameterName} is required for file upload", nameof(identifier));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"File size exceeds 100MB limit: {filePath}");
        }

        var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
        if (!Array.Exists(AllowedFileExtensions, ext => ext == fileExtension))
        {
            throw new NotSupportedException(
                $"Only allowed file types ({string.Join(", ", AllowedFileExtensions)}) are supported: {filePath}");
        }
    }

    /// <summary>
    /// Uploads a file located at the specified file path to a designated destination defined by the given path segments.
    /// </summary>
    /// <param name="filePath">The full file path of the file to upload.</param>
    /// <param name="pathSegments">An array of path segments specifying the destination endpoint for the file upload.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the HTTP response returned from the server.</returns>
    /// <exception cref="System.IO.FileNotFoundException">Thrown if the file at <paramref name="filePath"/> does not exist.</exception>
    /// <exception cref="System.Exception">Thrown if the file upload fails with a non-success HTTP status code.</exception>
    private async Task<IFlurlResponse> UploadFileAsync(string filePath, params string[] pathSegments)
    {
        await using var fileStream = File.OpenRead(filePath);
        var fileName = Path.GetFileName(filePath);
        var contentType = GetContentTypeForFile(filePath);

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
    /// <param name="filePath">The full path to the file that is to be uploaded.</param>
    /// <param name="conversationId">The unique identifier of the conversation to which the file is associated.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the provided file path or conversation ID is invalid.</exception>
    /// <exception cref="Exception">Thrown if an error occurs during the file upload process.</exception>
    public async Task UploadConversationFileAsync(string filePath, string conversationId)
    {
        await ExecuteWithExceptionHandlingAsync(async () =>
        {
            ValidateUploadFile(filePath, conversationId, nameof(conversationId));
            await UploadFileAsync(filePath, "conversations", conversationId, "upload", "file");
        }, "upload file", filePath);
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
    /// <param name="filePath">The path of the file to be uploaded.</param>
    /// <param name="categoryId">The identifier of the category to which the file will be associated.</param>
    /// <returns>A task that represents the asynchronous upload operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the file path or category ID is null or empty.</exception>
    /// <exception cref="IOException">Thrown if an I/O error occurs during file upload.</exception>
    /// <exception cref="Exception">Thrown if there is an unexpected error during the upload process.</exception>
    public async Task UploadGlobalFileAsync(string filePath, string categoryId)
    {
        await ExecuteWithExceptionHandlingAsync(async () =>
        {
            ValidateUploadFile(filePath, categoryId, nameof(categoryId));
            await UploadFileAsync(filePath, "globals", categoryId, "upload", "file");
        }, "upload file", filePath);
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
    /// Uses a mapping approach to determine common MIME types. For PDF files, returns "application/pdf".
    /// For other file types, attempts to determine the appropriate MIME type based on extension.
    /// Falls back to "application/octet-stream" if the content type cannot be determined.
    /// </remarks>
    private string GetContentTypeForFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        // Map common extensions to MIME types
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".txt" => "text/plain",
            ".html" => "text/html",
            ".htm" => "text/html",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".zip" => "application/zip",
            _ => "application/octet-stream" // Default fallback for unknown types
        };
    }
}