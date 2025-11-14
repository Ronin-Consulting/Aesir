using Aesir.Infrastructure.Modules;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Storage.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Storage;

/// <summary>
/// Storage module providing file storage and management functionality.
/// Manages file uploads, retrievals, and deletions with database-backed persistence.
/// </summary>
public class StorageModule : ModuleBase
{
    public StorageModule(ILogger logger) : base(logger)
    {
    }

    public override string Name => "Storage";

    public override string Version => "1.0.0";

    public override string? Description => "Provides file storage and management functionality with database persistence";

    public override Task RegisterServicesAsync(IServiceCollection services)
    {
        Log("Registering storage services...");

        // Register file storage service
        services.AddScoped<IFileStorageService, FileStorageService>();

        Log("Storage services registered successfully");
        
        return Task.CompletedTask;
    }
}
