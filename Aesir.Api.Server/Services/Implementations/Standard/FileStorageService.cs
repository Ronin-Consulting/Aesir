using Aesir.Api.Server.Data;
using Aesir.Api.Server.Models;
using Dapper;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Provides file storage services, supporting operations such as file creation, retrieval,
/// updates, and deletion. Uses a database backend for persistence.
/// </summary>
/// <param name="logger">The logger instance used for logging service operations.</param>
/// <param name="dbContext">The database context interface for accessing and manipulating stored files and metadata.</param>
public class FileStorageService(ILogger<FileStorageService> logger, IDbContext dbContext) : IFileStorageService
{
    /// <summary>
    /// A logger instance used for logging and tracking operations within the FileStorageService.
    /// </summary>
    private readonly ILogger<FileStorageService> _logger = logger;

    /// <summary>
    /// Represents the database context used for data access operations within the file storage service.
    /// Provides methods and functionalities to interact with the underlying database, such as executing queries
    /// and managing transactions.
    /// </summary>
    private readonly IDbContext _dbContext = dbContext;

    /// <summary>
    /// Inserts or updates a file record in the database with the provided information.
    /// </summary>
    /// <param name="filename">The name of the file.</param>
    /// <param name="mimeType">The MIME type of the file.</param>
    /// <param name="content">The binary content of the file.</param>
    /// <returns>The number of rows affected by the operation.</returns>
    public async Task<int> UpsertFileAsync(string filename, string mimeType, byte[] content)
    {
        const string sql = @"
            INSERT INTO aesir.aesir_file_storage (file_name, mime_type, file_size, file_content)
            VALUES (@FileName, @MimeType, @FileSize, @Content)
            ON CONFLICT (file_name) DO UPDATE SET
                mime_type = @MimeType,
                file_size = @FileSize,
                file_content = @Content,
                updated_at = CURRENT_TIMESTAMP
        ";
        
        return await _dbContext.UnitOfWorkAsync(async (connection) => 
        await connection.ExecuteAsync(sql, new
        {
            FileName = filename, 
            MimeType = mimeType, 
            FileSize = content.Length,
            Content = content
        }), true);
    }

    /// <summary>
    /// Retrieves file information from the storage by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the file to retrieve information for.</param>
    /// <returns>
    /// An instance of <see cref="AesirFileInfo"/> containing the file's metadata if found, or null if the file does not exist.
    /// </returns>
    public async Task<AesirFileInfo?> GetFileInfoAsync(Guid id)
    {
        const string sql = @"
            SELECT id, file_name as FileName, mime_type as MimeType, 
                file_size as FileSize, created_at as CreatedAt, updated_at as UpdatedAt
            FROM aesir.aesir_file_storage
            WHERE id = @Id::uuid
        ";
        
        return await _dbContext.UnitOfWorkAsync(async (connection) => 
        await connection.QueryFirstOrDefaultAsync<AesirFileInfo>(sql, new
        {
            Id = id
        }));
    }

    /// <summary>
    /// Retrieves information about a file by its filename from the storage system.
    /// </summary>
    /// <param name="filename">The name of the file to retrieve information for.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains an <c>AesirFileInfo</c> object with the file information,
    /// or <c>null</c> if the file is not found.
    /// </returns>
    public async Task<AesirFileInfo?> GetFileInfoAsync(string filename)
    {
        const string sql = @"
            SELECT id, file_name as FileName, mime_type as MimeType, 
                file_size as FileSize, created_at as CreatedAt, updated_at as UpdatedAt
            FROM aesir.aesir_file_storage
            WHERE file_name = @FileName
        ";
        
        return await _dbContext.UnitOfWorkAsync(async (connection) => 
            await connection.QueryFirstOrDefaultAsync<AesirFileInfo>(sql, new
            {
                FileName = filename
            }));
    }

    /// Retrieves a collection of file information from the file storage system asynchronously.
    /// <returns>
    /// An enumerable collection of AesirFileInfo objects representing the metadata of files
    /// stored in the file storage system.
    /// </returns>
    public async Task<IEnumerable<AesirFileInfo>> GetFilesAsync()
    {
        const string sql = @"
            SELECT id, file_name as FileName, mime_type as MimeType, 
                file_size as FileSize, created_at as CreatedAt, updated_at as UpdatedAt
            FROM aesir.aesir_file_storage
        ";

        return await _dbContext.UnitOfWorkAsync(async (connection) =>
            await connection.QueryAsync<AesirFileInfo>(sql));
    }

