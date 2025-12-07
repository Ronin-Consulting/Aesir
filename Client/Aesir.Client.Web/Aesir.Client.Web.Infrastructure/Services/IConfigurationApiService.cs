using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Service interface for Configuration API operations.
/// Provides typed methods for managing inference engines, MCP servers, tools, and agents.
/// </summary>
public interface IConfigurationApiService
{
    // System

    /// <summary>
    /// Checks if the system is ready and configured.
    /// </summary>
    Task<ApiResult<AesirConfigurationReadinessBase>> GetSystemReadinessAsync(CancellationToken ct = default);

    /// <summary>
    /// Reloads the server-side configuration and re-evaluates system readiness.
    /// Call this after making configuration changes to update the readiness state.
    /// </summary>
    Task<ApiResult<AesirConfigurationReadinessBase>> ReloadConfigurationAsync(CancellationToken ct = default);

    // General Settings

    /// <summary>
    /// Gets the general settings.
    /// </summary>
    Task<ApiResult<AesirGeneralSettingsBase>> GetGeneralSettingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates the general settings.
    /// </summary>
    Task<ApiResult> UpdateGeneralSettingsAsync(AesirGeneralSettingsBase settings, CancellationToken ct = default);

    // Inference Engines

    /// <summary>
    /// Gets all inference engines.
    /// </summary>
    Task<ApiResult<IReadOnlyList<AesirInferenceEngineBase>>> GetInferenceEnginesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets an inference engine by ID.
    /// </summary>
    Task<ApiResult<AesirInferenceEngineBase>> GetInferenceEngineAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new inference engine.
    /// </summary>
    Task<ApiResult<Guid>> CreateInferenceEngineAsync(AesirInferenceEngineBase engine, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing inference engine.
    /// </summary>
    Task<ApiResult> UpdateInferenceEngineAsync(AesirInferenceEngineBase engine, CancellationToken ct = default);

    /// <summary>
    /// Deletes an inference engine.
    /// </summary>
    Task<ApiResult> DeleteInferenceEngineAsync(Guid id, CancellationToken ct = default);

    // MCP Servers

    /// <summary>
    /// Gets all MCP servers.
    /// </summary>
    Task<ApiResult<IReadOnlyList<AesirMcpServerBase>>> GetMcpServersAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets an MCP server by ID.
    /// </summary>
    Task<ApiResult<AesirMcpServerBase>> GetMcpServerAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new MCP server.
    /// </summary>
    Task<ApiResult<Guid>> CreateMcpServerAsync(AesirMcpServerBase server, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing MCP server.
    /// </summary>
    Task<ApiResult> UpdateMcpServerAsync(AesirMcpServerBase server, CancellationToken ct = default);

    /// <summary>
    /// Deletes an MCP server.
    /// </summary>
    Task<ApiResult> DeleteMcpServerAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets tools discovered from an MCP server.
    /// </summary>
    Task<ApiResult<IReadOnlyList<AesirMcpServerToolBase>>> GetMcpServerToolsAsync(Guid serverId, CancellationToken ct = default);

    // Tools

    /// <summary>
    /// Gets all tools.
    /// </summary>
    Task<ApiResult<IReadOnlyList<AesirToolBase>>> GetToolsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a tool by ID.
    /// </summary>
    Task<ApiResult<AesirToolBase>> GetToolAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new tool.
    /// </summary>
    Task<ApiResult<Guid>> CreateToolAsync(AesirToolBase tool, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing tool.
    /// </summary>
    Task<ApiResult> UpdateToolAsync(AesirToolBase tool, CancellationToken ct = default);

    /// <summary>
    /// Deletes a tool.
    /// </summary>
    Task<ApiResult> DeleteToolAsync(Guid id, CancellationToken ct = default);

    // Agents

    /// <summary>
    /// Gets all agents.
    /// </summary>
    Task<ApiResult<IReadOnlyList<AesirAgentBase>>> GetAgentsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets an agent by ID.
    /// </summary>
    Task<ApiResult<AesirAgentBase>> GetAgentAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new agent.
    /// </summary>
    Task<ApiResult<Guid>> CreateAgentAsync(AesirAgentBase agent, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing agent.
    /// </summary>
    Task<ApiResult> UpdateAgentAsync(AesirAgentBase agent, CancellationToken ct = default);

    /// <summary>
    /// Deletes an agent.
    /// </summary>
    Task<ApiResult> DeleteAgentAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets tools assigned to an agent.
    /// </summary>
    Task<ApiResult<IReadOnlyList<AesirToolBase>>> GetAgentToolsAsync(Guid agentId, CancellationToken ct = default);

    /// <summary>
    /// Updates tools assigned to an agent.
    /// </summary>
    Task<ApiResult> UpdateAgentToolsAsync(Guid agentId, IEnumerable<Guid> toolIds, CancellationToken ct = default);
}
