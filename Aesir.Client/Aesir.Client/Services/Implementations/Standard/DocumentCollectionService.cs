using System;
using System.IO;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

public class DocumentCollectionService : IDocumentCollectionService
{
    private const long MaxFileSizeBytes = 104857600; // 100MB
    private const string AllowedFileExtension = ".pdf";
    
    private readonly ILogger<DocumentCollectionService> _logger;
    private readonly IFlurlClient _flurlClient;

    public DocumentCollectionService(ILogger<DocumentCollectionService> logger,
        IConfiguration configuration, IFlurlClientCache flurlClientCache)
    {
        _logger = logger;
        _flurlClient = flurlClientCache
            .GetOrAdd("DocumentCollectionClient",
                configuration.GetValue<string>("Inference:DocumentCollections"));
    }
    
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
            await _logger.LogFlurlExceptionAsync(ex);
            throw new Exception($"Failed to get file stream for '{filename}': {ex.Message}", ex);
        }
    }

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
    
    private async Task ExecuteWithExceptionHandlingAsync(Func<Task> operation, string operationName, string filePath)
    {
        try
        {
            await operation();
        }
        catch (FlurlHttpException ex)
        {
            await _logger.LogFlurlExceptionAsync(ex);
            throw new Exception($"Failed to {operationName} '{filePath}': {ex.Message}", ex);
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is FileNotFoundException || ex is InvalidOperationException || ex is NotSupportedException))
        {
            _logger.LogError(ex, "Error {OperationName}", operationName);
            throw new Exception($"Unexpected error {operationName} '{filePath}': {ex.Message}", ex);
        }
    }

    public async Task UploadConversationFileAsync(string filePath, string conversationId)
    {
        await ExecuteWithExceptionHandlingAsync(async () =>
        {
            ValidateUploadFile(filePath, conversationId, nameof(conversationId));
            await UploadFileAsync(filePath, "conversations", conversationId, "upload", "file");
        }, "upload file", filePath);
    }

    public async Task DeleteUploadedConversationFileAsync(string fileName, string conversationId)
    {
        await ExecuteWithExceptionHandlingAsync(async () =>
        {
            await DeleteFileAsync("conversations", conversationId, "files", fileName);
        }, "delete file", fileName);
    }

    public async Task UploadGlobalFileAsync(string filePath, string categoryId)
    {
        await ExecuteWithExceptionHandlingAsync(async () =>
        {
            ValidateUploadFile(filePath, categoryId, nameof(categoryId));
            await UploadFileAsync(filePath, "globals", categoryId, "upload", "file");
        }, "upload file", filePath);
    }

    public async Task DeleteUploadedGlobalFileAsync(string fileName, string categoryId)
    {
        await ExecuteWithExceptionHandlingAsync(async () =>
        {
            await DeleteFileAsync("globals", categoryId, "files", fileName);
        }, "delete file", fileName);
    }
}