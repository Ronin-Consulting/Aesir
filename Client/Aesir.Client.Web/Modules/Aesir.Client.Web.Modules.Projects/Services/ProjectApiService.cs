using System.Net;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Web.Modules.Projects.Services;

/// <summary>
/// Service implementation for project API operations.
/// </summary>
public class ProjectApiService : IProjectApiService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<ProjectApiService> _logger;

    public ProjectApiService(IApiClient apiClient, ILogger<ProjectApiService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ApiResult<IReadOnlyList<AesirProjectBase>>> GetProjectsAsync(string userId, CancellationToken ct = default)
    {
        try
        {
            var projects = await _apiClient.GetAsync<List<AesirProjectBase>>($"/projects/user/{userId}", ct);
            return ApiResult<IReadOnlyList<AesirProjectBase>>.Success(projects ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get projects for user {UserId}", userId);
            return ApiResult<IReadOnlyList<AesirProjectBase>>.FromException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResult<IReadOnlyList<AesirProjectBase>>> GetActiveProjectsAsync(string userId, CancellationToken ct = default)
    {
        try
        {
            var projects = await _apiClient.GetAsync<List<AesirProjectBase>>($"/projects/user/{userId}/active", ct);
            return ApiResult<IReadOnlyList<AesirProjectBase>>.Success(projects ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active projects for user {UserId}", userId);
            return ApiResult<IReadOnlyList<AesirProjectBase>>.FromException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResult<AesirProjectBase>> GetProjectAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var project = await _apiClient.GetAsync<AesirProjectBase>($"/projects/{id}", ct);
            if (project == null)
            {
                return ApiResult<AesirProjectBase>.Failure("Project not found", HttpStatusCode.NotFound);
            }
            return ApiResult<AesirProjectBase>.Success(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get project {ProjectId}", id);
            return ApiResult<AesirProjectBase>.FromException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResult<AesirProjectBase>> CreateProjectAsync(string userId, CreateProjectRequest request, CancellationToken ct = default)
    {
        try
        {
            var project = await _apiClient.PostAsync<AesirProjectBase>($"/projects/user/{userId}", request, ct);
            if (project == null)
            {
                return ApiResult<AesirProjectBase>.Failure("Failed to create project");
            }
            return ApiResult<AesirProjectBase>.Success(project, HttpStatusCode.Created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create project for user {UserId}", userId);
            return ApiResult<AesirProjectBase>.FromException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResult<AesirProjectBase>> UpdateProjectAsync(Guid id, string userId, UpdateProjectRequest request, CancellationToken ct = default)
    {
        try
        {
            var project = await _apiClient.PutAsync<AesirProjectBase>($"/projects/{id}/user/{userId}", request, ct);
            if (project == null)
            {
                return ApiResult<AesirProjectBase>.Failure("Failed to update project");
            }
            return ApiResult<AesirProjectBase>.Success(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update project {ProjectId}", id);
            return ApiResult<AesirProjectBase>.FromException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResult> DeleteProjectAsync(Guid id, string userId, bool deleteConversations = false, CancellationToken ct = default)
    {
        try
        {
            var request = new { delete_conversations = deleteConversations };
            var success = await _apiClient.DeleteAsync($"/projects/{id}/user/{userId}", ct);
            if (!success)
            {
                return ApiResult.Failure("Failed to delete project");
            }
            return ApiResult.Success(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete project {ProjectId}", id);
            return ApiResult.FromException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResult<int>> GetConversationCountAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var count = await _apiClient.GetAsync<int>($"/projects/{id}/conversations/count", ct);
            return ApiResult<int>.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get conversation count for project {ProjectId}", id);
            return ApiResult<int>.FromException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResult> AssociateConversationAsync(Guid projectId, Guid conversationId, CancellationToken ct = default)
    {
        try
        {
            await _apiClient.PostAsync<object>($"/projects/{projectId}/conversations/{conversationId}", new { }, ct);
            return ApiResult.Success(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to associate conversation {ConversationId} with project {ProjectId}", conversationId, projectId);
            return ApiResult.FromException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResult> DisassociateConversationAsync(Guid projectId, Guid conversationId, CancellationToken ct = default)
    {
        try
        {
            var success = await _apiClient.DeleteAsync($"/projects/{projectId}/conversations/{conversationId}", ct);
            if (!success)
            {
                return ApiResult.Failure("Failed to disassociate conversation");
            }
            return ApiResult.Success(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disassociate conversation {ConversationId} from project {ProjectId}", conversationId, projectId);
            return ApiResult.FromException(ex);
        }
    }
}
