namespace Aesir.Common.Models;

public class ToolRequest
{
    public static readonly ToolRequest WebSearchToolRequest = new ToolRequest { Name = AesirTools.WebSearchFunctionName };
	
    public required string Name { get; set; }
    
    protected bool Equals(ToolRequest other)
    {
        return Name == other.Name;
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
        return HashCode.Combine(Name);
    }
}