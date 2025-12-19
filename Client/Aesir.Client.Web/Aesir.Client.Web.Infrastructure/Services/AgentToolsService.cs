using System.Collections.Concurrent;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Service for managing agent tools with caching support.
/// </summary>
public class AgentToolsService : IAgentToolsService
{
    private readonly IApiClient _apiClient;
    private readonly ConcurrentDictionary<Guid, IReadOnlyList<AesirToolBase>> _toolsCache = new();
    private readonly ConcurrentDictionary<Guid, IReadOnlyList<ToolRequest>> _toolRequestsCache = new();
    private IReadOnlyList<AesirMcpServerBase>? _mcpServersCache;
    private readonly SemaphoreSlim _mcpServersLock = new(1, 1);

    public AgentToolsService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AesirToolBase>> GetAgentToolsAsync(Guid agentId, CancellationToken ct = default)
    {
        // Check cache first
        if (_toolsCache.TryGetValue(agentId, out var cachedTools))
        {
            return cachedTools;
        }

        // Fetch from API
        var tools = await _apiClient.GetAsync<List<AesirToolBase>>($"/configuration/agents/{agentId}/tools", ct);
        var toolsList = tools?.AsReadOnly() ?? (IReadOnlyList<AesirToolBase>)Array.Empty<AesirToolBase>();

        // Cache the result
        _toolsCache.TryAdd(agentId, toolsList);

        return toolsList;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ToolRequest>> GetAgentToolRequestsAsync(Guid agentId, CancellationToken ct = default)
    {
        // Check cache first
        if (_toolRequestsCache.TryGetValue(agentId, out var cachedRequests))
        {
            return cachedRequests;
        }

        // Get tools and MCP servers
        var tools = await GetAgentToolsAsync(agentId, ct);
        var mcpServers = await GetMcpServersAsync(ct);

        // Convert tools to ToolRequest objects
        var toolRequests = new List<ToolRequest>();
        foreach (var tool in tools)
        {
            string? mcpServerName = null;

            // For MCP server tools, look up the server name
            if (tool.Type == ToolType.McpServer && tool.McpServerId.HasValue)
            {
                var mcpServer = mcpServers.FirstOrDefault(s => s.Id == tool.McpServerId);
                mcpServerName = mcpServer?.Name;
            }

            var toolRequest = new ToolRequest
            {
                // For MCP tools, use Name; for internal tools, use ToolName
                ToolName = tool.Type == ToolType.McpServer ? tool.Name! : tool.ToolName!,
                McpServerName = mcpServerName
            };

            toolRequests.Add(toolRequest);
        }

        var result = toolRequests.AsReadOnly();

        // Cache the result
        _toolRequestsCache.TryAdd(agentId, result);

        return result;
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
        _toolsCache.TryRemove(agentId, out _);
        _toolRequestsCache.TryRemove(agentId, out _);
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        _toolsCache.Clear();
        _toolRequestsCache.Clear();
        _mcpServersCache = null;
    }

    /// <summary>
    /// Gets MCP servers with caching.
    /// </summary>
    private async Task<IReadOnlyList<AesirMcpServerBase>> GetMcpServersAsync(CancellationToken ct)
    {
        if (_mcpServersCache != null)
        {
            return _mcpServersCache;
        }

        await _mcpServersLock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_mcpServersCache != null)
            {
                return _mcpServersCache;
            }

            var servers = await _apiClient.GetAsync<List<AesirMcpServerBase>>("/configuration/mcpservers", ct);
            _mcpServersCache = servers?.AsReadOnly() ?? (IReadOnlyList<AesirMcpServerBase>)Array.Empty<AesirMcpServerBase>();
            return _mcpServersCache;
        }
        finally
        {
            _mcpServersLock.Release();
        }
    }
}
