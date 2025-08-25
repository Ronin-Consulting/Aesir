using System.Collections;
using Aesir.Api.Server.Models;

namespace Aesir.Api.Server.Services;

public interface IMcpServerService
{
    /// <summary>
    /// Looks at a standard MCP client configuraton file (JSON) and attempts to parse the name, command,
    /// arguments, and environment variable definition. 
    /// </summary>
    /// <param name="clientConfigurationJson">the JSON file in MCP client configuration format</param>
    /// <returns>a partially filled out AesirMcpServer instance (name, command, arguments, and env vars)</returns>
    AesirMcpServer ParseMcpServerFromClientConfiguration(string clientConfigurationJson);
    
    /// <summary>
    /// Uses the AesirMcpServer to connect to the MCP Server and list available tools
    /// </summary>
    /// <param name="mcpServer">The AESIR MCP Server definition</param>
    /// <returns>A list of MCP Server tools</returns>
    Task<IEnumerable<AesirMcpServerTool>> GetMcpServerToolsAsync(AesirMcpServer mcpServer);
}