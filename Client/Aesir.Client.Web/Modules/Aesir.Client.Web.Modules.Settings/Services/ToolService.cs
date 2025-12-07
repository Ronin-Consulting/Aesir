using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Settings.Services;

/// <summary>
/// Service implementation for managing tools.
/// </summary>
public class ToolService : IToolService
{
    private readonly IConfigurationApiService _apiService;

    public ToolService(IConfigurationApiService apiService)
    {
        _apiService = apiService;
    }

    /// <inheritdoc />
    public Task<ApiResult<IReadOnlyList<AesirToolBase>>> GetAllAsync(CancellationToken ct = default)
    {
        return _apiService.GetToolsAsync(ct);
    }

    /// <inheritdoc />
    public Task<ApiResult<AesirToolBase>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _apiService.GetToolAsync(id, ct);
    }

    /// <inheritdoc />
    public Task<ApiResult<Guid>> CreateAsync(AesirToolBase tool, CancellationToken ct = default)
    {
        return _apiService.CreateToolAsync(tool, ct);
    }

    /// <inheritdoc />
    public Task<ApiResult> UpdateAsync(AesirToolBase tool, CancellationToken ct = default)
    {
        return _apiService.UpdateToolAsync(tool, ct);
    }

    /// <inheritdoc />
    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return _apiService.DeleteToolAsync(id, ct);
    }
}
