using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Settings.Services;

/// <summary>
/// Service interface for managing inference engines.
/// </summary>
public interface IInferenceEngineService
{
    /// <summary>
    /// Gets all inference engines.
    /// </summary>
    Task<ApiResult<IReadOnlyList<AesirInferenceEngineBase>>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a specific inference engine by ID.
    /// </summary>
    Task<ApiResult<AesirInferenceEngineBase>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new inference engine.
    /// </summary>
    Task<ApiResult<Guid>> CreateAsync(AesirInferenceEngineBase engine, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing inference engine.
    /// </summary>
    Task<ApiResult> UpdateAsync(AesirInferenceEngineBase engine, CancellationToken ct = default);

    /// <summary>
    /// Deletes an inference engine.
    /// </summary>
    Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default);
}
