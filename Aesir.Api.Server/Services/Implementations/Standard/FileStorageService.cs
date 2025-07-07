using Aesir.Api.Server.Data;
using Aesir.Api.Server.Models;
using Dapper;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Provides file storage functionality using a database backend.
/// </summary>
/// <param name="logger">The logger instance for recording operations.</param>
/// <param name="dbContext">The database context for data access.</param>
public class FileStorageService(ILogger<FileStorageService> logger, IDbContext dbContext) : IFileStorageService
{
    private readonly ILogger<FileStorageService> _logger = logger;
    private readonly IDbContext _dbContext = dbContext;

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
    
    public async Task<bool> DeleteFileAsync(Guid id)
    {
        const string sql = @"DELETE FROM aesir.aesir_file_storage WHERE id = @Id::uuid";
        
        return await _dbContext.UnitOfWorkAsync(async (connection) => 
            await connection.ExecuteAsync(sql, new
            {
                Id = id
            }), true) > 1;
    }

    public async Task<bool> DeleteFilesByFolderAsync(string folder)
    {
        const string sql = @"DELETE FROM aesir.aesir_file_storage WHERE file_name LIKE @FolderPattern";
        
        return await _dbContext.UnitOfWorkAsync(async (connection) => 
            await connection.ExecuteAsync(sql, new
            {
                FolderPattern = $"/{folder}/%"
            }), true) > 0;
    }

    public async Task<(string FilePath, AesirFileInfo FileInfo)?> GetFileContentAsync(string filename)
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
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");

        await File.WriteAllBytesAsync(tempFilePath, result!.Content);
        
        return (tempFilePath,fileInfo);
    }
    
    public async Task<(string FilePath, AesirFileInfo FileInfo)?> GetFileContentAsync(Guid id)
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
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");

        await File.WriteAllBytesAsync(tempFilePath, result!.Content);

        return (tempFilePath, fileInfo);
    }
    
    record FileContent(byte[] Content);
}