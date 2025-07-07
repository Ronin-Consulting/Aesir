using System;
using System.IO;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

/// Provides services for managing document collections, including file uploads, deletions, and retrievals.
/// Handles operations such as uploading files to specific contexts, retrieving file content streams, and deleting files.
/// Uses Flurl for HTTP client operations and integrates with logging and configuration systems.
public class DocumentCollectionService(
    ILogger<DocumentCollectionService> logger,
    IConfiguration configuration,
    IFlurlClientCache flurlClientCache)
    : IDocumentCollectionService
{
    /// <summary>
    /// Specifies the maximum file size, in bytes, that can be uploaded or processed by the service.
    /// </summary>
    /// <remarks>
    /// The maximum file size is set to a constant value of 104857600 bytes, which is equivalent to 100 MB.
    /// Attempts to upload a file exceeding this size will result in an <see cref="InvalidOperationException"/>.
    /// </remarks>
    private const long MaxFileSizeBytes = 104857600; // 100MB

    /// <summary>
    /// Specifies the allowed file extension for files being validated or uploaded.
    /// Only files with this specific extension are permitted for processing in the system.
    /// </summary>
    /// <remarks>
    /// The value of this variable is used to enforce file format restrictions,
    /// ensuring that only files matching the predefined extension can be handled.
    /// Currently, the allowed file extension is restricted to ".pdf".
    /// </remarks>
    private const string AllowedFileExtension = ".pdf";

    /// <summary>
    /// Represents the Flurl client instance used to perform HTTP operations with predefined base configuration.
    /// This client is responsible for creating and managing requests to various endpoints,
    /// facilitating operations such as file uploads, file deletions, and retrieving file content streams.
    /// Configured to use the base URL retrieved from the application's configuration for
    /// interaction with the Document Collection service.
    /// </summary>
    private readonly IFlurlClient _flurlClient = flurlClientCache
        .GetOrAdd("DocumentCollectionClient",
            configuration.GetValue<string>("Inference:DocumentCollections"));

    /// <summary>
    /// Retrieves the content of a specified file from the server as a stream.
    /// </summary>
    /// <param name="filename">The name of the file whose content is to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a <see cref="Stream"/> for the file content.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file with the specified name is not found on the server.</exception>
    /// <exception cref="Exception">Thrown if there is an error retrieving the file content from the server.</exception>
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
        if (fileExtension != AllowedFileExtension)
        {
            throw new NotSupportedException($"Only PDF files are allowed: {filePath}");
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

        var response = await _flurlClient
            .Request(pathSegments)
            .PostMultipartAsync(mp =>
                mp.AddFile("file", fileStream, fileName, "application/pdf"));

        if (!response.ResponseMessage.IsSuccessStatusCode)
        {
            throw new Exception($"File upload failed with status code: {response.ResponseMessage.StatusCode}");
        }

        return response;
    }

    /// <summary>
    /// Deletes a file from the server using the specified path segments.
    /// Throws an exception if the deletion fails.
    /// </summary>
    /// <param name="pathSegments">The path segments that identify the file to be deleted on the server.</param>
    /// <returns>A task representing the asynchronous operation which returns the HTTP response for the delete operation.</returns>
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

    /// Executes a given asynchronous operation with exception handling, logging errors based on
    /// the type of exception, and rethrowing them with additional context when necessary.
    /// <param name="operation">The asynchronous operation to be executed.</param>
    /// <param name="operationName">The name of the operation, used for error logging and exception messages.</param>
    /// <param name="filePath">The file path associated with the operation, used for context in error reporting.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
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
    /// Asynchronously uploads a conversation-related file to the designated location.
    /// </summary>
    /// <param name="filePath">The path to the file that needs to be uploaded.</param>
    /// <param name="conversationId">The unique identifier of the conversation the file belongs to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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
    public async Task DeleteUploadedGlobalFileAsync(string fileName, string categoryId)
    {
        await ExecuteWithExceptionHandlingAsync(
            async () => { await DeleteFileAsync("globals", categoryId, "files", fileName); }, "delete file", fileName);
    }
}