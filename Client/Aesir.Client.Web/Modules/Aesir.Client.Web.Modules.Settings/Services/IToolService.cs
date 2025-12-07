using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Settings.Services;

/// <summary>
/// Service interface for managing tools.
/// </summary>
public interface IToolService
{
    /// <summary>
    /// Gets all tools.
    /// </summary>
    Task<ApiResult<IReadOnlyList<AesirToolBase>>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a specific tool by ID.
    /// </summary>
    Task<ApiResult<AesirToolBase>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new tool.
    /// </summary>
    Task<ApiResult<Guid>> CreateAsync(AesirToolBase tool, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing tool.
    /// </summary>
    Task<ApiResult> UpdateAsync(AesirToolBase tool, CancellationToken ct = default);

    /// <summary>
    /// Deletes a tool.
    /// </summary>
    Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default);
}
