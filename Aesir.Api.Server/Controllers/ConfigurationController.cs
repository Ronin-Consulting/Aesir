using System.Text.Json;
using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Api.Server.Controllers;

/// <summary>
/// The ConfigurationController class provides endpoints for managing agents, tools, and MCP servers
/// within the system. It supports CRUD operations for these entities and additional functionalities
/// such as retrieving default personas or associating tools with specific agents or MCP servers.
/// </summary>
/// <remarks>
/// This controller handles requests related to configurations and organizational resources.
/// </remarks>
/// <remarks>
/// Endpoints:
/// - Agents: Manage agents with CRUD operations.
/// - Tools: Manage tools with CRUD operations and associate tools with agents or MCP servers.
/// - MCP Servers: Perform CRUD operations for MCP servers and create instances from a configuration.
/// - Default Persona: Retrieve the default persona configuration.
/// </remarks>
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
    
    /// <summary>
    /// Retrieves a list of agents asynchronously using the configuration service.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> containing the list of agents if successful,
    /// or a status code with an error message in case of failure.
    /// </returns>
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

    /// <summary>
    /// Retrieves an agent by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the agent to retrieve.</param>
    /// <returns>
    /// An <see cref="IActionResult"/> containing the agent details if found, or an appropriate error message and status code.
    /// </returns>
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

    /// <summary>
    /// Asynchronously creates a new agent.
    /// </summary>
    /// <param name="agent">The agent object to create, provided in the request body.</param>
    /// <returns>
    /// An <see cref="IActionResult"/> indicating the result of the operation:
    /// - Returns a Created response with the agent's ID if the operation succeeds.
    /// - Returns a StatusCode 500 response if an error occurs during the creation process.
    /// </returns>
    /// <remarks>
    /// Logs the created agent's details at debug level upon successful execution.
    /// Logs any encountered exceptions as errors.
    /// Uses the <c>IConfigurationService</c> implementation to handle agent creation logic.
    /// </remarks>
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

    /// <summary>
    /// Updates an existing agent in the configuration. The agent to update is identified by the provided ID in the route.
    /// </summary>
    /// <param name="id">The unique identifier of the agent to update.</param>
    /// <param name="agent">The updated agent data.</param>
    /// <returns>
    /// Returns <c>NoContent</c> if the update operation succeeds.
    /// Returns <c>BadRequest</c> if the ID in the route does not match the ID in the provided agent data.
    /// Returns a <c>StatusCode(500)</c> if an internal server error occurs during the update process.
    /// </returns>
    /// <remarks>
    /// This method performs validation to ensure the ID in the route matches the ID in the body of the provided agent.
    /// Logs any errors or updates that occur during the operation.
    /// </remarks>
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

    /// <summary>
    /// Deletes an agent based on the specified unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the agent to be deleted.</param>
    /// <returns>A task representing the asynchronous operation, with no content returned upon successful deletion.</returns>
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

    /// <summary>
    /// Asynchronously retrieves a list of tools available in the configuration service.
    /// </summary>
    /// <returns>A task representing the asynchronous operation. The task result contains an IActionResult with
    /// the list of tools if successful, or an appropriate error response if an exception occurs.</returns>
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

    /// Retrieves a specific tool based on its unique identifier.
    /// <param name="id">The unique identifier (GUID) of the tool to retrieve.</param>
    /// <returns>
    /// An IActionResult containing the tool details if found, or an error response if an exception occurs or the tool is not found.
    /// </returns>
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

    /// <summary>
    /// Retrieves the list of tools associated with a specific agent.
    /// </summary>
    /// <param name="id">The unique identifier of the agent.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="IActionResult"/> representing the list of tools for the agent.</returns>
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

    /// <summary>
    /// Creates a new tool in the system and returns the result.
    /// </summary>
    /// <param name="tool">The tool object containing details about the tool to be created.</param>
    /// <returns>An <see cref="IActionResult"/> indicating the result of the operation. If successful, returns a 201 Created response with the created tool's ID. If an error occurs, returns a 500 Internal Server Error.</returns>
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

    /// <summary>
    /// Updates an existing tool with the specified ID using the provided tool data.
    /// </summary>
    /// <param name="id">The unique identifier of the tool to update.</param>
    /// <param name="tool">The updated tool data to replace the existing tool.</param>
    /// <returns>
    /// Returns a NoContent status if the tool is updated successfully.
    /// Returns a BadRequest status if the ID in the URL does not match the ID in the request body.
    /// Returns a StatusCode 500 if an internal server error occurs.
    /// </returns>
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

    /// <summary>
    /// Deletes a tool specified by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the tool to delete.</param>
    /// <returns>A response indicating the result of the delete operation. Returns NoContent if successful,
    /// or an error response if the operation fails.</returns>
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

    /// <summary>
    /// Retrieves a list of MCP (Multi-Channel Processing) servers.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> containing the list of MCP servers if the call succeeds,
    /// or a server error response if an exception occurs during the retrieval process.
    /// </returns>
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

    /// <summary>
    /// Retrieves an MCP server by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the MCP server.</param>
    /// <returns>An <see cref="IActionResult"/> containing the requested MCP server if found, or an error response if not.</returns>
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

    /// <summary>
    /// Creates a new MCP server with the specified configuration.
    /// </summary>
    /// <param name="mcpServer">The MCP server configuration to create.</param>
    /// <returns>An <see cref="IActionResult"/> indicating the result of the operation, including the created MCP server's ID if successful.</returns>
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

    /// <summary>
    /// Updates an existing MCP server identified by its unique ID.
    /// </summary>
    /// <param name="id">The unique identifier of the MCP server to be updated.</param>
    /// <param name="mcpServer">The updated details of the MCP server.</param>
    /// <returns>A task representing the asynchronous operation. Returns HTTP 204 No Content if successful. Returns HTTP 400 if the ID in the route does not match the ID in the request body. Returns HTTP 500 if an error occurs during the update process.</returns>
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

    /// <summary>
    /// Deletes an MCP (Managed Configuration Protocol) server by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier (GUID) of the MCP server to delete.</param>
    /// <returns>An <see cref="IActionResult"/> indicating the result of the operation.
    /// Returns 204 No Content on success or an appropriate error response on failure.</returns>
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

    /// <summary>
    /// Creates a new MCP server instance from the provided client configuration in JSON format.
    /// </summary>
    /// <param name="clientConfigurationJson">The JSON representation of the client configuration used to create an MCP server instance.</param>
    /// <returns>Returns an <see cref="IActionResult"/> containing the created MCP server instance or an appropriate error response if the creation process fails.</returns>
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

    /// Retrieves the list of tools associated with the specified MCP server ID.
    /// <param name="id">The unique identifier of the MCP server to retrieve tools for.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult with the list of tools associated with the specified MCP server or an error message if the operation fails.</returns>
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