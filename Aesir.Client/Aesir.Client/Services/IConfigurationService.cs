using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Common.Models;

namespace Aesir.Client.Services;

public interface IConfigurationService
{
    Task<IEnumerable<AesirAgentBase>> GetAgentsAsync();
    
    Task<AesirAgentBase> GetAgentAsync(Guid id);

    Task<IEnumerable<AesirToolBase>> GetToolsAsync();

    Task<AesirToolBase> GetToolAsync(Guid id);

    Task<IEnumerable<AesirToolBase>> GetToolsForAgentAsync(Guid agentId);
}