using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

public class ToolRequest
{
    public static readonly ToolRequest WebSearchToolRequest = new ToolRequest { ToolName = AesirTools.WebToolName, McpServerName = null };
	
    [JsonPropertyName("tool_name")]
    public required string ToolName { get; set; }
    
    [JsonPropertyName("msp_server_name")]
    public required string? McpServerName { get; set; }
    
    public bool IsWebSearchToolRequest => ToolName == AesirTools.WebToolName;
    
    public bool IsRagToolRequest => ToolName == AesirTools.RagToolName;
    
    public bool IsMcpServerToolRequest => !string.IsNullOrWhiteSpace(McpServerName);
    
    protected bool Equals(ToolRequest other)
    {
        return ToolName == other.ToolName && McpServerName == other.McpServerName;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ToolRequest)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ToolName, McpServerName);
    }
}