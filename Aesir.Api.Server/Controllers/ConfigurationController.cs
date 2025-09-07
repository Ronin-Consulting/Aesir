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
    IConfigurationService configurationService,
    IMcpServerService mcpServerService) : ControllerBase
{
    [HttpGet("generalsettings")]
    public async Task<IActionResult> GetGeneralSettingsAsync()
    {
        try
        {
            var generalSettings = await configurationService.GetGeneralSettingsAsync();

            logger.LogDebug("General Settings = {GeneralSettings}", JsonSerializer.Serialize(generalSettings));

            return Ok(generalSettings);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving general settings");
            return StatusCode(500, "An error occurred while retrieving general settings");
        }
    }

    [HttpPut("generalsettings")]
    public async Task<IActionResult> UpdateGeneralSettingsAsync([FromBody] AesirGeneralSettings generalSettings)
    {
        try
        {
            await configurationService.UpdateGeneralSettingsAsync(generalSettings);

            logger.LogDebug("Updated general settings = {GeneralSettings}", JsonSerializer.Serialize(generalSettings));

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating general settings");
            return StatusCode(500, "An error occurred while updating the general settings");
        }
    }
    
    [HttpGet("inferenceengines")]
    public async Task<IActionResult> GetInferenceEnginesAsync()
    {
        try
        {
            var results = (await configurationService.GetInferenceEnginesAsync()).ToList();

            logger.LogDebug("Found {Count} inference engines", results.Count);
            logger.LogDebug("Results = {Results}", JsonSerializer.Serialize(results));

            return Ok(results);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving inference engines");
            return StatusCode(500, "An error occurred while retrieving inference engines");
        }
    }
    
    [HttpGet("inferenceengines/{id:guid}")]
    public async Task<IActionResult> GetInferenceEngineAsync([FromRoute] Guid id)
    {
        try
        {
            var inferenceEngine = await configurationService.GetInferenceEngineAsync(id);

            logger.LogDebug("Inference Engine = {InferenceEngine}", JsonSerializer.Serialize(inferenceEngine));

            return Ok(inferenceEngine);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving inference engine with ID = {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the inference engine");
        }
    }
    
    [HttpPost("inferenceengines")]
    public async Task<IActionResult> CreateInferenceEngineAsync([FromBody] AesirInferenceEngine inferenceEngine)
    {
        try
        {
            await configurationService.CreateInferenceEngineAsync(inferenceEngine);

            logger.LogDebug("Created inference engine = {InferenceEngine}", JsonSerializer.Serialize(inferenceEngine));

            return Created("", new { id = inferenceEngine.Id });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating inference engine");
            return StatusCode(500, "An error occurred while creating the inference engine");
        }
    }

    [HttpPut("inferenceengines/{id:guid}")]
    public async Task<IActionResult> UpdateInferenceEngineAsync([FromRoute] Guid id, [FromBody] AesirInferenceEngine inferenceEngine)
    {
        if (id != inferenceEngine.Id)
        {
            return BadRequest("The ID in the URL does not match the ID in the request body.");
        }

        try
        {
            await configurationService.UpdateInferenceEngineAsync(inferenceEngine);

            logger.LogDebug("Updated inference engine = {InferenceEngine}", JsonSerializer.Serialize(inferenceEngine));

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating inference engine with ID = {Id}", id);
            return StatusCode(500, "An error occurred while updating the inference engine");
        }
    }

    [HttpDelete("inferenceengines/{id:guid}")]
    public async Task<IActionResult> DeleteInferenceEngineAsync([FromRoute] Guid id)
    {
        try
        {
            await configurationService.DeleteInferenceEngineAsync(id);

            logger.LogDebug("Deleted inference engine with ID = {Id}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting inference engine with ID = {Id}", id);
            return StatusCode(500, "An error occurred while deleting the inference engine");
        }
    }
    
    [HttpGet("agents")]
    public async Task<IActionResult> GetAgentsAsync()
    {
        try
        {
            var results = (await configurationService.GetAgentsAsync()).ToList();

            logger.LogDebug("Found {Count} agents", results.Count);
            logger.LogDebug("Results = {Results}", JsonSerializer.Serialize(results));

            return Ok(results);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving agents");
            return StatusCode(500, "An error occurred while retrieving agents");
        }
    }
    
    [HttpGet("agents/{id:guid}")]
    public async Task<IActionResult> GetAgentAsync([FromRoute] Guid id)
    {
        try
        {
            var agent = await configurationService.GetAgentAsync(id);

            logger.LogDebug("Agent = {Agent}", JsonSerializer.Serialize(agent));

            return Ok(agent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving agent with ID = {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the agent");
        }
    }
    
    [HttpPost("agents")]
    public async Task<IActionResult> CreateAgentAsync([FromBody] AesirAgent agent)
    {
        try
        {
            await configurationService.CreateAgentAsync(agent);

            logger.LogDebug("Created agent = {Agent}", JsonSerializer.Serialize(agent));

            return Created("", new { id = agent.Id });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating agent");
            return StatusCode(500, "An error occurred while creating the agent");
        }
    }

    [HttpPut("agents/{id:guid}")]
    public async Task<IActionResult> UpdateAgentAsync([FromRoute] Guid id, [FromBody] AesirAgent agent)
    {
        if (id != agent.Id)
        {
            return BadRequest("The ID in the URL does not match the ID in the request body.");
        }

        try
        {
            await configurationService.UpdateAgentAsync(agent);

            logger.LogDebug("Updated agent = {Agent}", JsonSerializer.Serialize(agent));

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating agent with ID = {Id}", id);
            return StatusCode(500, "An error occurred while updating the agent");
        }
    }

    [HttpDelete("agents/{id:guid}")]
    public async Task<IActionResult> DeleteAgentAsync([FromRoute] Guid id)
    {
        try
        {
            await configurationService.DeleteAgentAsync(id);

            logger.LogDebug("Deleted agent with ID = {Id}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting agent with ID = {Id}", id);
            return StatusCode(500, "An error occurred while deleting the agent");
        }
    }
    
    [HttpGet("tools")]
    public async Task<IActionResult> GetToolsAsync()
    {
        try
        {
            var results = (await configurationService.GetToolsAsync()).ToList();

            logger.LogDebug("Found {Count} tools", results.Count);
            logger.LogDebug("Results = {Results}", JsonSerializer.Serialize(results));

            return Ok(results);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving tools");
            return StatusCode(500, "An error occurred while retrieving tools");
        }
    }
    
    [HttpGet("tools/{id:guid}")]
    public async Task<IActionResult> GetToolAsync([FromRoute] Guid id)
    {
        try
        {
            var tool = await configurationService.GetToolAsync(id);

            logger.LogDebug("Tool = {Tool}", JsonSerializer.Serialize(tool));

            return Ok(tool);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving tool with ID = {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the tool");
        }
    }
    
    [HttpGet("agents/{id:guid}/tools")]
    public async Task<IActionResult> GetToolsForAgentAsync([FromRoute] Guid id)
    {
        try
        {
            var results = (await configurationService.GetToolsUsedByAgentAsync(id)).ToList();

            logger.LogDebug("Found {Count} tools", results.Count);
            logger.LogDebug("Results = {Results}", JsonSerializer.Serialize(results));

            return Ok(results);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving tools for agent with ID = {Id}", id);
            return StatusCode(500, "An error occurred while retrieving tools for the agent");
        }
    }
    
    [HttpPost("tools")]
    public async Task<IActionResult> CreateToolAsync([FromBody] AesirTool tool)
    {
        try
        {
            await configurationService.CreateToolAsync(tool);

            logger.LogDebug("Created tool = {Tool}", JsonSerializer.Serialize(tool));

            return Created("", new { id = tool.Id });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating tool");
            return StatusCode(500, "An error occurred while creating the tool");
        }
    }

    [HttpPut("tools/{id:guid}")]
    public async Task<IActionResult> UpdateToolAsync([FromRoute] Guid id, [FromBody] AesirTool tool)
    {
        if (id != tool.Id)
        {
            return BadRequest("The ID in the URL does not match the ID in the request body.");
        }

        try
        {
            await configurationService.UpdateToolAsync(tool);

            logger.LogDebug("Updated tool = {Tool}", JsonSerializer.Serialize(tool));

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating tool with ID = {Id}", id);
            return StatusCode(500, "An error occurred while updating the tool");
        }
    }

    [HttpDelete("tools/{id:guid}")]
    public async Task<IActionResult> DeleteToolAsync([FromRoute] Guid id)
    {
        try
        {
            await configurationService.DeleteToolAsync(id);

            logger.LogDebug("Deleted tool with ID = {Id}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting tool with ID = {Id}", id);
            return StatusCode(500, "An error occurred while deleting the tool");
        }
    }
    
    [HttpGet("mcpservers")]
    public async Task<IActionResult> GetMcpServersAsync()
    {
        try
        {
            var results = (await configurationService.GetMcpServersAsync()).ToList();

            logger.LogDebug("Found {Count} MCP Servers", results.Count);
            logger.LogDebug("Results = {Results}", JsonSerializer.Serialize(results));

            return Ok(results);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving MCP servers");
            return StatusCode(500, "An error occurred while retrieving MCP servers");
        }
    }
    
    [HttpGet("mcpservers/{id:guid}")]
    public async Task<IActionResult> GetMcpServerAsync([FromRoute] Guid id)
    {
        try
        {
            var mcpServer = await configurationService.GetMcpServerAsync(id);

            logger.LogDebug("MCP Server = {McpServer}", JsonSerializer.Serialize(mcpServer));

            return Ok(mcpServer);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving MCP server with ID = {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the MCP server");
        }
    }
    
    [HttpPost("mcpservers")]
    public async Task<IActionResult> CreateMcpServerAsync([FromBody] AesirMcpServer mcpServer)
    {
        try
        {
            await configurationService.CreateMcpServerAsync(mcpServer);

            logger.LogDebug("Created MCP Server = {McpServer}", JsonSerializer.Serialize(mcpServer));

            return Created("", new { id = mcpServer.Id });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating MCP server");
            return StatusCode(500, "An error occurred while creating the MCP server");
        }
    }

    [HttpPut("mcpservers/{id:guid}")]
    public async Task<IActionResult> UpdateMcpServerAsync([FromRoute] Guid id, [FromBody] AesirMcpServer mcpServer)
    {
        if (id != mcpServer.Id)
        {
            return BadRequest("The ID in the URL does not match the ID in the request body.");
        }

        try
        {
            await configurationService.UpdateMcpServerAsync(mcpServer);

            logger.LogDebug("Updated MCP Server = {McpServer}", JsonSerializer.Serialize(mcpServer));

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating MCP server with ID = {Id}", id);
            return StatusCode(500, "An error occurred while updating the MCP server");
        }
    }

    [HttpDelete("mcpservers/{id:guid}")]
    public async Task<IActionResult> DeleteMcpServerAsync([FromRoute] Guid id)
    {
        try
        {
            await configurationService.DeleteMcpServerAsync(id);

            logger.LogDebug("Deleted MCP Server with ID = {Id}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting MCP Server with ID = {Id}", id);
            return StatusCode(500, "An error occurred while deleting the MCP Server");
        }
    }
    
    [HttpPost("mcpservers/from-config")]
    public async Task<IActionResult> CreateMcpServerFromConfigAsync([FromBody] string clientConfigurationJson)
    {
        try
        {
            var mcpServer = mcpServerService.ParseMcpServerFromClientConfiguration(clientConfigurationJson);

            logger.LogDebug("Created unsaved MCP Server from client configuration = {McpServer}", JsonSerializer.Serialize(mcpServer));

            return await Task.FromResult(Created("", mcpServer));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating MCP server from client configuration");
            return StatusCode(500, "An error occurred while creating the MCP server from configuration");
        }
    }
    
    [HttpGet("mcpservers/{id:guid}/tools")]
    public async Task<IActionResult> GetToolsForMcpServerAsync([FromRoute] Guid id)
    {
        try
        {
            var mcpServer = await configurationService.GetMcpServerAsync(id);
            var results = (await mcpServerService.GetMcpServerToolsAsync(mcpServer)).ToList();

            logger.LogDebug("Found {Count} tools", results.Count);
            logger.LogDebug("Results = {Results}", JsonSerializer.Serialize(results));

            return Ok(results);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving MCP Server tools in MCP Server with ID = {Id}", id);
            return StatusCode(500, "An error occurred while retrieving tools in the MCP Servers");
        }
    }
}