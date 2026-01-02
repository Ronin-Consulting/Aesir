using System.Net;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Implementation of IResearchSessionApiService that communicates with the AESIR Research API.
/// </summary>
public class ResearchSessionApiService : IResearchSessionApiService
{
    private readonly IApiClient _apiClient;
    private const string BaseUrl = "/research/sessions";

    public ResearchSessionApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
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
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<ResearchSessionBase>($"{BaseUrl}/{id}", ct);
            return result ?? throw new InvalidOperationException($"Research session {id} not found");
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
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.PostAsync<ResearchSessionBase>($"{BaseUrl}", request, ct);
            return result ?? throw new InvalidOperationException("Failed to start research session");
        });
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
}
