using Aesir.Infrastructure.Modules;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Chat.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Chat;

/// <summary>
/// Chat module providing chat history management and chat completion endpoints.
/// </summary>
public class ChatModule : ModuleBase
{
    public ChatModule(ILogger logger) : base(logger)
    {
    }

    public override string Name => "Chat";

    public override string Version => "1.0.0";

    public override string? Description => "Provides chat history management and chat completion API endpoints";

    public override Task RegisterServicesAsync(IServiceCollection services)
    {
        Log("Registering chat services...");

        // Register chat history service
        services.AddSingleton<IChatHistoryService, ChatHistoryService>();

        Log("Chat services registered successfully");
        
        return Task.CompletedTask;
    }
}
