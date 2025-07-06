using Aesir.Api.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Api.Server.Controllers;

// NOTE: REFACTOR SOON
[ApiController]
[Route("document/collections")]
[Produces("application/json")]
public class DocumentCollectionController(
    ILogger<DocumentCollectionController> logger,
    IFileStorageService fileStorageService,
    IDocumentCollectionService documentCollectionService)
    : ControllerBase
{
    private const int MaxFileSize = 104857600; // 100MB

    private enum FolderType
    {
        Global,
        Conversation
    }

    //https://aesir.localhost/document/collections/file/%2F407e11d8-2763-48a9-aa7a-2bd549b3e7f9%2FMissionPlan-OU812.pdf/content
    [HttpGet("file/{filename}/content")]
    public async Task<IActionResult> GetFileContentAsync([FromRoute]string filename)
    {
        return await GetFileContentCoreAsync(Uri.UnescapeDataString(filename));
    }
    
    #region Global Files
    [HttpPost("globals/{categoryId}/upload/file")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
    public async Task<IActionResult> UploadGlobalFileAsync(IFormFile? file, [FromRoute] string categoryId)
    {
        var result = await ProcessFileUploadAsync(file, categoryId, FolderType.Global);

        if (!result.Success)
            return BadRequest(result.ErrorMessage);

        return Ok(new { message = "File uploaded successfully", fileName = file?.FileName, categoryId });
    }

    [HttpGet("globals/{categoryId}/files")]
    public async Task<IActionResult> GetGlobalFilesAsync([FromRoute] string categoryId)
    {
        return await GetFilesByFolderAsync(categoryId, "CategoryId", FolderType.Global);
    }

    [HttpGet("globals/{categoryId}/files/{filename}/content")]
    public async Task<IActionResult> GetGlobalFileContentAsync([FromRoute] string categoryId, [FromRoute] string filename)
    {
        return await GetFolderFileContentAsync(categoryId, filename, FolderType.Global);
    }
    
    [HttpDelete("globals/{categoryId}/files/{filename}")]
    public async Task<IActionResult> DeleteGlobalFileAsync([FromRoute] string categoryId, [FromRoute] string filename)
    {
        return await DeleteFileAsync(categoryId, "CategoryId", FolderType.Global, filename);
    }
    #endregion

    #region Conversation Files
    [HttpPost("conversations/{conversationId}/upload/file")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
    public async Task<IActionResult> UploadConversationFileAsync(IFormFile? file, [FromRoute] string conversationId)
    {
        var result = await ProcessFileUploadAsync(file, conversationId, FolderType.Conversation);

        if (!result.Success)
            return BadRequest(result.ErrorMessage);

        return Ok(new { message = "File uploaded successfully", fileName = file?.FileName, conversationId });
    }

    [HttpGet("conversations/{conversationId}/files")]
    public async Task<IActionResult> GetConversationFilesAsync([FromRoute] string conversationId)
    {
        return await GetFilesByFolderAsync(conversationId, "ConversationId", FolderType.Conversation);
    }

    [HttpGet("conversations/{conversationId}/files/{filename}/content")]
    public async Task<IActionResult> GetConversationFileContentAsync([FromRoute] string conversationId, [FromRoute] string filename)
    {
        return await GetFolderFileContentAsync(conversationId, filename, FolderType.Conversation);
    }
    
    [HttpDelete("conversations/{conversationId}/files/{filename}")]
    public async Task<IActionResult> DeleteConversationFileAsync([FromRoute] string conversationId, [FromRoute] string filename)
    {
        return await DeleteFileAsync(conversationId, "ConversationId", FolderType.Conversation, filename);
    }
    #endregion

    #region Common Methods
    private async Task<IActionResult> GetFileContentCoreAsync(string filename)
    {
        var result = await fileStorageService.GetFileContentAsync(filename);

        if(result == null || !System.IO.File.Exists(result.Value.FilePath))
            return NotFound();

        var fileStream = new FileStream(result.Value.FilePath, FileMode.Open, FileAccess.Read);
        var contentType = result.Value.FileInfo.MimeType;

        return new FileStreamResult(fileStream, contentType)
        {
            FileDownloadName = filename,
            EnableRangeProcessing = true
        };
    }

    private async Task<IActionResult> GetFilesByFolderAsync(string folderId, string folderIdName, FolderType folderType)
    {
        if (string.IsNullOrWhiteSpace(folderId))
            return BadRequest($"{folderIdName} is required.");

        try
        {
            var files = await fileStorageService.GetFilesByFolderAsync(folderId);
            return Ok(files);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving files for {FolderType} {FolderId}", folderType.ToString().ToLowerInvariant(), folderId);
            return StatusCode(500, "An error occurred while retrieving files.");
        }
    }

    private async Task<IActionResult> GetFolderFileContentAsync(string folderId, string filename, FolderType folderType)
    {
        if (string.IsNullOrWhiteSpace(folderId))
            return BadRequest($"{folderType.ToString()} ID is required.");

        if (string.IsNullOrWhiteSpace(filename))
            return BadRequest("Filename is required.");

        try
        {
            var virtualFilename = $"{folderId}/{filename}";
            return await GetFileContentCoreAsync(virtualFilename);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving file content for {FolderId}/{Filename}", folderId, filename);
            return StatusCode(500, "An error occurred while retrieving the file.");
        }
    }
    
    private async Task<IActionResult> DeleteFileAsync(string folderId, string folderIdName, FolderType folderType, string filename)
    {
        if (string.IsNullOrWhiteSpace(folderId))
            return BadRequest($"{folderIdName} is required.");

        try
        {
            var files = await fileStorageService.GetFilesByFolderAsync(folderId);
            
            var virtualFilename = $"/{folderId}/{filename}";
            var fileToDelete = files.FirstOrDefault(f => f.FileName == virtualFilename);
            if (fileToDelete != null)
            {
                await fileStorageService.DeleteFileAsync(fileToDelete.Id);
            }
            
            IDictionary<string, object>? args = null;
            switch (folderType)
            {
                case FolderType.Global:
                    var globalArgs = GlobalDocumentCollectionArgs.Default;
                    globalArgs.SetCategoryId(folderId);
                    globalArgs["FileName"] = virtualFilename;
                    args = globalArgs;
                    break;
                case FolderType.Conversation:
                    var conversationArgs = ConversationDocumentCollectionArgs.Default;
                    conversationArgs.SetConversationId(folderId);
                    conversationArgs["FileName"] = virtualFilename;
                    args = conversationArgs;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(folderType), folderType, null);
            }

            await documentCollectionService.DeleteDocumentAsync(args);

            return Ok(new { message = "Files deleted successfully", folderId, filename });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting file {FileName} for {FolderType} {FolderId}", folderType.ToString().ToLowerInvariant(), folderId, filename);
            return StatusCode(500, "An error occurred while deleting file.");
        }
    }
    
    private async Task<(bool Success, string? ErrorMessage, string? VirtualFilename)> ProcessFileUploadAsync(
        IFormFile? file, string folderId, FolderType folderType)
    {
        if (string.IsNullOrWhiteSpace(folderId))
            return (false, $"{folderType.ToString()} ID is required.", null);

        if (file == null || file.Length == 0)
            return (false, "No file uploaded.", null);

        if (file.Length > MaxFileSize)
            return (false, "File size exceeds 100MB limit.", null);

        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (fileExtension != ".pdf")
            return (false, "Only PDF files are allowed.", null);

        var tempFilePath = Path.GetTempFileName() + fileExtension;

        try
        {
            var mimeType = file.ContentType;

            var fileName = Path.GetFileName(file.FileName);
            var virtualFilename = $"/{folderId}/{fileName}";

            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                var fileContent = memoryStream.ToArray();

                await fileStorageService.UpsertFileAsync(virtualFilename, mimeType, fileContent);
                await System.IO.File.WriteAllBytesAsync(tempFilePath, fileContent);

                fileContent = null;
                GC.Collect();
            }

            IDictionary<string, object>? args = null;
            switch (folderType)
            {
                case FolderType.Global:
                    var globalArgs = GlobalDocumentCollectionArgs.Default;
                    globalArgs.SetCategoryId(folderId);
                    globalArgs["FileName"] = virtualFilename;
                    args = globalArgs;
                    break;
                case FolderType.Conversation:
                    var conversationArgs = ConversationDocumentCollectionArgs.Default;
                    conversationArgs.SetConversationId(folderId);
                    conversationArgs["FileName"] = virtualFilename;
                    args = conversationArgs;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(folderType), folderType, null);
            }
            
            await documentCollectionService.LoadDocumentAsync(tempFilePath, args);
            
            return (true, null, virtualFilename);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading file");
            return (false, "An error occurred while uploading the file.", null);
        }
        finally
        {
            System.IO.File.Delete(tempFilePath);
        }
    }
    #endregion
}