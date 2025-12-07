using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Settings.Services;

/// <summary>
/// Service interface for managing MCP servers.
/// </summary>
public interface IMcpServerService
{
    /// <summary>
    /// Gets all MCP servers.
    /// </summary>
    Task<ApiResult<IReadOnlyList<AesirMcpServerBase>>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a specific MCP server by ID.
    /// </summary>
    Task<ApiResult<AesirMcpServerBase>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new MCP server.
    /// </summary>
    Task<ApiResult<Guid>> CreateAsync(AesirMcpServerBase server, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing MCP server.
    /// </summary>
    Task<ApiResult> UpdateAsync(AesirMcpServerBase server, CancellationToken ct = default);

    /// <summary>
    /// Deletes an MCP server.
    /// </summary>
    Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Discovers tools from an MCP server.
    /// </summary>
    Task<ApiResult<IReadOnlyList<AesirMcpServerToolBase>>> DiscoverToolsAsync(Guid serverId, CancellationToken ct = default);
}
