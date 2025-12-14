using Aesir.Infrastructure.Services;
using Aesir.Modules.Documents.Services.DocumentCollections;
using Aesir.Modules.Storage.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using IDocumentCollectionService = Aesir.Infrastructure.Services.IDocumentCollectionService;

namespace Aesir.Modules.Documents.Controllers;

/// <summary>
/// Controller for managing document collections and associated file operations.
/// </summary>
[ApiController]
[Route("document/collections")]
[Produces("application/json")]
public class DocumentCollectionController(
    ILogger<DocumentCollectionController> logger,
    IFileStorageService fileStorageService,
    IDocumentCollectionService documentCollectionService,
    IConfigurationService configurationService,
    IPdfThumbnailService pdfThumbnailService,
    IChatHistoryService chatHistoryService)
    : ControllerBase
{
    /// <summary>
    /// Represents the maximum allowable file size for file uploads in the controller, specified in bytes.
    /// </summary>
    /// <remarks>
    /// The constant defines a size limit, used to enforce restrictions on the size of files that can be uploaded.
    /// For example, this value is set to 100MB (104857600 bytes) in the current implementation.
    /// It is used in attributes such as <see cref="Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute" />
    /// and <see cref="Microsoft.AspNetCore.Mvc.RequestFormLimitsAttribute" /> to limit request body size.
    /// Additionally, it is validated in file processing logic to ensure compliance with the size restriction.
    /// </remarks>
    private const int MaxFileSize = 104857600; // 100MB

    /// <summary>
    /// Represents the types of folders that can be used for organizing files in the document collection system.
    /// </summary>
    /// <remarks>
    /// This enumeration defines specific folder types to categorize files distinctly.
    /// The two folder types are:
    /// - `Global`: Used for files that are shared or categorized globally.
    /// - `Conversation`: Used for files related to specific conversations or contexts.
    /// </remarks>
    private enum FolderType
    {
        /// <summary>
        /// Represents a folder type designated for global file operations.
        /// Files associated with this folder type are typically managed at a global scope
        /// and are not tied to specific conversations or localized contexts.
        /// </summary>
        Global,

        /// <summary>
        /// Represents a folder type used for storing and managing files linked to a conversation context.
        /// This folder type is specifically designated for organizing conversation-related files
        /// and supports operations such as file upload, retrieval, inline content access, and deletion.
        /// </summary>
        Conversation
    }

    /// <summary>
    /// Asynchronously retrieves the content of a specified file and returns it
    /// as an attachment for download.
    /// </summary>
    /// <param name="filename">The name of the file to retrieve, encoded in the URL.</param>
    /// <returns>An <c>IActionResult</c> containing the file content for download
    /// or an appropriate error response if the file is not found.</returns>
    [HttpGet("file/{filename}/content")]
    public async Task<IActionResult> GetFileContentAsync([FromRoute] string filename)
    {
        return await GetFileContentCoreAsync(Uri.UnescapeDataString(filename));
    }

    /// <summary>
    /// This method returns the file inline, suitable for display directly in the browser,
    /// rather than being automatically downloaded. The file is accessed using the provided
    /// identifier and filename, which will be part of the URL.
    /// </summary>
    /// <param name="id">The unique identifier of the file.</param>
    /// <param name="filename">The name of the file, used as the last segment of the path and browser tab title.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains
    /// an HTTP response, with the inline file content.</returns>
    [HttpGet("file/{id}/{filename}")]
    public async Task<IActionResult> GetFileInlineAsync([FromRoute] string id, [FromRoute] string filename)
    {
        return await GetFileInlineCoreAsync(Uri.UnescapeDataString(id), Uri.UnescapeDataString(filename));
    }

    #region Global Files

    /// <summary>
    /// Uploads a file to the specified global category folder. This method processes the file upload
    /// and ensures the uploaded file meets size and format constraints before storing it in the global file storage.
    /// </summary>
    /// <param name="file">The file to be uploaded. If null or empty, the upload will not be processed.</param>
    /// <param name="categoryId">The ID of the global category where the file will be stored. This is required.</param>
    /// <returns>Returns an HTTP response indicating success or failure of the file upload operation, along with relevant messages.</returns>
    [HttpPost("globals/{categoryId}/upload/file")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
    public async Task<IActionResult> UploadGlobalFileAsync(IFormFile? file, [FromRoute] string categoryId)
    {
        var result = await ProcessFileUploadAsync(file, categoryId, FolderType.Global, HttpContext.RequestAborted);

        if (!result.Success)
            return BadRequest(result.ErrorMessage);

        return Ok(new { message = "File uploaded successfully", fileName = file?.FileName, categoryId });
    }

    /// <summary>
    /// Retrieves a list of global files associated with the specified category.
    /// </summary>
    /// <param name="categoryId">The unique identifier of the category for which global files are to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult with the list of global files.</returns>
    [HttpGet("globals/{categoryId}/files")]
    public async Task<IActionResult> GetGlobalFilesAsync([FromRoute] string categoryId)
    {
        return await GetFilesByFolderAsync(categoryId, "CategoryId", FolderType.Global);
    }

    /// <summary>
    /// This method returns the content of a global file as an attachment.
    /// A sample URL would be
    /// https://aesir.localhost/document/collections/globals/{categoryId}/files/{filename}/content
    /// </summary>
    /// <param name="categoryId">The ID of the category where the global file is located.</param>
    /// <param name="filename">The name of the file to retrieve.</param>
    /// <returns>An <see cref="IActionResult"/> that contains the file content or an error response.</returns>
    [HttpGet("globals/{categoryId}/files/{filename}/content")]
    public async Task<IActionResult> GetGlobalFileContentAsync([FromRoute] string categoryId,
        [FromRoute] string filename)
    {
        return await GetFolderFileContentAsync(categoryId, filename, FolderType.Global);
    }

    /// <summary>
    /// Deletes a global file from a specified category.
    /// </summary>
    /// <param name="categoryId">The identifier of the category from which the file will be deleted.</param>
    /// <param name="filename">The name of the file to delete.</param>
    /// <returns>An asynchronous operation that returns an IActionResult indicating the result of the file deletion process.</returns>
    [HttpDelete("globals/{categoryId}/files/{filename}")]
    public async Task<IActionResult> DeleteGlobalFileAsync([FromRoute] string categoryId, [FromRoute] string filename)
    {
        return await DeleteFileAsync(categoryId, "CategoryId", FolderType.Global, filename);
    }

    #endregion

    #region Conversation Files

    // TODO: Replace with claims-based user ID from authentication context
    private const string CurrentUserId = "blangford@gmail.com";

    /// <summary>
    /// Retrieves a list of files from the current user's conversations only.
    /// This ensures users can only see documents from their own chat sessions.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the action result with the list of files.</returns>
    [HttpGet("conversations/files")]
    public async Task<IActionResult> GetConversationFilesAsync()
    {
        try
        {
            // Get the user's chat sessions to extract conversation IDs
            // TODO: Get userId from claims when authentication is implemented
            var userId = CurrentUserId;

            var userSessions = await chatHistoryService.GetChatSessionsAsync(userId);
            var conversationIds = userSessions
                .Select(s => s.Conversation.Id)
                .Where(id => Guid.TryParse(id, out _))
                .Select(Guid.Parse)
                .ToList();

            if (conversationIds.Count == 0)
            {
                logger.LogDebug("No conversations found for user {UserId}", userId);
                return Ok(Array.Empty<object>());
            }

            // Get files only for the user's conversations
            var files = await fileStorageService.GetFilesByConversationIdsAsync(conversationIds);

            logger.LogDebug("Retrieved {Count} files for user {UserId} across {ConversationCount} conversations",
                files.Count(), userId, conversationIds.Count);

            return Ok(files);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving files for current user");
            return StatusCode(500, "An error occurred while retrieving files.");
        }
    }

    /// <summary>
    /// Uploads a file to the server and associates it with a specific conversation.
    /// The file must comply with the maximum file size and other constraints set by the system.
    /// </summary>
    /// <param name="file">The file being uploaded, provided as an IFormFile object. Null or invalid files will be rejected.</param>
    /// <param name="conversationId">The unique identifier for the conversation to associate the file with.</param>
    /// <returns>An IActionResult indicating the result of the upload operation, including success or failure messages.</returns>
    [HttpPost("conversations/{conversationId}/upload/file")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
    public async Task<IActionResult> UploadConversationFileAsync(IFormFile? file, [FromRoute] string conversationId)
    {
        var result = await ProcessFileUploadAsync(file, conversationId, FolderType.Conversation, HttpContext.RequestAborted);

        if (!result.Success)
            return BadRequest(result.ErrorMessage);

        return Ok(new { message = "File uploaded successfully", fileName = file?.FileName, conversationId });
    }

    /// <summary>
    /// Retrieves a list of files associated with a specific conversation.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the action result with the list of files.</returns>
    [HttpGet("conversations/{conversationId}/files")]
    public async Task<IActionResult> GetConversationFilesAsync([FromRoute] string conversationId)
    {
        return await GetFilesByFolderAsync(conversationId, "ConversationId", FolderType.Conversation);
    }

    /// <summary>
    /// This method retrieves the content of a file associated with a specific conversation as an attachment.
    /// The file can be downloaded using a URL formatted as:
    /// https://aesir.localhost/document/collections/conversations/{conversationId}/files/{filename}/content
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation to which the file belongs.</param>
    /// <param name="filename">The name of the file to retrieve its content.</param>
    /// <returns>An <see cref="IActionResult"/> containing the file content as an attachment, or an error response if retrieval fails.</returns>
    [HttpGet("conversations/{conversationId}/files/{filename}/content")]
    public async Task<IActionResult> GetConversationFileContentAsync([FromRoute] string conversationId,
        [FromRoute] string filename)
    {
        return await GetFolderFileContentAsync(conversationId, filename, FolderType.Conversation);
    }

    /// <summary>
    /// Retrieves the thumbnail for a file associated with a specific conversation.
    /// Returns 404 if no thumbnail exists for the file.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation to which the file belongs.</param>
    /// <param name="filename">The name of the file to retrieve the thumbnail for.</param>
    /// <returns>An <see cref="IActionResult"/> containing the thumbnail image, or an error response if not found.</returns>
    [HttpGet("conversations/{conversationId}/files/{filename}/thumbnail")]
    public async Task<IActionResult> GetConversationFileThumbnailAsync([FromRoute] string conversationId,
        [FromRoute] string filename)
    {
        return await GetFileThumbnailCoreAsync(conversationId, filename, FolderType.Conversation);
    }

    /// <summary>
    /// Retrieves metadata information for a specific file associated with a conversation.
    /// Returns file info without the actual file content.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation to which the file belongs.</param>
    /// <param name="filename">The name of the file to retrieve metadata for.</param>
    /// <returns>An <see cref="IActionResult"/> containing the file metadata, or NotFound if the file doesn't exist.</returns>
    [HttpGet("conversations/{conversationId}/files/{filename}/info")]
    public async Task<IActionResult> GetConversationFileInfoAsync(
        [FromRoute] string conversationId,
        [FromRoute] string filename)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            return BadRequest("Conversation ID is required.");

        if (string.IsNullOrWhiteSpace(filename))
            return BadRequest("Filename is required.");

        try
        {
            var virtualFilename = $"/{conversationId}/{Uri.UnescapeDataString(filename)}";
            var fileInfo = await fileStorageService.GetFileInfoAsync(virtualFilename);

            if (fileInfo == null)
                return NotFound();

            return Ok(new
            {
                fileName = Path.GetFileName(fileInfo.FileName),
                mimeType = fileInfo.MimeType,
                fileSize = fileInfo.FileSize,
                hasThumbnail = fileInfo.HasThumbnail,
                createdAt = fileInfo.CreatedAt
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving file info for {ConversationId}/{Filename}",
                conversationId, filename);
            return StatusCode(500, "An error occurred while retrieving file information.");
        }
    }

    /// <summary>
    /// Deletes the specified file associated with a conversation.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation to which the file belongs.</param>
    /// <param name="filename">The name of the file to be deleted.</param>
    /// <returns>An <see cref="IActionResult"/> indicating the result of the operation.</returns>
    [HttpDelete("conversations/{conversationId}/files/{filename}")]
    public async Task<IActionResult> DeleteConversationFileAsync([FromRoute] string conversationId,
        [FromRoute] string filename)
    {
        return await DeleteFileAsync(conversationId, "ConversationId", FolderType.Conversation, filename);
    }

    /// <summary>
    /// Moves all files from one conversation to another.
    /// This is used when a session ID is assigned after files were uploaded with a temporary conversation ID.
    /// </summary>
    /// <param name="sourceConversationId">The source conversation ID containing the files.</param>
    /// <param name="targetConversationId">The target conversation ID to move files to.</param>
    /// <returns>An <see cref="IActionResult"/> indicating the result of the operation.</returns>
    [HttpPost("conversations/{sourceConversationId}/move/{targetConversationId}")]
    public async Task<IActionResult> MoveConversationFilesAsync(
        [FromRoute] string sourceConversationId,
        [FromRoute] string targetConversationId)
    {
        if (string.IsNullOrWhiteSpace(sourceConversationId))
            return BadRequest("Source conversation ID is required.");

        if (string.IsNullOrWhiteSpace(targetConversationId))
            return BadRequest("Target conversation ID is required.");

        if (sourceConversationId == targetConversationId)
            return Ok(new { message = "Source and target are the same, no migration needed", count = 0 });

        try
        {
            // Get all files from the source conversation
            var sourceFiles = await fileStorageService.GetFilesByFolderAsync(sourceConversationId);
            var fileList = sourceFiles.ToList();

            if (fileList.Count == 0)
            {
                return Ok(new { message = "No files to migrate", count = 0 });
            }

            var migratedCount = 0;

            foreach (var file in fileList)
            {
                try
                {
                    // Get file content
                    var fileContent = await fileStorageService.GetFileContentAsync(file.FileName);
                    if (fileContent == null)
                    {
                        logger.LogWarning("Could not retrieve content for file {FileName}", file.FileName);
                        continue;
                    }

                    // Read file bytes
                    byte[] fileBytes;
                    await using (var fileStream = new FileStream(fileContent.Value.TempFile.FilePath, FileMode.Open, FileAccess.Read))
                    {
                        fileBytes = new byte[fileStream.Length];
                        await fileStream.ReadExactlyAsync(fileBytes.AsMemory());
                    }

                    // Create new filename with target conversation ID
                    var originalFileName = Path.GetFileName(file.FileName.TrimStart('/').Split('/').LastOrDefault() ?? file.FileName);
                    var newVirtualFilename = $"/{targetConversationId}/{originalFileName}";

                    // Upload to new location
                    await fileStorageService.UpsertFileAsync(newVirtualFilename, file.MimeType, fileBytes);

                    // Delete from old location
                    await fileStorageService.DeleteFileAsync(file.Id);

                    // Clean up temp file
                    fileContent.Value.TempFile.Dispose();

                    migratedCount++;
                    logger.LogInformation("Migrated file {FileName} from {Source} to {Target}",
                        originalFileName, sourceConversationId, targetConversationId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error migrating file {FileName}", file.FileName);
                }
            }

            return Ok(new { message = $"Migrated {migratedCount} files", count = migratedCount });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error migrating files from {Source} to {Target}",
                sourceConversationId, targetConversationId);
            return StatusCode(500, "An error occurred while migrating files.");
        }
    }

    #endregion

    #region Common Methods

    /// <summary>
    /// Retrieves the content of the specified file from the file storage.
    /// </summary>
    /// <param name="filename">The name of the file whose content needs to be retrieved.</param>
    /// <returns>An <see cref="IActionResult"/> containing the file content as a file stream
    /// or a NotFound result if the file does not exist.</returns>
    private async Task<IActionResult> GetFileContentCoreAsync(string filename)
    {
        var result = await fileStorageService.GetFileContentAsync(filename);

        if (result == null || !System.IO.File.Exists(result.Value.TempFile.FilePath))
            return NotFound();

        var fileStream = new FileStream(result.Value.TempFile.FilePath, FileMode.Open, FileAccess.Read);
        var contentType = result.Value.FileInfo.MimeType;

        // Create a custom FileStreamResult that disposes the temp file when the stream is disposed
        return new TempFileStreamResult(fileStream, contentType, result.Value.TempFile)
        {
            FileDownloadName = filename,
            EnableRangeProcessing = true
        };
    }

    /// <summary>
    /// This method retrieves a file as an inline stream for viewing directly in the browser.
    /// </summary>
    /// <param name="id">The unique identifier of the file, representing its directory or grouping.</param>
    /// <param name="filename">The name of the file to be retrieved inline.</param>
    /// <returns>An <see cref="IActionResult"/> containing the file stream if successful, or a 404 Not Found response if the file does not exist.</returns>
    private async Task<IActionResult> GetFileInlineCoreAsync(string id, string filename)
    {
        // Files are stored with leading slash: /{id}/{filename}
        var virtualFilename = $"/{id}/{filename}";
        var result = await fileStorageService.GetFileContentAsync(virtualFilename);

        if (result == null || !System.IO.File.Exists(result.Value.TempFile.FilePath))
            return NotFound();

        var fileStream = new FileStream(result.Value.TempFile.FilePath, FileMode.Open, FileAccess.Read);
        var contentType = result.Value.FileInfo.MimeType;

        Response.Headers["Content-Disposition"] = $"inline; filename=\"{filename}\"";
        return new TempFileStreamResult(fileStream, contentType, result.Value.TempFile)
        {
            EnableRangeProcessing = true
        };
    }

    /// <summary>
    /// Retrieves the thumbnail for a file by folder ID and filename.
    /// </summary>
    /// <param name="folderId">The folder ID (conversation ID or category ID).</param>
    /// <param name="filename">The name of the file.</param>
    /// <param name="folderType">The type of folder.</param>
    /// <returns>An <see cref="IActionResult"/> containing the thumbnail image or NotFound if no thumbnail exists.</returns>
    private async Task<IActionResult> GetFileThumbnailCoreAsync(string folderId, string filename, FolderType folderType)
    {
        if (string.IsNullOrWhiteSpace(folderId))
            return BadRequest($"{folderType} ID is required.");

        if (string.IsNullOrWhiteSpace(filename))
            return BadRequest("Filename is required.");

        try
        {
            var virtualFilename = $"/{folderId}/{Uri.UnescapeDataString(filename)}";
            var thumbnail = await fileStorageService.GetFileThumbnailAsync(virtualFilename);

            if (thumbnail == null)
                return NotFound("No thumbnail available for this file.");

            // Return thumbnail with caching headers (thumbnails don't change)
            Response.Headers["Cache-Control"] = "public, max-age=31536000"; // 1 year
            return File(thumbnail.Value.Content, thumbnail.Value.MimeType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving thumbnail for {FolderType} file: {FolderId}/{Filename}",
                folderType.ToString().ToLowerInvariant(), folderId, filename);
            return StatusCode(500, "An error occurred while retrieving the thumbnail.");
        }
    }

    /// <summary>
    /// Retrieves a list of files from all folders.
    /// </summary>
    /// <param name="folderType">The type of folder, such as Global or Conversation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an HTTP response with the list of files or an error status if the retrieval fails.</returns>
    private async Task<IActionResult> GetFilesAllFoldersAsync(FolderType folderType)
    {
        try
        {
            var files = await fileStorageService.GetFilesAsync();
            return Ok(files);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving files for {FolderType}",
                folderType.ToString().ToLowerInvariant());
            return StatusCode(500, "An error occurred while retrieving files.");
        }
    }

    /// <summary>
    /// Retrieves a list of files from a specified folder based on its identifier, type, and name.
    /// </summary>
    /// <param name="folderId">The unique identifier of the folder.</param>
    /// <param name="folderIdName">The name associated with the folder identifier (e.g., "CategoryId" or "ConversationId").</param>
    /// <param name="folderType">The type of folder, such as Global or Conversation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an HTTP response with the list of files or an error status if the retrieval fails.</returns>
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
            logger.LogError(ex, "Error retrieving files for {FolderType} {FolderId}",
                folderType.ToString().ToLowerInvariant(), folderId);
            return StatusCode(500, "An error occurred while retrieving files.");
        }
    }

    /// <summary>
    /// Retrieves the content of a file stored under a specific folder, identified by the folder ID and filename.
    /// </summary>
    /// <param name="folderId">The unique identifier of the folder containing the file.</param>
    /// <param name="filename">The name of the file whose content is to be retrieved.</param>
    /// <param name="folderType">The type of the folder (e.g., Global or Conversation).</param>
    /// <returns>An <see cref="IActionResult"/> containing the file content or an error response.</returns>
    private async Task<IActionResult> GetFolderFileContentAsync(string folderId, string filename, FolderType folderType)
    {
        if (string.IsNullOrWhiteSpace(folderId))
            return BadRequest($"{folderType.ToString()} ID is required.");

        if (string.IsNullOrWhiteSpace(filename))
            return BadRequest("Filename is required.");

        try
        {
            // Files are stored with leading slash: /{folderId}/{filename}
            var virtualFilename = $"/{folderId}/{filename}";
            return await GetFileContentCoreAsync(virtualFilename);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving file content for {FolderId}/{Filename}", folderId, filename);
            return StatusCode(500, "An error occurred while retrieving the file.");
        }
    }

    /// <summary>
    /// Deletes a specified file from the storage folder.
    /// </summary>
    /// <param name="folderId">The unique identifier of the folder containing the file.</param>
    /// <param name="folderIdName">The name of the folder identifier for validation or descriptive purposes.</param>
    /// <param name="folderType">The type of the folder (e.g., Global or Conversation) where the file resides.</param>
    /// <param name="filename">The name of the file to be deleted.</param>
    /// <returns>An IActionResult indicating the result of the delete operation, which could be a success message,
    /// a bad request if required parameters are missing, or an error response if the operation fails.</returns>
    private async Task<IActionResult> DeleteFileAsync(string folderId, string folderIdName, FolderType folderType,
        string filename)
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
            logger.LogError(ex, "Error deleting file {FileName} for {FolderType} {FolderId}",
                folderType.ToString().ToLowerInvariant(), folderId, filename);
            return StatusCode(500, "An error occurred while deleting file.");
        }
    }

    /// <summary>
    /// Processes the file upload by validating the file, determining its storage location based on the folder type,
    /// and storing the file in the specified location.
    /// </summary>
    /// <param name="file">The file to be uploaded, provided as an IFormFile object.</param>
    /// <param name="folderId">The unique identifier of the folder in which the file should be stored.</param>
    /// <param name="folderType">The type of folder where the file will be uploaded, indicating its context (e.g., Global or Conversation).</param>
    /// <returns>
    /// A tuple containing:
    /// - Success: A boolean indicating whether the file upload was successful.
    /// - ErrorMessage: A string containing an error message if the upload fails; null if successful.
    /// - VirtualFilename: A string representing the virtual file path where the uploaded file is stored; null if unsuccessful.
    /// </returns>
    private async Task<(bool Success, string? ErrorMessage, string? VirtualFilename)> ProcessFileUploadAsync(
        IFormFile? file, string folderId, FolderType folderType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folderId))
            return (false, $"{folderType.ToString()} ID is required.", null);

        if (file == null || file.Length == 0)
            return (false, "No file uploaded.", null);

        if (file.Length > MaxFileSize)
            return (false, "File size exceeds 100MB limit.", null);

        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        var tempFilePath = Path.GetTempFileName() + fileExtension;
        string? virtualFilename = null;
        var fileStoredInDb = false;

        try
        {
            // Check for cancellation before starting
            cancellationToken.ThrowIfCancellationRequested();

            var mimeType = file.ContentType;

            var fileName = Path.GetFileName(file.FileName);
            virtualFilename = $"/{folderId}/{fileName}";

            // Stream directly to temp file to reduce memory pressure
            await using (var tempFileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true))
            {
                await file.CopyToAsync(tempFileStream, cancellationToken);
            }

            // Check for cancellation after file copy
            cancellationToken.ThrowIfCancellationRequested();

            // For database storage, read file content in optimized chunks
            byte[] fileContent;
            const int maxMemoryFileSize = 50 * 1024 * 1024; // 50MB threshold

            if (file.Length <= maxMemoryFileSize)
            {
                // Small files: read all at once
                fileContent = await System.IO.File.ReadAllBytesAsync(tempFilePath, cancellationToken);
            }
            else
            {
                // Large files: still need to load for database, but with better memory management
                await using var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192, useAsync: true);
                using var memoryStream = new MemoryStream((int)file.Length);
                await fileStream.CopyToAsync(memoryStream, 8192, cancellationToken);
                fileContent = memoryStream.ToArray();
            }

            try
            {
                // Check for cancellation before DB storage
                cancellationToken.ThrowIfCancellationRequested();

                // Generate thumbnail for PDFs
                byte[]? thumbnailContent = null;
                string? thumbnailMimeType = null;

                if (mimeType == "application/pdf" || fileExtension == ".pdf")
                {
                    try
                    {
                        var thumbnailResult = await pdfThumbnailService.GenerateThumbnailAsync(fileContent, cancellationToken);
                        if (thumbnailResult != null)
                        {
                            thumbnailContent = thumbnailResult.Content;
                            thumbnailMimeType = thumbnailResult.MimeType;
                            logger.LogDebug("Generated thumbnail for PDF: {Filename} ({Width}x{Height})",
                                virtualFilename, thumbnailResult.Width, thumbnailResult.Height);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail the upload if thumbnail generation fails
                        logger.LogWarning(ex, "Failed to generate thumbnail for PDF: {Filename}", virtualFilename);
                    }
                }

                await fileStorageService.UpsertFileWithThumbnailAsync(virtualFilename, mimeType, fileContent,
                    thumbnailContent, thumbnailMimeType);
                fileStoredInDb = true;
            }
            finally
            {
                // Allow large byte array to be eligible for garbage collection
                // The GC will reclaim memory as needed - explicit GC.Collect() calls are an anti-pattern
                fileContent = null!;
            }

            // Check for cancellation after DB storage but before indexing
            cancellationToken.ThrowIfCancellationRequested();

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

            // assume we are using the global RAG vision inference engine and model, but
            // at some point the agent being used may identify its separate version
            var ragVisionModelLocationDescriptor = await GetRagVisionModelLocationDescriptorAsync();

            await documentCollectionService.LoadDocumentAsync(tempFilePath, ragVisionModelLocationDescriptor, args, cancellationToken);

            return (true, null, virtualFilename);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("File upload cancelled for {VirtualFilename}", virtualFilename ?? file?.FileName);

            // Cleanup: if file was stored in DB before cancellation, delete it
            if (fileStoredInDb && !string.IsNullOrEmpty(virtualFilename))
            {
                try
                {
                    var files = await fileStorageService.GetFilesByFolderAsync(folderId);
                    var fileToDelete = files.FirstOrDefault(f => f.FileName == virtualFilename);
                    if (fileToDelete != null)
                    {
                        await fileStorageService.DeleteFileAsync(fileToDelete.Id);
                        logger.LogInformation("Cleaned up cancelled upload: {VirtualFilename}", virtualFilename);
                    }
                }
                catch (Exception cleanupEx)
                {
                    logger.LogWarning(cleanupEx, "Failed to cleanup cancelled upload: {VirtualFilename}", virtualFilename);
                }
            }

            return (false, "File upload was cancelled.", null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading file");
            return (false, $"An error occurred while uploading the file. Message: {ex.Message}", null);
        }
        finally
        {
            if (System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }
        }
    }
    
    /// <summary>
    /// Retrieves the location descriptor of the RAG vision model by accessing general settings.
    /// </summary>
    /// <returns>
    /// A <see cref="ModelLocationDescriptor"/> that contains the inference engine identifier and model identifier
    /// required for locating and working with an embedding model.
    /// </returns>
    private async Task<ModelLocationDescriptor> GetRagVisionModelLocationDescriptorAsync()
    {
        var generalSettings = await configurationService.GetGeneralSettingsAsync();
        
        var inferenceEngineId = generalSettings.RagVisionInferenceEngineId 
                                ?? throw new InvalidOperationException($"RagVisionInferenceEngineId has not been defined in GeneralSettings");
        var modelId = generalSettings.RagVisionModel
                      ?? throw new InvalidOperationException($"RagVisionModel has not been defined in GeneralSettings");

        return new ModelLocationDescriptor(inferenceEngineId, modelId);
    }

    #endregion
}