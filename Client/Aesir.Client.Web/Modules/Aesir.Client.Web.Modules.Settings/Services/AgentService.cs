using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Settings.Services;

/// <summary>
/// Service implementation for managing agents.
/// </summary>
public class AgentService : IAgentService
{
    private readonly IConfigurationApiService _apiService;

    public AgentService(IConfigurationApiService apiService)
    {
        _apiService = apiService;
    }

    /// <inheritdoc />
    public Task<ApiResult<IReadOnlyList<AesirAgentBase>>> GetAllAsync(CancellationToken ct = default)
    {
        return _apiService.GetAgentsAsync(ct);
    }

    /// <inheritdoc />
    public Task<ApiResult<AesirAgentBase>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _apiService.GetAgentAsync(id, ct);
    }

    /// <inheritdoc />
    public Task<ApiResult<Guid>> CreateAsync(AesirAgentBase agent, CancellationToken ct = default)
    {
        return _apiService.CreateAgentAsync(agent, ct);
    }

    /// <inheritdoc />
    public Task<ApiResult> UpdateAsync(AesirAgentBase agent, CancellationToken ct = default)
    {
        return _apiService.UpdateAgentAsync(agent, ct);
    }

    /// <inheritdoc />
    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return _apiService.DeleteAgentAsync(id, ct);
    }

    /// <inheritdoc />
    public Task<ApiResult<IReadOnlyList<AesirToolBase>>> GetAgentToolsAsync(Guid agentId, CancellationToken ct = default)
    {
        return _apiService.GetAgentToolsAsync(agentId, ct);
    }

    /// <inheritdoc />
    public Task<ApiResult> UpdateAgentToolsAsync(Guid agentId, IEnumerable<Guid> toolIds, CancellationToken ct = default)
    {
        return _apiService.UpdateAgentToolsAsync(agentId, toolIds, ct);
    }
}
