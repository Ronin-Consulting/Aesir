using Aesir.Api.Server.Extensions;
using Aesir.Api.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Api.Server.Controllers;

[ApiController]
[Route("document/collections")]
[Produces("application/json")]
public class DocumentCollectionController : ControllerBase
{
    private readonly ILogger<DocumentCollectionController> _logger;
    private readonly IFileStorageService _fileStorageService;
    private readonly IDocumentCollectionService _documentCollectionService;
    private const int MaxFileSize = 104857600; // 100MB

    private enum FolderType
    {
        Global,
        Conversation
    }

    public DocumentCollectionController(
        ILogger<DocumentCollectionController> logger,
        IFileStorageService fileStorageService, 
        IDocumentCollectionService documentCollectionService)
    {
        _logger = logger;
        _fileStorageService = fileStorageService;
        _documentCollectionService = documentCollectionService;
    }
    
    [HttpGet("file/{filename}/content")]
    public async Task<IActionResult> GetFileContentAsync([FromRoute]string filename)
    {
        return await GetFileContentCoreAsync(filename);
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

    [HttpDelete("globals/{categoryId}/files")]
    public async Task<IActionResult> DeleteGlobalsFilesAsync([FromRoute] string categoryId)
    {
        return await DeleteFilesByFolderAsync(categoryId, "CategoryId", FolderType.Global);
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

    [HttpDelete("conversations/{conversationId}/files")]
    public async Task<IActionResult> DeleteConversationFilesAsync([FromRoute] string conversationId)
    {
        return await DeleteFilesByFolderAsync(conversationId, "ConversationId", FolderType.Conversation);
    }
    #endregion

    #region Common Methods
    private async Task<IActionResult> GetFileContentCoreAsync(string filename)
    {
        var result = await _fileStorageService.GetFileContentAsync(filename);

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
            var files = await _fileStorageService.GetFilesByFolderAsync(folderId);
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving files for {FolderType} {FolderId}", folderType.ToString().ToLowerInvariant(), folderId);
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
            _logger.LogError(ex, "Error retrieving file content for {FolderId}/{Filename}", folderId, filename);
            return StatusCode(500, "An error occurred while retrieving the file.");
        }
    }

    private async Task<IActionResult> DeleteFilesByFolderAsync(string folderId, string folderIdName, FolderType folderType)
    {
        if (string.IsNullOrWhiteSpace(folderId))
            return BadRequest($"{folderIdName} is required.");

        try
        {
            var success = await _fileStorageService.DeleteFilesByFolderAsync(folderId);

            if (success)
                return Ok(new { message = "Files deleted successfully", folderId });
            else
                return NotFound(new { message = $"No files found for the specified {folderType.ToString().ToLowerInvariant()}", folderId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting files for {FolderType} {FolderId}", folderType.ToString().ToLowerInvariant(), folderId);
            return StatusCode(500, "An error occurred while deleting files.");
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
            var virtualFilename = $"{folderId}/{fileName}";

            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                var fileContent = memoryStream.ToArray();

                await _fileStorageService.UpsertFileAsync(virtualFilename, mimeType, fileContent);
                await System.IO.File.WriteAllBytesAsync(tempFilePath, fileContent);

                fileContent = null;
                GC.Collect();
            }

            switch (folderType)
            {
                case FolderType.Global:
                    var globalArgs = GlobalDocumentCollectionArgs.Default;
                    globalArgs.AddCategoryId(folderId);
                    await _documentCollectionService.LoadDocumentAsync(tempFilePath, globalArgs);
                    break;
                case FolderType.Conversation:
                    var conversationArgs = ConversationDocumentCollectionArgs.Default;
                    conversationArgs.AddConversationId(folderId);
                    await _documentCollectionService.LoadDocumentAsync(tempFilePath, conversationArgs);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(folderType), folderType, null);
            }
            
            return (true, null, virtualFilename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return (false, "An error occurred while uploading the file.", null);
        }
        finally
        {
            System.IO.File.Delete(tempFilePath);
        }
    }
    #endregion
}