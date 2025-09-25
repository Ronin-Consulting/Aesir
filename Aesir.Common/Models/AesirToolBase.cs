using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

public class AesirToolBase
{
    /// <summary>
    /// Gets or sets the id of the tool
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the tool
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    /// <summary>
    /// Gets or sets the type of the tool
    /// </summary>
    [JsonPropertyName("type")]
    public ToolType? Type { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the id of the MCP Server if this is an MCP Server ToolType
    /// </summary>
    [JsonPropertyName("mcp_server_id")]
    public Guid? McpServerId { get; set; }


    /// <summary>
    /// Gets or sets the name of the tool.
    /// </summary>
    [JsonPropertyName("tool_name")]
    public string? ToolName { get; set; }
}

public enum ToolType
{
    [Description("Internal")]
    Internal,
    [Description("MCP Server")]
    McpServer
}

/// <summary>
/// Normalized names for Aesir built-in tools. The actual tool name will map to a plugin/functions.
/// </summary>
public static class AesirTools
{
    public const string WebToolName = "WebTool";
    public const string RagToolName = "RagTool";
}