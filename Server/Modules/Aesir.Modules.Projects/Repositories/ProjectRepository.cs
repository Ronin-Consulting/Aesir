using Aesir.Infrastructure.Data;
using Aesir.Infrastructure.Models;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Projects.Repositories;

/// <summary>
/// Repository implementation for managing projects.
/// </summary>
public class ProjectRepository(
    ILogger<ProjectRepository> logger,
    IDbContext dbContext) : IProjectRepository
{
    /// <inheritdoc />
    public async Task<AesirProject?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT
                id as Id,
                user_id as UserId,
                name as Name,
                description as Description,
                instructions as Instructions,
                is_active as IsActive,
                is_deleted as IsDeleted,
                deleted_at as DeletedAt,
                deleted_by as DeletedBy,
                created_at as CreatedAt,
                created_by as CreatedBy,
                updated_at as UpdatedAt,
                updated_by as UpdatedBy
            FROM aesir.aesir_project
            WHERE id = @Id AND is_deleted = false";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryFirstOrDefaultAsync<AesirProject>(sql, new { Id = id }));
    }

    /// <inheritdoc />
    public async Task<AesirProject?> GetByNameAsync(string name)
    {
        const string sql = @"
            SELECT
                id as Id,
                user_id as UserId,
                name as Name,
                description as Description,
                instructions as Instructions,
                is_active as IsActive,
                is_deleted as IsDeleted,
                deleted_at as DeletedAt,
                deleted_by as DeletedBy,
                created_at as CreatedAt,
                created_by as CreatedBy,
                updated_at as UpdatedAt,
                updated_by as UpdatedBy
            FROM aesir.aesir_project
            WHERE name = @Name AND is_deleted = false";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryFirstOrDefaultAsync<AesirProject>(sql, new { Name = name }));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AesirProject>> GetByUserIdAsync(string userId)
    {
        const string sql = @"
            SELECT
                id as Id,
                user_id as UserId,
                name as Name,
                description as Description,
                instructions as Instructions,
                is_active as IsActive,
                is_deleted as IsDeleted,
                deleted_at as DeletedAt,
                deleted_by as DeletedBy,
                created_at as CreatedAt,
                created_by as CreatedBy,
                updated_at as UpdatedAt,
                updated_by as UpdatedBy
            FROM aesir.aesir_project
            WHERE user_id = @UserId AND is_deleted = false
            ORDER BY created_at DESC";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirProject>(sql, new { UserId = userId }));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AesirProject>> GetActiveByUserIdAsync(string userId)
    {
        const string sql = @"
            SELECT
                id as Id,
                user_id as UserId,
                name as Name,
                description as Description,
                instructions as Instructions,
                is_active as IsActive,
                is_deleted as IsDeleted,
                deleted_at as DeletedAt,
                deleted_by as DeletedBy,
                created_at as CreatedAt,
                created_by as CreatedBy,
                updated_at as UpdatedAt,
                updated_by as UpdatedBy
            FROM aesir.aesir_project
            WHERE user_id = @UserId AND is_active = true AND is_deleted = false
            ORDER BY created_at DESC";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirProject>(sql, new { UserId = userId }));
    }

    /// <inheritdoc />
    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        const string sql = @"
            SELECT EXISTS(
                SELECT 1 FROM aesir.aesir_project
                WHERE name = @Name AND is_deleted = false
                AND (@ExcludeId IS NULL OR id != @ExcludeId)
            )";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteScalarAsync<bool>(sql, new { Name = name, ExcludeId = excludeId }));
    }

    /// <inheritdoc />
    public async Task<AesirProject> AddAsync(AesirProject project)
    {
        if (project.Id == Guid.Empty)
            project.Id = Guid.NewGuid();

        var now = DateTime.UtcNow;
        project.CreatedAt = now;
        project.UpdatedAt = now;

        const string sql = @"
            INSERT INTO aesir.aesir_project
                (id, user_id, name, description, instructions, is_active, is_deleted,
                 created_at, created_by, updated_at, updated_by)
            VALUES
                (@Id, @UserId, @Name, @Description, @Instructions, @IsActive, @IsDeleted,
                 @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy)";

        await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, project), withTransaction: true);

        logger.LogInformation("Created project {ProjectId} with name '{ProjectName}'",
            project.Id, project.Name);

        return project;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(AesirProject project)
    {
        project.UpdatedAt = DateTime.UtcNow;

        const string sql = @"
            UPDATE aesir.aesir_project
            SET name = @Name,
                description = @Description,
                instructions = @Instructions,
                is_active = @IsActive,
                updated_at = @UpdatedAt,
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = false";

        var rowsAffected = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, project), withTransaction: true);

        logger.LogInformation("Updated project {ProjectId}", project.Id);

        return rowsAffected > 0;
    }

    /// <inheritdoc />
    public async Task<bool> SoftDeleteAsync(Guid id, string deletedBy)
    {
        var now = DateTime.UtcNow;

        const string sql = @"
            UPDATE aesir.aesir_project
            SET is_deleted = true,
                deleted_at = @DeletedAt,
                deleted_by = @DeletedBy,
                updated_at = @UpdatedAt
            WHERE id = @Id AND is_deleted = false";

        var rowsAffected = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, new
            {
                Id = id,
                DeletedAt = now,
                DeletedBy = deletedBy,
                UpdatedAt = now
            }), withTransaction: true);

        logger.LogInformation("Soft deleted project {ProjectId} by user {DeletedBy}", id, deletedBy);

        return rowsAffected > 0;
    }

    /// <inheritdoc />
    public async Task<int> GetConversationCountAsync(Guid projectId)
    {
        const string sql = @"
            SELECT COUNT(*) FROM aesir.aesir_chat_session
            WHERE project_id = @ProjectId";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteScalarAsync<int>(sql, new { ProjectId = projectId }));
    }

    /// <inheritdoc />
    public async Task<bool> AssociateConversationAsync(Guid projectId, Guid conversationId)
    {
        const string sql = @"
            UPDATE aesir.aesir_chat_session
            SET project_id = @ProjectId
            WHERE id = @ConversationId";

        var rowsAffected = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, new { ProjectId = projectId, ConversationId = conversationId }),
            withTransaction: true);

        logger.LogInformation("Associated conversation {ConversationId} with project {ProjectId}",
            conversationId, projectId);

        return rowsAffected > 0;
    }

    /// <inheritdoc />
    public async Task<bool> DisassociateConversationAsync(Guid conversationId)
    {
        const string sql = @"
            UPDATE aesir.aesir_chat_session
            SET project_id = NULL
            WHERE id = @ConversationId";

        var rowsAffected = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, new { ConversationId = conversationId }),
            withTransaction: true);

        logger.LogInformation("Disassociated conversation {ConversationId} from its project", conversationId);

        return rowsAffected > 0;
    }

    /// <inheritdoc />
    public async Task<int> DisassociateAllConversationsAsync(Guid projectId)
    {
        const string sql = @"
            UPDATE aesir.aesir_chat_session
            SET project_id = NULL
            WHERE project_id = @ProjectId";

        var rowsAffected = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, new { ProjectId = projectId }),
            withTransaction: true);

        logger.LogInformation("Disassociated {Count} conversations from project {ProjectId}",
            rowsAffected, projectId);

        return rowsAffected;
    }
}
