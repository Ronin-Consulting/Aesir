using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Service interface for managing general settings.
/// </summary>
public interface IGeneralSettingsService
{
    /// <summary>
    /// Gets the current general settings.
    /// </summary>
    Task<ApiResult<AesirGeneralSettingsBase>> GetSettingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates the general settings.
    /// </summary>
    Task<ApiResult> UpdateSettingsAsync(AesirGeneralSettingsBase settings, CancellationToken ct = default);

    /// <summary>
    /// Updates the general settings and reloads the server configuration.
    /// This ensures embedding/vision services are re-registered with new settings.
    /// </summary>
    Task<ApiResult> UpdateSettingsAndReloadAsync(AesirGeneralSettingsBase settings, CancellationToken ct = default);
}