    /// <summary>
    /// Retrieves a collection of file information objects that belong to a specified folder.
    /// </summary>
    /// <param name="folder">The name of the folder for which files need to be retrieved. The folder name is used as part of the query pattern.</param>
    /// <returns>A task representing the asynchronous operation that returns an enumerable collection of <see cref="AesirFileInfo"/> objects containing file information for the specified folder.</returns>
    public async Task<IEnumerable<AesirFileInfo>> GetFilesByFolderAsync(string folder)
    {
        const string sql = @"
            SELECT id, file_name as FileName, mime_type as MimeType, 
                file_size as FileSize, created_at as CreatedAt, updated_at as UpdatedAt
            FROM aesir.aesir_file_storage
            WHERE file_name LIKE @FolderPattern
        ";

        return await _dbContext.UnitOfWorkAsync(async (connection) =>
            await connection.QueryAsync<AesirFileInfo>(sql, new { FolderPattern = $"/{folder}/%" }));
    }

    /// <summary>
    /// Deletes a file from the storage based on the specified unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the file to delete.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// The task result contains a boolean value indicating whether the file
    /// was successfully deleted or not.
    /// </returns>
    public async Task<bool> DeleteFileAsync(Guid id)
    {
        const string sql = @"DELETE FROM aesir.aesir_file_storage WHERE id = @Id::uuid";
        
        return await _dbContext.UnitOfWorkAsync(async (connection) => 
            await connection.ExecuteAsync(sql, new
            {
                Id = id
            }), true) > 1;
    }

    /// <summary>
    /// Deletes all files stored in the file storage that are within or under the specified folder.
    /// </summary>
    /// <param name="folder">The name of the folder whose associated files should be deleted. This must match the folder structure used in the file storage system.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task returns a boolean indicating whether files were successfully deleted (true) or not (false).
    /// </returns>
    public async Task<bool> DeleteFilesByFolderAsync(string folder)
    {
        const string sql = @"DELETE FROM aesir.aesir_file_storage WHERE file_name LIKE @FolderPattern";
        
        return await _dbContext.UnitOfWorkAsync(async (connection) => 
            await connection.ExecuteAsync(sql, new
            {
                FolderPattern = $"/{folder}/%"
            }), true) > 0;
    }

    /// <summary>
    /// Retrieves the content of a file identified by its filename and returns a temporary file handle that manages cleanup.
    /// </summary>
    /// <param name="filename">The name of the file to retrieve content for.</param>
    /// <returns>A tuple containing the temporary file handle and file metadata, or null if the file does not exist.</returns>
    public async Task<(TempFileHandle TempFile, AesirFileInfo FileInfo)?> GetFileContentAsync(string filename)
    {
        var fileInfo = await GetFileInfoAsync(filename);
        
        if (fileInfo == null)
            return null;
        
        const string sql = @"
            SELECT file_content as Content
            FROM aesir.aesir_file_storage
            WHERE file_name = @FileName
        ";
        
        var result = await _dbContext.UnitOfWorkAsync(async (connection) => 
            await connection.QueryFirstOrDefaultAsync<FileContent>(sql, new { FileName = filename }));
        
        var extension = Path.GetExtension(fileInfo.FileName);
        var tempFile = await TempFileHandle.CreateAsync(extension, result!.Content);
        
        return (tempFile, fileInfo);
    }

    /// <summary>
    /// Retrieves the content of a file and its associated information from the data storage.
    /// </summary>
    /// <param name="id">The unique identifier of the file to retrieve.</param>
    /// <returns>
    /// A tuple containing the temporary file handle and file information if the file exists;
    /// otherwise, returns <c>null</c>.
    /// </returns>
    public async Task<(TempFileHandle TempFile, AesirFileInfo FileInfo)?> GetFileContentAsync(Guid id)
    {
        var fileInfo = await GetFileInfoAsync(id);
        
        if (fileInfo == null)
            return null;
        
        const string sql = @"
            SELECT  file_content as Content
            FROM aesir.aesir_file_storage
            WHERE id = @Id::uuid
        ";

        var result = await _dbContext.UnitOfWorkAsync(async (connection) => 
            await connection.QueryFirstOrDefaultAsync<FileContent>(sql, new { Id = id }));
        
        var extension = Path.GetExtension(fileInfo.FileName);
        var tempFile = await TempFileHandle.CreateAsync(extension, result!.Content);

        return (tempFile, fileInfo);
    }

    /// <summary>
    /// Represents the content of a file stored in the database.
    /// </summary>
    /// <param name="Content">The binary content of the file.</param>
    record FileContent(byte[] Content);
}