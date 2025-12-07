using System.Net;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Implementation of IModelApiService that communicates with the AESIR Models API.
/// </summary>
public class ModelApiService : IModelApiService
{
    private readonly IApiClient _apiClient;
    private const string BaseUrl = "/models";

    public ModelApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ApiResult<IReadOnlyList<AesirModelInfo>>> GetModelsAsync(
        Guid inferenceEngineId,
        ModelCategory category,
        CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<List<AesirModelInfo>>(
                $"{BaseUrl}/{inferenceEngineId}/{category}", ct);
            return (IReadOnlyList<AesirModelInfo>)(result ?? []);
        });
    }

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
}
