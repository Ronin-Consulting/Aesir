using Aesir.Client.Models;
using Aesir.Common.Models;

namespace Aesir.Client.Messages;

/// <summary>
/// Represents a message used to show the details of an MCP Server in the system.
/// </summary>
public class ShowDocumentDetailMessage(AesirDocument? document)
{
    /// <summary>
    /// Represents an MCP Server entity, encapsulated within the <see cref="ShowMcpServerDetailMessage"/> class.
    /// This property holds an instance of <see cref="AesirMcpServerBase"/> which provides core MCP Server information such as
    /// ID, name, type, and description. Should be null if new or if import was requested.
    /// </summary>
    public AesirDocument Document  { get; set; } = document ?? new AesirDocument();
}