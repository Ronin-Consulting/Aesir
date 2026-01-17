using System.Net;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Implementation of IResearchSessionApiService that communicates with the AESIR Research API.
/// </summary>
public class ResearchSessionApiService : IResearchSessionApiService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<ResearchSessionApiService>? _logger;
    private const string BaseUrl = "/research/sessions";

    public ResearchSessionApiService(IApiClient apiClient, ILogger<ResearchSessionApiService>? logger = null)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<ApiResult<ResearchSessionListBase>> GetSessionsAsync(string userId = "default", CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<ResearchSessionListBase>($"{BaseUrl}?userId={Uri.EscapeDataString(userId)}", ct);
            return result ?? new ResearchSessionListBase();
        });
    }

    public async Task<ApiResult<ResearchSessionBase>> GetSessionAsync(Guid id, CancellationToken ct = default)
    {
        _logger?.LogDebug("[RESEARCH-API] GetSessionAsync called: {SessionId}", id);
        var startTime = DateTime.UtcNow;

        var result = await ExecuteAsync(async () =>
        {
            var response = await _apiClient.GetAsync<ResearchSessionBase>($"{BaseUrl}/{id}", ct);
            return response ?? throw new InvalidOperationException($"Research session {id} not found");
        });

        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        _logger?.LogDebug("[RESEARCH-API] GetSessionAsync completed in {Elapsed}ms, success={Success}, status={Status}",
            elapsed, result.IsSuccess, result.Value?.Status);

        return result;
    }

    public async Task<ApiResult<ResearchSessionListBase>> GetSessionsByChatSessionAsync(Guid chatSessionId, CancellationToken ct = default)
    {
        _logger?.LogDebug("[RESEARCH-API] GetSessionsByChatSessionAsync called: {ChatSessionId}", chatSessionId);
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<ResearchSessionListBase>($"{BaseUrl}/chat-session/{chatSessionId}", ct);
            return result ?? new ResearchSessionListBase();
        });
    }

    public async Task<ApiResult<ResearchReportBase>> GetReportAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<ResearchReportBase>($"{BaseUrl}/{sessionId}/report", ct);
            return result ?? throw new InvalidOperationException($"Research report for session {sessionId} not found");
        });
    }

    public async Task<ApiResult<string>> GetReportMarkdownAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<string>($"{BaseUrl}/{sessionId}/report/markdown", ct);
            return result ?? throw new InvalidOperationException($"Research report markdown for session {sessionId} not found");
        });
    }

    public async Task<ApiResult<ResearchSessionBase>> StartResearchAsync(CreateResearchSessionRequestBase request, CancellationToken ct = default)
    {
        _logger?.LogInformation("[RESEARCH-API] StartResearchAsync called: TeamId={TeamId}, Query='{Query}'",
            request.TeamId, request.Query?.Substring(0, Math.Min(50, request.Query?.Length ?? 0)));

        var startTime = DateTime.UtcNow;
        var result = await ExecuteAsync(async () =>
        {
            _logger?.LogDebug("[RESEARCH-API] Sending POST to {Url}", BaseUrl);
            var response = await _apiClient.PostAsync<ResearchSessionBase>($"{BaseUrl}", request, ct);
            return response ?? throw new InvalidOperationException("Failed to start research session");
        });

        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        if (result.IsSuccess)
        {
            _logger?.LogInformation("[RESEARCH-API] StartResearchAsync completed in {Elapsed}ms, SessionId={SessionId}, Status={Status}",
                elapsed, result.Value?.Id, result.Value?.Status);
        }
        else
        {
            _logger?.LogWarning("[RESEARCH-API] StartResearchAsync failed after {Elapsed}ms: {Error}, StatusCode={StatusCode}",
                elapsed, result.Error, result.StatusCode);
        }

        return result;
    }

    public async Task<ApiResult<ResearchSessionBase>> SubmitClarificationAsync(Guid sessionId, Dictionary<string, string> answers, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var request = new { Answers = answers };
            var result = await _apiClient.PostAsync<ResearchSessionBase>($"{BaseUrl}/{sessionId}/clarify", request, ct);
            return result ?? throw new InvalidOperationException($"Failed to submit clarification for session {sessionId}");
        });
    }

    public async Task<ApiResult> CancelResearchAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await ExecuteVoidAsync(async () =>
        {
            await _apiClient.PostAsync<object>($"{BaseUrl}/{sessionId}/cancel", new { }, ct);
        });
    }

    public async Task<ApiResult> DeleteSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await ExecuteVoidAsync(async () =>
        {
            var success = await _apiClient.DeleteAsync($"{BaseUrl}/{sessionId}", ct);
            if (!success) throw new InvalidOperationException($"Failed to delete research session {sessionId}");
        });
    }

    public async Task<FileDownloadResult> ExportReportPdfAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await _apiClient.DownloadFileAsync($"{BaseUrl}/{sessionId}/report/export/pdf", ct);
    }

    public async Task<FileDownloadResult> ExportReportWordAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await _apiClient.DownloadFileAsync($"{BaseUrl}/{sessionId}/report/export/word", ct);
    }

    public async Task<ApiResult<List<ExportFormatInfo>>> GetExportFormatsAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<List<ExportFormatInfo>>($"{BaseUrl}/export-formats", ct);
            return result ?? new List<ExportFormatInfo>();
        });
    }

    // Helper methods

    private async Task<ApiResult<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            var result = await action();
            return ApiResult<T>.Success(result);
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning("[RESEARCH-API] HttpRequestException: {Message}, StatusCode={StatusCode}",
                ex.Message, ex.StatusCode);
            var statusCode = ex.StatusCode ?? HttpStatusCode.InternalServerError;
            return ApiResult<T>.Failure(ex.Message, statusCode);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger?.LogWarning("[RESEARCH-API] Request timed out: {Message}", ex.Message);
            return ApiResult<T>.Failure("Request timed out", HttpStatusCode.RequestTimeout);
        }
        catch (TaskCanceledException ex)
        {
            _logger?.LogWarning("[RESEARCH-API] Request cancelled: {Message}", ex.Message);
            return ApiResult<T>.Failure("Request was cancelled", HttpStatusCode.RequestTimeout);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[RESEARCH-API] Unexpected error: {Type} - {Message}", ex.GetType().Name, ex.Message);
            return ApiResult<T>.FromException(ex);
        }
    }

    private async Task<ApiResult> ExecuteVoidAsync(Func<Task> action)
    {
        try
        {
            await action();
            return ApiResult.Success();
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning("[RESEARCH-API] HttpRequestException: {Message}, StatusCode={StatusCode}",
                ex.Message, ex.StatusCode);
            var statusCode = ex.StatusCode ?? HttpStatusCode.InternalServerError;
            return ApiResult.Failure(ex.Message, statusCode);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger?.LogWarning("[RESEARCH-API] Request timed out: {Message}", ex.Message);
            return ApiResult.Failure("Request timed out", HttpStatusCode.RequestTimeout);
        }
        catch (TaskCanceledException ex)
        {
            _logger?.LogWarning("[RESEARCH-API] Request cancelled: {Message}", ex.Message);
            return ApiResult.Failure("Request was cancelled", HttpStatusCode.RequestTimeout);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[RESEARCH-API] Unexpected error: {Type} - {Message}", ex.GetType().Name, ex.Message);
            return ApiResult.FromException(ex);
        }
    }
}
