using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Settings.Services;

/// <summary>
/// Service implementation for managing MCP servers.
/// </summary>
public class McpServerService : IMcpServerService
{
    private readonly IConfigurationApiService _apiService;

    public McpServerService(IConfigurationApiService apiService)
    {
        _apiService = apiService;
    }

    /// <inheritdoc />
    public Task<ApiResult<IReadOnlyList<AesirMcpServerBase>>> GetAllAsync(CancellationToken ct = default)
    {
        return _apiService.GetMcpServersAsync(ct);
    }

    /// <inheritdoc />
    public Task<ApiResult<AesirMcpServerBase>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _apiService.GetMcpServerAsync(id, ct);
    }

    /// <inheritdoc />
    public Task<ApiResult<Guid>> CreateAsync(AesirMcpServerBase server, CancellationToken ct = default)
    {
        return _apiService.CreateMcpServerAsync(server, ct);
    }

    /// <inheritdoc />
    public Task<ApiResult> UpdateAsync(AesirMcpServerBase server, CancellationToken ct = default)
    {
        return _apiService.UpdateMcpServerAsync(server, ct);
    }

    /// <inheritdoc />
    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return _apiService.DeleteMcpServerAsync(id, ct);
    }

    /// <inheritdoc />
    public Task<ApiResult<IReadOnlyList<AesirMcpServerToolBase>>> DiscoverToolsAsync(Guid serverId, CancellationToken ct = default)
    {
        return _apiService.GetMcpServerToolsAsync(serverId, ct);
    }
}
