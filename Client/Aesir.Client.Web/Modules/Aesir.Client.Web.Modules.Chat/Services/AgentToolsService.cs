using System.Collections.Concurrent;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Service for managing agent tools with caching support.
/// </summary>
public class AgentToolsService : IAgentToolsService
{
    private readonly IApiClient _apiClient;
    private readonly ConcurrentDictionary<Guid, IReadOnlyList<AesirToolBase>> _cache = new();

    public AgentToolsService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AesirToolBase>> GetAgentToolsAsync(Guid agentId, CancellationToken ct = default)
    {
        // Check cache first
        if (_cache.TryGetValue(agentId, out var cachedTools))
        {
            return cachedTools;
        }

        // Fetch from API
        var tools = await _apiClient.GetAsync<List<AesirToolBase>>($"/configuration/agents/{agentId}/tools", ct);
        var toolsList = tools?.AsReadOnly() ?? (IReadOnlyList<AesirToolBase>)Array.Empty<AesirToolBase>();

        // Cache the result
        _cache.TryAdd(agentId, toolsList);

        return toolsList;
    }

    /// <inheritdoc />
    public bool HasRagTool(IEnumerable<AesirToolBase> tools)
    {
        return tools.Any(t =>
            string.Equals(t.Name, AesirTools.RagToolName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.ToolName, AesirTools.RagToolName, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<bool> AgentHasRagToolAsync(Guid agentId, CancellationToken ct = default)
    {
        var tools = await GetAgentToolsAsync(agentId, ct);
        return HasRagTool(tools);
    }

    /// <inheritdoc />
    public void InvalidateCache(Guid agentId)
    {
        _cache.TryRemove(agentId, out _);
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        _cache.Clear();
    }
}
