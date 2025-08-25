using System.Text.Json;
using Aesir.Api.Server.Models;
using Aesir.Common.Models;
using ModelContextProtocol.Client;
using Newtonsoft.Json.Linq;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Provides services for managing and interacting with MCP servers.
/// </summary>
public class McpServerService(ILogger<McpServerService> logger) : IMcpServerService
{
    /// Parses the MCP server client configuration from a given JSON string representing the client configuration.
    /// The method attempts to extract and parse MCP server information from the provided JSON, handling variations
    /// in section names and potential parsing errors. If exactly one server is found in the configuration,
    /// it is returned as an instance of AesirMcpServer. If no servers or more than one server is found, or if the JSON
    /// is invalid, an exception is thrown.
    /// <param name="clientConfigurationJson">
    /// A JSON string containing the client configuration. The format is expected to have an "mcpServers"
    /// or "servers" section with details of the MCP servers.
    /// </param>
    /// <returns>
    /// An AesirMcpServer instance representing the parsed MCP server configuration if exactly one server is found.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the JSON is invalid, or if the number of servers parsed from the configuration is not exactly one.
    /// </exception>
    public AesirMcpServer ParseMcpServerFromClientConfiguration(string clientConfigurationJson)
    {
        try
        {
            var jsonObject = JObject.Parse(clientConfigurationJson);
            
            // look for the servers section using the standard name
            var mcpServersSection = jsonObject["mcpServers"] as JObject;
            if (mcpServersSection == null)
            {
                // try MS SK name for the servers section
                mcpServersSection = jsonObject["servers"] as JObject;
            }
        
            var mcpServers = new List<AesirMcpServer>();
        
            foreach (var serverProperty in mcpServersSection.Properties())
            {
                var serverName = serverProperty.Name;
                var serverConfig = serverProperty.Value as JObject;
            
                if (serverConfig != null)
                {
                    var mcpServer = new AesirMcpServer
                    {
                        Name = serverName
                    };

                    if (!string.IsNullOrWhiteSpace(serverConfig["command"]?.Value<string>()))
                    {
                        // stdio
                        mcpServer.Location = ServerLocation.Local;
                        mcpServer.Command = serverConfig["command"]?.Value<string>();
                        mcpServer.Arguments = serverConfig["args"]?.ToObject<List<string>>() ?? new List<string>();
                        mcpServer.EnvironmentVariables = serverConfig["env"]?.ToObject<Dictionary<string, string?>>() ??
                                                         new Dictionary<string, string?>();
                
                        mcpServers.Add(mcpServer);
                    }
                    else if (!string.IsNullOrWhiteSpace(serverConfig["uri"]?.Value<string>()))
                    {
                        // sse or websocket
                        mcpServer.Location = ServerLocation.Remote;
                        mcpServer.Url = serverConfig["uri"]?.Value<string>();
                        mcpServer.HttpHeaders = new Dictionary<string, string?>();

                        // consider specifying the type, for now we are using autodetect in transport, if that's
                        // not always good enough we may need to read the type and store
                        //serverConfig["type"]?.Value<string>();
                        
                        mcpServers.Add(mcpServer);
                    }
                }
            }

            if (mcpServers.Count == 1)
                return mcpServers[0];

            throw new ArgumentException($"Unexpected MCP Server count in client configuration: {mcpServers.Count}");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse MCP servers from client configuration JSON: {Json}", clientConfigurationJson);
            throw new ArgumentException("Invalid JSON format in client configuration", nameof(clientConfigurationJson), ex);
        }
    }

    /// <summary>
    /// Retrieves a collection of tools from a running MCP server based on the specified MCP server configuration.
    /// </summary>
    /// <param name="mcpServer">The MCP server configuration specifying the location and parameters
    /// to access it.
    /// </param>
    /// <returns>A task representing the asynchronous operation. The task result contains an enumerable collection of MCP server tools associated with the given configuration.</returns>
    public async Task<IEnumerable<AesirMcpServerTool>> GetMcpServerToolsAsync(AesirMcpServer mcpServer)
    {
        switch (mcpServer.Location)
        {
            case ServerLocation.Local:
            {
                // Create the MCP client
                var transport = new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = mcpServer.Name,
                    Command = mcpServer.Command,
                    Arguments = mcpServer.Arguments,
                    EnvironmentVariables = mcpServer.EnvironmentVariables
                });
                await using var mcp = await McpClientFactory.CreateAsync(transport);

                return await AddTools(mcpServer, mcp);
            }
            case ServerLocation.Remote:
            {
                var endpoint = new Uri(mcpServer.Url);

                // Create the MCP client
                var transport = new SseClientTransport(new SseClientTransportOptions
                {
                    Endpoint = endpoint,
                    TransportMode = HttpTransportMode.AutoDetect
                });
                await using var mcp = await McpClientFactory.CreateAsync(transport);

                return await AddTools(mcpServer, mcp);
            }
            default:
                throw new ArgumentException("Unexpected server location");
        }
    }

    /// <summary>
    /// Retrieves and processes the tool list for a given MCP server using the provided IMcpClient instance.
    /// </summary>
    /// <param name="mcpServer">The metadata of the target MCP server.</param>
    /// <param name="mcp">An instance of IMcpClient used to interact with the MCP server.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of tools available on the MCP server.</returns>
    private async Task<IEnumerable<AesirMcpServerTool>> AddTools(AesirMcpServerBase mcpServer, IMcpClient mcp)
    {
        var mcpServerTools = new List<AesirMcpServerTool>();
        
        var toolsResults = await mcp.ListToolsAsync();
        logger.LogDebug($"== Tools for {mcpServer.Command} ==");
        foreach (var tool in toolsResults)
        {
            var schema = GetJsonAsString(tool.JsonSchema);

            logger.LogDebug($"- {tool.Name} : {tool.Description}\n");
            logger.LogDebug(schema);

            mcpServerTools.Add(new AesirMcpServerTool()
            {
                Name = tool.Name,
                Description = tool.Description,
                Schema = schema
            });
        }

        return mcpServerTools;
    }

    /// Converts a JSON element into its string representation with formatting options.
    /// <param name="schemaElement">
    /// The JSON element to be converted to a string format.
    /// </param>
    /// <returns>
    /// The JSON element represented as a formatted string.
    /// </returns>
    private string GetJsonAsString(JsonElement schemaElement)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true // pretty print
        };
        return JsonSerializer.Serialize(schemaElement, options);
    }
}