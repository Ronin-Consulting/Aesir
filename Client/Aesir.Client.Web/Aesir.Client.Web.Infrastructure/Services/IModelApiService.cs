using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Service interface for Model API operations.
/// Provides methods for retrieving AI models from inference engines.
/// </summary>
public interface IModelApiService
{
    /// <summary>
    /// Gets available models from an inference engine by category.
    /// </summary>
    /// <param name="inferenceEngineId">The ID of the inference engine to query.</param>
    /// <param name="category">The model category to filter by (Embedding, Chat, Vision).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of available models.</returns>
    Task<ApiResult<IReadOnlyList<AesirModelInfo>>> GetModelsAsync(
        Guid inferenceEngineId,
        ModelCategory category,
        CancellationToken ct = default);
}
