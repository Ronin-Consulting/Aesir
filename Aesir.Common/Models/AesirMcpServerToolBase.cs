namespace Aesir.Common.Models;

public class AesirMcpServerToolBase
{
    /// <summary>
    /// Gets or sets the name of the MCP Server Tool
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the MCP Server Tool
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the schema of the MCP Server Tool
    /// </summary>
    public string? Schema { get; set; }
    
    public bool IsNull => Name == null && Description == null && Schema == null;
}