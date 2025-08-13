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
}

public enum ToolType
{
    [Description("Internal")]
    Internal,
    [Description("MCP Server")]
    McpServer
}