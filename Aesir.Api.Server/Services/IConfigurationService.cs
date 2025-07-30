using Aesir.Api.Server.Models;

namespace Aesir.Api.Server.Services;

public interface IConfigurationService
{
    Task<IEnumerable<AesirAgent>> GetAgentsAsync();
    
    Task<AesirAgent> GetAgentAsync(Guid id);
    
    Task<IEnumerable<AesirTool>> GetToolsAsync();
    
    Task<IEnumerable<AesirTool>> GetToolsUsedByAgentAsync(Guid agentId);
    
    Task<AesirTool> GetToolAsync(Guid id);
}