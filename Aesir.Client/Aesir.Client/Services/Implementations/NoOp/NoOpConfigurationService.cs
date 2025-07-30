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
}