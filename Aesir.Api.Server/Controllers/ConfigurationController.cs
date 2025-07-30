using System.Text.Json;
using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Api.Server.Controllers;

[ApiController]
[Route("configuration")]
[Produces("application/json")]
public class ConfigurationController(
    ILogger<ChatHistoryController> logger,
    IConfigurationService configurationService) : ControllerBase
{
    [HttpGet("agents")]
    public async Task<IEnumerable<AesirAgent>> GetAgentsAsync()
    {
        var results = (await configurationService.GetAgentsAsync()).ToList();

        logger.LogDebug("Found {Count} agents", results.Count);
        logger.LogDebug("Results = {Results}", JsonSerializer.Serialize(results));

        return results;
    }
    
    [HttpGet("agents/{id:guid}")]
    public async Task<AesirAgent> GetAgentAsync([FromRoute] Guid id)
    {
        var agent = await configurationService.GetAgentAsync(id);

        logger.LogDebug("Agent = {Agent}", JsonSerializer.Serialize(agent));

        return agent;
    }
    
    [HttpGet("tools")]
    public async Task<IEnumerable<AesirTool>> GetToolsAsync()
    {
        var results = (await configurationService.GetToolsAsync()).ToList();

        logger.LogDebug("Found {Count} tools", results.Count);
        logger.LogDebug("Results = {Results}", JsonSerializer.Serialize(results));

        return results;
    }
    
    [HttpGet("tools/{id:guid}")]
    public async Task<AesirTool> GetToolAsync([FromRoute] Guid id)
    {
        var tool = await configurationService.GetToolAsync(id);

        logger.LogDebug("Tool = {Tool}", JsonSerializer.Serialize(tool));

        return tool;
    }
    
    [HttpGet("agents/{agentId:guid}/tools")]
    public async Task<IEnumerable<AesirTool>> GetToolsForAgentAsync([FromRoute] Guid agentId)
    {
        var results = (await configurationService.GetToolsUsedByAgentAsync(agentId)).ToList();

        logger.LogDebug("Found {Count} tools", results.Count);
        logger.LogDebug("Results = {Results}", JsonSerializer.Serialize(results));

        return results;
    }
}