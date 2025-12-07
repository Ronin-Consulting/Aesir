using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Settings.Services;

/// <summary>
/// Service for managing inference engines.
/// </summary>
public class InferenceEngineService : IInferenceEngineService
{
    private readonly IConfigurationApiService _apiService;

    public InferenceEngineService(IConfigurationApiService apiService)
    {
        _apiService = apiService;
    }

    /// <inheritdoc />
    public Task<ApiResult<IReadOnlyList<AesirInferenceEngineBase>>> GetAllAsync(CancellationToken ct = default)
    {
        return _apiService.GetInferenceEnginesAsync(ct);
    }

    /// <inheritdoc />
    public Task<ApiResult<AesirInferenceEngineBase>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _apiService.GetInferenceEngineAsync(id, ct);
    }

    /// <inheritdoc />
    public Task<ApiResult<Guid>> CreateAsync(AesirInferenceEngineBase engine, CancellationToken ct = default)
    {
        return _apiService.CreateInferenceEngineAsync(engine, ct);
    }

    /// <inheritdoc />
    public Task<ApiResult> UpdateAsync(AesirInferenceEngineBase engine, CancellationToken ct = default)
    {
        return _apiService.UpdateInferenceEngineAsync(engine, ct);
    }

    /// <inheritdoc />
    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return _apiService.DeleteInferenceEngineAsync(id, ct);
    }
}
