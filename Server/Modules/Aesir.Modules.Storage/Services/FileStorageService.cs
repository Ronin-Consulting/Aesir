using Aesir.Infrastructure.Data;
using Aesir.Infrastructure.Models;
using Aesir.Infrastructure.Services;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Storage.Services;

/// <summary>
/// Provides file storage services, supporting operations such as file creation, retrieval,
/// updates, and deletion. Uses a database backend for persistence.
/// </summary>
public class FileStorageService : IFileStorageService
{
    private readonly ILogger<FileStorageService> _logger;
    private readonly IDbContext _dbContext;

    public FileStorageService(ILogger<FileStorageService> logger, IDbContext dbContext)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public async Task<int> UpsertFileAsync(string filename, string mimeType, byte[] content)
    {
        _logger.LogDebug("Upserting file: {FileName} ({MimeType}, {Size} bytes)", filename, mimeType, content.Length);

        const string sql = @"
            INSERT INTO aesir.aesir_file_storage (file_name, mime_type, file_size, file_content)
            VALUES (@FileName, @MimeType, @FileSize, @Content)
            ON CONFLICT (file_name) DO UPDATE SET
                mime_type = @MimeType,
                file_size = @FileSize,
                file_content = @Content,
                updated_at = CURRENT_TIMESTAMP
        ";

        var result = await _dbContext.UnitOfWorkAsync(async (connection) =>
            await connection.ExecuteAsync(sql, new
            {
                FileName = filename,
                MimeType = mimeType,
                FileSize = content.Length,
                Content = content
            }), true);

        _logger.LogInformation("File upserted: {FileName} ({Size} bytes)", filename, content.Length);

        return result;
    }

    /// <inheritdoc />
    public async Task<AesirFileInfo?> GetFileInfoAsync(Guid id)
    {
        _logger.LogDebug("Getting file info by Id: {Id}", id);

        const string sql = @"
            SELECT id, file_name as FileName, mime_type as MimeType,
                file_size as FileSize, has_thumbnail as HasThumbnail,
                created_at as CreatedAt, updated_at as UpdatedAt
            FROM aesir.aesir_file_storage
            WHERE id = @Id::uuid
        ";

        return await _dbContext.UnitOfWorkAsync(async (connection) =>
            await connection.QueryFirstOrDefaultAsync<AesirFileInfo>(sql, new { Id = id }));
    }

