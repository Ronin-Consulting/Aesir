using System.Collections.Generic;
using Aesir.Common.Models;

namespace Aesir.Client.Messages;

/// <summary>
/// Represents a message used to show the details of an MCP Server in the system.
/// </summary>
public class ShowMcpServerDetailMessage(bool import, AesirMcpServerBase? mcpServer)
{
    /// <summary>
    /// Indicates if an import should be shown first.
    /// </summary>
    public bool Import { get; set; } = import;
    
    /// <summary>
    /// Represents an MCP Server entity, encapsulated within the <see cref="ShowMcpServerDetailMessage"/> class.
    /// This property holds an instance of <see cref="AesirMcpServerBase"/> which provides core MCP Server information such as
    /// ID, name, type, and description. Should be null if new or if import was requested.
    /// </summary>
    public AesirMcpServerBase McpServer { get; set; } = mcpServer ?? new AesirMcpServerBase()
    {
        Arguments = new List<string>(), 
        EnvironmentVariables = new Dictionary<string, string?>(),
        HttpHeaders = new Dictionary<string, string?>()
    };
}