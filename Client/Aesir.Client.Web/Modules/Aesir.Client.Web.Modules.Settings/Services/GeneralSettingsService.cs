using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Settings.Services;

/// <summary>
/// Service for managing general settings.
/// </summary>
public class GeneralSettingsService : IGeneralSettingsService
{
    private readonly IConfigurationApiService _apiService;

    public GeneralSettingsService(IConfigurationApiService apiService)
    {
        _apiService = apiService;
    }

    /// <inheritdoc />
    public Task<ApiResult<AesirGeneralSettingsBase>> GetSettingsAsync(CancellationToken ct = default)
    {
        return _apiService.GetGeneralSettingsAsync(ct);
    }

    /// <inheritdoc />
    public Task<ApiResult> UpdateSettingsAsync(AesirGeneralSettingsBase settings, CancellationToken ct = default)
    {
        return _apiService.UpdateGeneralSettingsAsync(settings, ct);
    }

    /// <inheritdoc />
    public async Task<ApiResult> UpdateSettingsAndReloadAsync(AesirGeneralSettingsBase settings, CancellationToken ct = default)
    {
        // First update the settings
        var updateResult = await _apiService.UpdateGeneralSettingsAsync(settings, ct);
        if (!updateResult.IsSuccess)
        {
            return updateResult;
        }

        // Then reload configuration to re-register embedding/vision services
        var reloadResult = await _apiService.ReloadConfigurationAsync(ct);
        if (!reloadResult.IsSuccess)
        {
            // Settings were saved but reload failed - return partial success info
            return ApiResult.Failure($"Settings saved but configuration reload failed: {reloadResult.Error}");
        }

        return ApiResult.Success();
    }
}
