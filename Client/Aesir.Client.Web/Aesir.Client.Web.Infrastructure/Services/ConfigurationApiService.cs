using System.Net;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Implementation of IConfigurationApiService that communicates with the AESIR Configuration API.
/// </summary>
public class ConfigurationApiService : IConfigurationApiService
{
    private readonly IApiClient _apiClient;
    private const string BaseUrl = "/configuration";

    public ConfigurationApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // System

    public async Task<ApiResult<AesirConfigurationReadinessBase>> GetSystemReadinessAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<AesirConfigurationReadinessBase>($"{BaseUrl}/systemready", ct);
            return result ?? new AesirConfigurationReadinessBase { IsReady = false };
        });
    }

    public async Task<ApiResult<AesirConfigurationReadinessBase>> ReloadConfigurationAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.PostAsync<AesirConfigurationReadinessBase>($"{BaseUrl}/reload", new { }, ct);
            return result ?? new AesirConfigurationReadinessBase { IsReady = false };
        });
    }

    // General Settings

    public async Task<ApiResult<AesirGeneralSettingsBase>> GetGeneralSettingsAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<AesirGeneralSettingsBase>($"{BaseUrl}/generalsettings", ct);
            return result ?? new AesirGeneralSettingsBase();
        });
    }

    public async Task<ApiResult> UpdateGeneralSettingsAsync(AesirGeneralSettingsBase settings, CancellationToken ct = default)
    {
        return await ExecuteVoidAsync(async () =>
        {
            await _apiClient.PutAsync<object>($"{BaseUrl}/generalsettings", settings, ct);
        });
    }

    // Inference Engines

    public async Task<ApiResult<IReadOnlyList<AesirInferenceEngineBase>>> GetInferenceEnginesAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<List<AesirInferenceEngineBase>>($"{BaseUrl}/inferenceengines", ct);
            return (IReadOnlyList<AesirInferenceEngineBase>)(result ?? []);
        });
    }

    public async Task<ApiResult<AesirInferenceEngineBase>> GetInferenceEngineAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<AesirInferenceEngineBase>($"{BaseUrl}/inferenceengines/{id}", ct);
            return result ?? throw new InvalidOperationException($"Inference engine {id} not found");
        });
    }

    public async Task<ApiResult<Guid>> CreateInferenceEngineAsync(AesirInferenceEngineBase engine, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.PostAsync<Guid>($"{BaseUrl}/inferenceengines", engine, ct);
            return result;
        });
    }

    public async Task<ApiResult> UpdateInferenceEngineAsync(AesirInferenceEngineBase engine, CancellationToken ct = default)
    {
        return await ExecuteVoidAsync(async () =>
        {
            await _apiClient.PutAsync<object>($"{BaseUrl}/inferenceengines/{engine.Id}", engine, ct);
        });
    }

    public async Task<ApiResult> DeleteInferenceEngineAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteVoidAsync(async () =>
        {
            var success = await _apiClient.DeleteAsync($"{BaseUrl}/inferenceengines/{id}", ct);
            if (!success) throw new InvalidOperationException($"Failed to delete inference engine {id}");
        });
    }

    // MCP Servers

    public async Task<ApiResult<IReadOnlyList<AesirMcpServerBase>>> GetMcpServersAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<List<AesirMcpServerBase>>($"{BaseUrl}/mcpservers", ct);
            return (IReadOnlyList<AesirMcpServerBase>)(result ?? []);
        });
    }

    public async Task<ApiResult<AesirMcpServerBase>> GetMcpServerAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<AesirMcpServerBase>($"{BaseUrl}/mcpservers/{id}", ct);
            return result ?? throw new InvalidOperationException($"MCP server {id} not found");
        });
    }

    public async Task<ApiResult<Guid>> CreateMcpServerAsync(AesirMcpServerBase server, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.PostAsync<Guid>($"{BaseUrl}/mcpservers", server, ct);
            return result;
        });
    }

    public async Task<ApiResult> UpdateMcpServerAsync(AesirMcpServerBase server, CancellationToken ct = default)
    {
        return await ExecuteVoidAsync(async () =>
        {
            await _apiClient.PutAsync<object>($"{BaseUrl}/mcpservers/{server.Id}", server, ct);
        });
    }

    public async Task<ApiResult> DeleteMcpServerAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteVoidAsync(async () =>
        {
            var success = await _apiClient.DeleteAsync($"{BaseUrl}/mcpservers/{id}", ct);
            if (!success) throw new InvalidOperationException($"Failed to delete MCP server {id}");
        });
    }

    public async Task<ApiResult<IReadOnlyList<AesirMcpServerToolBase>>> GetMcpServerToolsAsync(Guid serverId, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<List<AesirMcpServerToolBase>>($"{BaseUrl}/mcpservers/{serverId}/tools", ct);
            return (IReadOnlyList<AesirMcpServerToolBase>)(result ?? []);
        });
    }

    // Tools

    public async Task<ApiResult<IReadOnlyList<AesirToolBase>>> GetToolsAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<List<AesirToolBase>>($"{BaseUrl}/tools", ct);
            return (IReadOnlyList<AesirToolBase>)(result ?? []);
        });
    }

    public async Task<ApiResult<AesirToolBase>> GetToolAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<AesirToolBase>($"{BaseUrl}/tools/{id}", ct);
            return result ?? throw new InvalidOperationException($"Tool {id} not found");
        });
    }

    public async Task<ApiResult<Guid>> CreateToolAsync(AesirToolBase tool, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.PostAsync<Guid>($"{BaseUrl}/tools", tool, ct);
            return result;
        });
    }

    public async Task<ApiResult> UpdateToolAsync(AesirToolBase tool, CancellationToken ct = default)
    {
        return await ExecuteVoidAsync(async () =>
        {
            await _apiClient.PutAsync<object>($"{BaseUrl}/tools/{tool.Id}", tool, ct);
        });
    }

    public async Task<ApiResult> DeleteToolAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteVoidAsync(async () =>
        {
            var success = await _apiClient.DeleteAsync($"{BaseUrl}/tools/{id}", ct);
            if (!success) throw new InvalidOperationException($"Failed to delete tool {id}");
        });
    }

    // Agents

    public async Task<ApiResult<IReadOnlyList<AesirAgentBase>>> GetAgentsAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<List<AesirAgentBase>>($"{BaseUrl}/agents", ct);
            return (IReadOnlyList<AesirAgentBase>)(result ?? []);
        });
    }

    public async Task<ApiResult<AesirAgentBase>> GetAgentAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<AesirAgentBase>($"{BaseUrl}/agents/{id}", ct);
            return result ?? throw new InvalidOperationException($"Agent {id} not found");
        });
    }

    public async Task<ApiResult<Guid>> CreateAgentAsync(AesirAgentBase agent, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.PostAsync<Guid>($"{BaseUrl}/agents", agent, ct);
            return result;
        });
    }

    public async Task<ApiResult> UpdateAgentAsync(AesirAgentBase agent, CancellationToken ct = default)
    {
        return await ExecuteVoidAsync(async () =>
        {
            await _apiClient.PutAsync<object>($"{BaseUrl}/agents/{agent.Id}", agent, ct);
        });
    }

    public async Task<ApiResult> DeleteAgentAsync(Guid id, CancellationToken ct = default)
    {
        return await ExecuteVoidAsync(async () =>
        {
            var success = await _apiClient.DeleteAsync($"{BaseUrl}/agents/{id}", ct);
            if (!success) throw new InvalidOperationException($"Failed to delete agent {id}");
        });
    }

    public async Task<ApiResult<IReadOnlyList<AesirToolBase>>> GetAgentToolsAsync(Guid agentId, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _apiClient.GetAsync<List<AesirToolBase>>($"{BaseUrl}/agents/{agentId}/tools", ct);
            return (IReadOnlyList<AesirToolBase>)(result ?? []);
        });
    }

    public async Task<ApiResult> UpdateAgentToolsAsync(Guid agentId, IEnumerable<Guid> toolIds, CancellationToken ct = default)
    {
        return await ExecuteVoidAsync(async () =>
        {
            await _apiClient.PutAsync<object>($"{BaseUrl}/agents/{agentId}/tools", toolIds.ToArray(), ct);
        });
    }

    // Helper methods

    private static async Task<ApiResult<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            var result = await action();
            return ApiResult<T>.Success(result);
        }
        catch (HttpRequestException ex)
        {
            var statusCode = ex.StatusCode ?? HttpStatusCode.InternalServerError;
            return ApiResult<T>.Failure(ex.Message, statusCode);
        }
        catch (Exception ex)
        {
            return ApiResult<T>.FromException(ex);
        }
    }

    private static async Task<ApiResult> ExecuteVoidAsync(Func<Task> action)
    {
        try
        {
            await action();
            return ApiResult.Success();
        }
        catch (HttpRequestException ex)
        {
            var statusCode = ex.StatusCode ?? HttpStatusCode.InternalServerError;
            return ApiResult.Failure(ex.Message, statusCode);
        }
        catch (Exception ex)
        {
            return ApiResult.FromException(ex);
        }
    }
}