    /// <inheritdoc />
    public async Task<AesirFileInfo?> GetFileInfoAsync(string filename)
    {
        _logger.LogDebug("Getting file info by filename: {FileName}", filename);

        const string sql = @"
            SELECT id, file_name as FileName, mime_type as MimeType,
                file_size as FileSize, has_thumbnail as HasThumbnail,
                created_at as CreatedAt, updated_at as UpdatedAt
            FROM aesir.aesir_file_storage
            WHERE file_name = @FileName
        ";

        return await _dbContext.UnitOfWorkAsync(async (connection) =>
            await connection.QueryFirstOrDefaultAsync<AesirFileInfo>(sql, new { FileName = filename }));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AesirFileInfo>> GetFilesAsync()
    {
        _logger.LogDebug("Getting all files");

        const string sql = @"
            SELECT id, file_name as FileName, mime_type as MimeType,
                file_size as FileSize, has_thumbnail as HasThumbnail,
                created_at as CreatedAt, updated_at as UpdatedAt
            FROM aesir.aesir_file_storage
        ";

        var files = await _dbContext.UnitOfWorkAsync(async (connection) =>
            await connection.QueryAsync<AesirFileInfo>(sql));

        _logger.LogDebug("Retrieved {Count} files", files.Count());

        return files;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AesirFileInfo>> GetFilesByFolderAsync(string folder)
    {
        _logger.LogDebug("Getting files by folder: {Folder}", folder);

        const string sql = @"
            SELECT id, file_name as FileName, mime_type as MimeType,
                file_size as FileSize, has_thumbnail as HasThumbnail,
                created_at as CreatedAt, updated_at as UpdatedAt
            FROM aesir.aesir_file_storage
            WHERE file_name LIKE @FolderPattern
        ";

        var files = await _dbContext.UnitOfWorkAsync(async (connection) =>
            await connection.QueryAsync<AesirFileInfo>(sql, new { FolderPattern = $"/{folder}/%" }));

        _logger.LogDebug("Retrieved {Count} files from folder {Folder}", files.Count(), folder);

        return files;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFileAsync(Guid id)
    {
        _logger.LogDebug("Deleting file by Id: {Id}", id);

        const string sql = @"DELETE FROM aesir.aesir_file_storage WHERE id = @Id::uuid";

        var result = await _dbContext.UnitOfWorkAsync(async (connection) =>
            await connection.ExecuteAsync(sql, new { Id = id }), true) > 0;

        if (result)
        {
            _logger.LogInformation("File deleted: {Id}", id);
        }
        else
        {
            _logger.LogWarning("Failed to delete file: {Id}", id);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFilesByFolderAsync(string folder)
    {
        _logger.LogDebug("Deleting files by folder: {Folder}", folder);

        const string sql = @"DELETE FROM aesir.aesir_file_storage WHERE file_name LIKE @FolderPattern";

        var result = await _dbContext.UnitOfWorkAsync(async (connection) =>
            await connection.ExecuteAsync(sql, new { FolderPattern = $"/{folder}/%" }), true) > 0;

        if (result)
        {
            _logger.LogInformation("Files deleted from folder: {Folder}", folder);
        }
        else
        {
            _logger.LogWarning("No files deleted from folder: {Folder}", folder);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<(TempFileHandle TempFile, AesirFileInfo FileInfo)?> GetFileContentAsync(string filename)
    {
        _logger.LogDebug("Getting file content by filename: {FileName}", filename);

        var fileInfo = await GetFileInfoAsync(filename);

        if (fileInfo == null)
        {
            _logger.LogWarning("File not found: {FileName}", filename);
            return null;
        }

        const string sql = @"
            SELECT file_content as Content
            FROM aesir.aesir_file_storage
            WHERE file_name = @FileName
        ";

        var result = await _dbContext.UnitOfWorkAsync(async (connection) =>
            await connection.QueryFirstOrDefaultAsync<FileContent>(sql, new { FileName = filename }));

        if (result == null)
        {
            _logger.LogWarning("File content not found: {FileName}", filename);
            return null;
        }

        var extension = Path.GetExtension(fileInfo.FileName);
        var tempFile = await TempFileHandle.CreateAsync(extension, result.Content);

        _logger.LogDebug("Retrieved file content: {FileName} ({Size} bytes)", filename, result.Content.Length);

        return (tempFile, fileInfo);
    }

    /// <inheritdoc />
    public async Task<(TempFileHandle TempFile, AesirFileInfo FileInfo)?> GetFileContentAsync(Guid id)
    {
        _logger.LogDebug("Getting file content by Id: {Id}", id);

        var fileInfo = await GetFileInfoAsync(id);

        if (fileInfo == null)
        {
            _logger.LogWarning("File not found: {Id}", id);
            return null;
        }

        const string sql = @"
            SELECT file_content as Content
            FROM aesir.aesir_file_storage
            WHERE id = @Id::uuid
        ";

        var result = await _dbContext.UnitOfWorkAsync(async (connection) =>
            await connection.QueryFirstOrDefaultAsync<FileContent>(sql, new { Id = id }));

        if (result == null)
        {
            _logger.LogWarning("File content not found: {Id}", id);
            return null;
        }

        var extension = Path.GetExtension(fileInfo.FileName);
        var tempFile = await TempFileHandle.CreateAsync(extension, result.Content);

        _logger.LogDebug("Retrieved file content: {Id} ({Size} bytes)", id, result.Content.Length);

        return (tempFile, fileInfo);
    }

    /// <summary>
    /// Represents the content of a file stored in the database.
    /// </summary>
    private record FileContent(byte[] Content);

    /// <summary>
    /// Represents thumbnail content stored in the database.
    /// </summary>
    private record ThumbnailContent(byte[] Content, string MimeType);

    /// <inheritdoc />
    public async Task<int> UpsertFileWithThumbnailAsync(string filename, string mimeType, byte[] content,
        byte[]? thumbnailContent, string? thumbnailMimeType)
    {
        _logger.LogDebug("Upserting file with thumbnail: {FileName} ({MimeType}, {Size} bytes, HasThumbnail: {HasThumbnail})",
            filename, mimeType, content.Length, thumbnailContent != null);

        const string sql = @"
            INSERT INTO aesir.aesir_file_storage (file_name, mime_type, file_size, file_content, thumbnail_content, thumbnail_mime_type, has_thumbnail)
            VALUES (@FileName, @MimeType, @FileSize, @Content, @ThumbnailContent, @ThumbnailMimeType, @HasThumbnail)
            ON CONFLICT (file_name) DO UPDATE SET
                mime_type = @MimeType,
                file_size = @FileSize,
                file_content = @Content,
                thumbnail_content = @ThumbnailContent,
                thumbnail_mime_type = @ThumbnailMimeType,
                has_thumbnail = @HasThumbnail,
                updated_at = CURRENT_TIMESTAMP
        ";

        var result = await _dbContext.UnitOfWorkAsync(async (connection) =>
            await connection.ExecuteAsync(sql, new
            {
                FileName = filename,
                MimeType = mimeType,
                FileSize = content.Length,
                Content = content,
                ThumbnailContent = thumbnailContent,
                ThumbnailMimeType = thumbnailMimeType,
                HasThumbnail = thumbnailContent != null
            }), true);

        _logger.LogInformation("File upserted with thumbnail: {FileName} ({Size} bytes, HasThumbnail: {HasThumbnail})",
            filename, content.Length, thumbnailContent != null);

        return result;
    }

    /// <inheritdoc />
    public async Task<(byte[] Content, string MimeType)?> GetFileThumbnailAsync(string filename)
    {
        _logger.LogDebug("Getting file thumbnail: {FileName}", filename);

        const string sql = @"
            SELECT thumbnail_content as Content, thumbnail_mime_type as MimeType
            FROM aesir.aesir_file_storage
            WHERE file_name = @FileName AND has_thumbnail = true
        ";

        var result = await _dbContext.UnitOfWorkAsync(async (connection) =>
            await connection.QueryFirstOrDefaultAsync<ThumbnailContent>(sql, new { FileName = filename }));

        if (result == null)
        {
            _logger.LogDebug("No thumbnail found for file: {FileName}", filename);
            return null;
        }

        _logger.LogDebug("Retrieved thumbnail for file: {FileName} ({Size} bytes)", filename, result.Content.Length);
        return (result.Content, result.MimeType);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateFileThumbnailAsync(string filename, byte[] thumbnailContent, string thumbnailMimeType)
    {
        _logger.LogDebug("Updating thumbnail for file: {FileName}", filename);

        const string sql = @"
            UPDATE aesir.aesir_file_storage
            SET thumbnail_content = @ThumbnailContent,
                thumbnail_mime_type = @ThumbnailMimeType,
                has_thumbnail = true,
                updated_at = CURRENT_TIMESTAMP
            WHERE file_name = @FileName
        ";

        var result = await _dbContext.UnitOfWorkAsync(async (connection) =>
            await connection.ExecuteAsync(sql, new
            {
                FileName = filename,
                ThumbnailContent = thumbnailContent,
                ThumbnailMimeType = thumbnailMimeType
            }), true) > 0;

        if (result)
        {
            _logger.LogInformation("Thumbnail updated for file: {FileName}", filename);
        }
        else
        {
            _logger.LogWarning("Failed to update thumbnail for file: {FileName}", filename);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AesirFileInfo>> GetFilesByConversationIdsAsync(IEnumerable<Guid> conversationIds)
    {
        var conversationIdList = conversationIds.ToList();

        if (conversationIdList.Count == 0)
        {
            _logger.LogDebug("No conversation IDs provided, returning empty result");
            return [];
        }

        _logger.LogDebug("Getting files for {Count} conversations", conversationIdList.Count);

        // Build folder patterns for each conversation ID
        // Files are stored as /{conversationId}/{filename}
        var folderPatterns = conversationIdList.Select(id => $"/{id}/%").ToArray();

        // Build dynamic SQL with OR conditions for each pattern
        var conditions = string.Join(" OR ", folderPatterns.Select((_, i) => $"file_name LIKE @Pattern{i}"));

        var sql = $@"
            SELECT id, file_name as FileName, mime_type as MimeType,
                file_size as FileSize, has_thumbnail as HasThumbnail,
                created_at as CreatedAt, updated_at as UpdatedAt
            FROM aesir.aesir_file_storage
            WHERE {conditions}
        ";

        // Build parameters dynamically
        var parameters = new DynamicParameters();
        for (var i = 0; i < folderPatterns.Length; i++)
        {
            parameters.Add($"Pattern{i}", folderPatterns[i]);
        }

        var files = await _dbContext.UnitOfWorkAsync(async (connection) =>
            await connection.QueryAsync<AesirFileInfo>(sql, parameters));

        _logger.LogDebug("Retrieved {Count} files for {ConversationCount} conversations",
            files.Count(), conversationIdList.Count);

        return files;
    }
}
