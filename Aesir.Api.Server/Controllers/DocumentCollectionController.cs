using Aesir.Api.Server.Services;
using Aesir.Api.Server.Services.Implementations.Standard;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Api.Server.Controllers;

/// <summary>
/// Controller for managing document collections and associated file operations.
/// </summary>
[ApiController]
[Route("document/collections")]
[Produces("application/json")]
public class DocumentCollectionController(
    ILogger<DocumentCollectionController> logger,
    IFileStorageService fileStorageService,
    IDocumentCollectionService documentCollectionService)
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
        var result = await ProcessFileUploadAsync(file, categoryId, FolderType.Global);

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

    /// <summary>
    /// Retrieves a list of files from all conversations.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the action result with the list of files.</returns>
    [HttpGet("conversations/files")]
    public async Task<IActionResult> GetConversationFilesAsync()
    {
        return await GetFilesAllFoldersAsync(FolderType.Conversation);
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
        var result = await ProcessFileUploadAsync(file, conversationId, FolderType.Conversation);

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
        var virtualFilename = $"{id}/{filename}";
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
            var virtualFilename = $"{folderId}/{filename}";
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
        IFormFile? file, string folderId, FolderType folderType)
    {
        if (string.IsNullOrWhiteSpace(folderId))
            return (false, $"{folderType.ToString()} ID is required.", null);

        if (file == null || file.Length == 0)
            return (false, "No file uploaded.", null);

        if (file.Length > MaxFileSize)
            return (false, "File size exceeds 100MB limit.", null);

        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        var tempFilePath = Path.GetTempFileName() + fileExtension;

        try
        {
            var mimeType = file.ContentType;

            var fileName = Path.GetFileName(file.FileName);
            var virtualFilename = $"/{folderId}/{fileName}";

            // Stream directly to temp file to reduce memory pressure
            await using (var tempFileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true))
            {
                await file.CopyToAsync(tempFileStream);
            }

            // For database storage, read file content in optimized chunks
            byte[] fileContent;
            const int maxMemoryFileSize = 50 * 1024 * 1024; // 50MB threshold
            
            if (file.Length <= maxMemoryFileSize)
            {
                // Small files: read all at once
                fileContent = await System.IO.File.ReadAllBytesAsync(tempFilePath);
            }
            else
            {
                // Large files: still need to load for database, but with better memory management
                await using var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192, useAsync: true);
                using var memoryStream = new MemoryStream((int)file.Length);
                await fileStream.CopyToAsync(memoryStream, 8192);
                fileContent = memoryStream.ToArray();
            }

            try
            {
                await fileStorageService.UpsertFileAsync(virtualFilename, mimeType, fileContent);
            }
            finally
            {
                // Explicitly clear large memory allocation
                fileContent = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
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
            return (false, $"An error occurred while uploading the file. Message: {ex.Message}", null);
        }
        finally
        {
            System.IO.File.Delete(tempFilePath);
        }
    }

    #endregion
}