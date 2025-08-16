using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Common.Models;

namespace Aesir.Client.Services.Implementations.NoOp;

public class NoOpConfigurationService : IConfigurationService
{
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

    public Task UpdateAToolAsync(AesirToolBase tool)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateToolAsync(AesirToolBase tool)
    {
        await Task.CompletedTask;
    }

    public async Task DeleteToolAsync(Guid id)
    {
        await Task.CompletedTask;
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

    public Task<PromptPersona> GetDefaultPersonaAsync()
    {
        return Task.FromResult(PromptPersona.Business);
    }
}