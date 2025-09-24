using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Client.Models;
using Aesir.Common.Models;
using Aesir.Common.Prompts;

namespace Aesir.Client.Services.Implementations.NoOp;

public class NoOpConfigurationService : IConfigurationService
{
    public async Task<AesirConfigurationReadinessBase> GetIsSystemConfigurationReadyAsync()
    {
        return await Task.FromResult(new AesirConfigurationReadinessBase());
    }

    public async Task<bool> GetIsInDatabaseModeAsync()
    {
        return await Task.FromResult(false);
    }

    public async Task<AesirGeneralSettingsBase> GetGeneralSettingsAsync()
    {
        return await Task.FromResult(new AesirGeneralSettingsBase());   
    }

    public async Task UpdateGeneralSettingsAsync(AesirGeneralSettingsBase generalSettings)
    {
        await Task.CompletedTask;
    }
    
    public async Task<IEnumerable<AesirInferenceEngineBase>> GetInferenceEnginesAsync()
    {
        return await Task.FromResult(new List<AesirInferenceEngineBase>());
    }

    public async Task<AesirInferenceEngineBase> GetInferenceEngineAsync(Guid id)
    {
        return await Task.FromResult(new AesirInferenceEngineBase());
    }

    public async Task CreateInferenceEngineAsync(AesirInferenceEngineBase inferenceEngine)
    {
        await Task.CompletedTask;
    }

    public async Task UpdateInferenceEngineAsync(AesirInferenceEngineBase inferenceEngine)
    {
        await Task.CompletedTask;
    }

    public async Task DeleteInferenceEngineAsync(Guid id)
    {
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<AesirAgentBase>> GetAgentsAsync()
    {
        return await Task.FromResult(new List<AesirAgentBase>());
    }

    public async Task<AesirAgentBase> GetAgentAsync(Guid id)
    {
        return await Task.FromResult(new AesirAgentBase());
    }

    public async Task CreateAgentAsync(AesirAgentBase agent)
    {
        await Task.CompletedTask;
    }

    public async Task UpdateAgentAsync(AesirAgentBase agent)
    {
        await Task.CompletedTask;
    }

    public async Task DeleteAgentAsync(Guid id)
    {
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<AesirDocument>> GetDocumentsAsync()
    {
        return await Task.FromResult(new List<AesirDocument>());
    }

    public async Task<IEnumerable<AesirToolBase>> GetToolsAsync()
    {
        return await Task.FromResult(new List<AesirToolBase>());
    }

    public async Task<AesirToolBase> GetToolAsync(Guid id)
    {
        return await Task.FromResult(new AesirToolBase());
    }

    public async Task<IEnumerable<AesirToolBase>> GetToolsForAgentAsync(Guid agentId)
    {
        return await Task.FromResult(new List<AesirToolBase>());
    }

    public async Task CreateToolAsync(AesirToolBase tool)
    {
        await Task.CompletedTask;
    }

    public async Task UpdateToolAsync(AesirToolBase tool)
    {
        await Task.CompletedTask;
    }

    public async Task DeleteToolAsync(Guid id)
    {
        await Task.CompletedTask;
    }

    public async Task<AesirMcpServerBase> CreateMcpServerFromConfigAsync(string clientConfigurationJson)
    {
        return await Task.FromResult(new AesirMcpServerBase());
    }

    public async Task<IEnumerable<AesirMcpServerBase>> GetMcpServersAsync()
    {
        return await Task.FromResult(new List<AesirMcpServerBase>());
    }

    public async Task<AesirMcpServerBase> GetMcpServerAsync(Guid id)
    {
        return await Task.FromResult(new AesirMcpServerBase());
    }

    public async Task CreateMcpServerAsync(AesirMcpServerBase mcpServer)
    {
        await Task.CompletedTask;
    }

    public async Task UpdateMcpServerAsync(AesirMcpServerBase mcpServer)
    {
        await Task.CompletedTask;
    }

    public async Task DeleteMcpServerAsync(Guid id)
    {
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<AesirMcpServerToolBase>> GetMcpServerTools(Guid id)
    {
        return await Task.FromResult(new List<AesirMcpServerToolBase>());   
    }
}