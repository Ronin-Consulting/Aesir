using Aesir.Infrastructure.Modules;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Mcp;

public class McpModule : ModuleBase
{
    public McpModule(ILogger<McpModule> logger) : base(logger)
    {
    }

    public override string Name => "MCP";
    public override string Version => "1.0.0";
    public override string? Description => "Provides Model Context Protocol (MCP) server management and integration";

    public override Task RegisterServicesAsync(IServiceCollection services)
    {
        Log("Registering MCP services...");
        services.AddSingleton<IMcpServerService, McpServerService>();
        Log("MCP services registered successfully");
        
        return Task.CompletedTask;
    }
}
