using System;
using System.IO;
using System.Threading.Tasks;
using Aesir.Client.Services.Implementations;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

public class FileUploadService(
    ILogger<FileUploadService> logger,
    IConfiguration configuration,
    IFlurlClientCache flurlClientCache)
    : IFileUploadService
{
    private readonly IFlurlClient _flurlClient = flurlClientCache
        .GetOrAdd("FileUploadClient",
            configuration.GetValue<string>("Inference:FileUpload"));

    public async Task<bool> UploadFileAsync(string filePath, string conversationId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                logger.LogError("ConversationId is required for file upload");
                return false;
            }

            if (!File.Exists(filePath))
            {
                logger.LogError("File not found: {FilePath}", filePath);
                return false;
            }

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > 104857600) // 100MB
            {
                logger.LogError("File size exceeds 100MB limit: {FilePath}", filePath);
                return false;
            }

            var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
            if (fileExtension != ".pdf")
            {
                logger.LogError("Only PDF files are allowed: {FilePath}", filePath);
                return false;
            }

            await using var fileStream = File.OpenRead(filePath);
            var fileName = Path.GetFileName(filePath);

            var response = await _flurlClient
                .Request("upload", conversationId)
                .PostMultipartAsync(mp => 
                    mp.AddFile("file", fileStream, fileName, "application/pdf"));

            return response.ResponseMessage.IsSuccessStatusCode;
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading file");
            return false;
        }
    }
}