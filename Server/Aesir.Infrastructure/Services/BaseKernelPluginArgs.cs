namespace Aesir.Infrastructure.Services;

/// <summary>
/// Provides common arguments for kernel plugin operations.
/// </summary>
public class BaseKernelPluginArgs : Dictionary<string, object>
{
    public BaseKernelPluginArgs()
    {
    }

    public void SetMcpTools(McpServerToolArg[] mcpTools)
    {
        this["McpTools"] = mcpTools;
    }

    public class McpServerToolArg(string mcpServerName, string toolName)
    {
        public string McpServerName { get; set; } = mcpServerName;

        public string ToolName { get; set; } = toolName;
    }
}
