using Aesir.Common.Models;

namespace Aesir.Client.Messages;

/// <summary>
/// Represents a message used to show the details of an MCP Server in the system.
/// </summary>
public class ShowMcpServerDetailMessage(AesirMcpServerBase? mcpServer)
{
    /// <summary>
    /// Represents an MCP Server entity, encapsulated within the <see cref="ShowMcpServerDetailMessage"/> class.
    /// This property holds an instance of <see cref="AesirMcpServerBase"/> which provides core MCP Server information such as
    /// ID, name, type, and description
    /// </summary>
    public AesirMcpServerBase McpServer { get; set; } = mcpServer ?? new AesirMcpServerBase();
}