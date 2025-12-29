using System.Net;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Implementation of IResearchTeamApiService that communicates with the AESIR Research API.
/// </summary>
public class ResearchTeamApiService : IResearchTeamApiService
{
    private readonly IApiClient _apiClient;
    private const string BaseUrl = "/research/teams";

    public ResearchTeamApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ApiResult<IReadOnlyList<ResearchTeamBase>>> GetTeamsAsync(string userId = "blangford@gmail.com", CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<List<ResearchTeamBase>>($"{BaseUrl}?userId={Uri.EscapeDataString(userId)}", ct);
            return (IReadOnlyList<ResearchTeamBase>)(result ?? []);
        });
    }

    public async Task<ApiResult<IReadOnlyList<ResearchTeamBase>>> GetActiveTeamsAsync(string userId = "blangford@gmail.com", CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<List<ResearchTeamBase>>($"{BaseUrl}/active?userId={Uri.EscapeDataString(userId)}", ct);
            return (IReadOnlyList<ResearchTeamBase>)(result ?? []);
        });
    }

    public async Task<ApiResult<ResearchTeamBase>> GetTeamAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<ResearchTeamBase>($"{BaseUrl}/{id}", ct);
            return result ?? throw new InvalidOperationException($"Research team {id} not found");
        });
    }

    public async Task<ApiResult<ResearchTeamBase>> CreateTeamAsync(ResearchTeamBase team, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            // Set default user ID (hardcoded for now)
            team.UserId ??= "blangford@gmail.com";
            var result = await _apiClient.PostAsync<ResearchTeamBase>($"{BaseUrl}", team, ct);
            return result ?? throw new InvalidOperationException("Failed to create research team");
        });
    }

    public async Task<ApiResult<ResearchTeamBase>> UpdateTeamAsync(ResearchTeamBase team, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.PutAsync<ResearchTeamBase>($"{BaseUrl}/{team.Id}", team, ct);
            return result ?? throw new InvalidOperationException($"Failed to update research team {team.Id}");
        });
    }

    public async Task<ApiResult> DeleteTeamAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteVoidAsync(async () =>
        {
            var success = await _apiClient.DeleteAsync($"{BaseUrl}/{id}", ct);
            if (!success) throw new InvalidOperationException($"Failed to delete research team {id}");
        });
    }

    public async Task<ApiResult<ResearchTeamBase>> DuplicateTeamAsync(Guid id, string newName, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var request = new { NewName = newName };
            var result = await _apiClient.PostAsync<ResearchTeamBase>($"{BaseUrl}/{id}/duplicate", request, ct);
            return result ?? throw new InvalidOperationException($"Failed to duplicate research team {id}");
        });
    }

    public async Task<ApiResult<bool>> ValidateAgentsAsync(IEnumerable<Guid> agentIds, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.PostAsync<ValidationResult>($"{BaseUrl}/validate-agents", agentIds.ToList(), ct);
            return result?.IsValid ?? false;
        });
    }

    // Helper methods

    private static async Task<ApiResult<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            var result = await action();
            return ApiResult<T>.Success(result);
        }
        catch (HttpRequestException ex)
        {
            var statusCode = ex.StatusCode ?? HttpStatusCode.InternalServerError;
            return ApiResult<T>.Failure(ex.Message, statusCode);
        }
        catch (Exception ex)
        {
            return ApiResult<T>.FromException(ex);
        }
    }

    private static async Task<ApiResult> ExecuteVoidAsync(Func<Task> action)
    {
        try
        {
            await action();
            return ApiResult.Success();
        }
        catch (HttpRequestException ex)
        {
            var statusCode = ex.StatusCode ?? HttpStatusCode.InternalServerError;
            return ApiResult.Failure(ex.Message, statusCode);
        }
        catch (Exception ex)
        {
            return ApiResult.FromException(ex);
        }
    }

    private class ValidationResult
    {
        public bool IsValid { get; set; }
    }
}
