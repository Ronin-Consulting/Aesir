using System.Net;
using System.Runtime.CompilerServices;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Models;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Implementation of IChatApiService that communicates with the AESIR Chat API.
/// </summary>
public class ChatApiService : IChatApiService
{
    private readonly IApiClient _apiClient;
    private const string ChatBaseUrl = "/chat/completions";
    private const string HistoryBaseUrl = "/chat/history";

    public ChatApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async IAsyncEnumerable<AesirChatStreamedResult> StreamChatAsync(
        AesirAgentChatRequestBase request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var result in _apiClient.StreamPostAsync<AesirChatStreamedResult>(
            $"{ChatBaseUrl}/agent/streamed", request, ct))
        {
            yield return result;
        }
    }

    public async Task<ApiResult<AesirChatResult?>> ChatAsync(
        AesirAgentChatRequestBase request,
        CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.PostAsync<AesirChatResult>(
                $"{ChatBaseUrl}/agent", request, ct);
            return result;
        });
    }

    public async Task<ApiResult<IReadOnlyList<AesirChatSessionItem>>> GetChatSessionsAsync(
        string userId,
        CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<List<AesirChatSessionItem>>(
                $"{HistoryBaseUrl}/user/{Uri.EscapeDataString(userId)}", ct);
            return (IReadOnlyList<AesirChatSessionItem>)(result ?? []);
        });
    }

    public async Task<ApiResult<AesirChatSession?>> GetChatSessionAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<AesirChatSession>(
                $"{HistoryBaseUrl}/{sessionId}", ct);
            return result;
        });
    }

    public async Task<ApiResult> UpdateChatSessionTitleAsync(
        Guid sessionId,
        string title,
        CancellationToken ct = default)
    {
        return await ExecuteVoidAsync(async () =>
        {
            await _apiClient.PutAsync<object>(
                $"{HistoryBaseUrl}/{sessionId}/{Uri.EscapeDataString(title)}",
                new { }, // Empty body, title is in URL
                ct);
        });
    }

    public async Task<ApiResult> DeleteChatSessionAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        return await ExecuteVoidAsync(async () =>
        {
            var success = await _apiClient.DeleteAsync($"{HistoryBaseUrl}/{sessionId}", ct);
            if (!success) throw new InvalidOperationException($"Failed to delete chat session {sessionId}");
        });
    }

    public async Task<ApiResult<IReadOnlyList<AesirChatSessionItem>>> SearchChatSessionsAsync(
        string userId,
        string searchTerm,
        CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<List<AesirChatSessionItem>>(
                $"{HistoryBaseUrl}/user/{Uri.EscapeDataString(userId)}/search/{Uri.EscapeDataString(searchTerm)}",
                ct);
            return (IReadOnlyList<AesirChatSessionItem>)(result ?? []);
        });
    }

    public async Task<ApiResult> UpdateSessionStarredAsync(
        Guid sessionId,
        bool isStarred,
        CancellationToken ct = default)
    {
        return await ExecuteVoidAsync(async () =>
        {
            await _apiClient.PutAsync<object>(
                $"{HistoryBaseUrl}/{sessionId}/star/{isStarred.ToString().ToLowerInvariant()}",
                new { }, // Empty body, isStarred is in URL
                ct);
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
