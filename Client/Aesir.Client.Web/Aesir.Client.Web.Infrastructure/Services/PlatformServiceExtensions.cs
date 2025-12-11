using Microsoft.Extensions.DependencyInjection;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Extension methods for configuring platform-specific services.
/// </summary>
public static class PlatformServiceExtensions
{
    /// <summary>
    /// Adds platform detection and native file services to the service collection.
    /// These services provide Tauri desktop integration when available.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        // Platform detection should be singleton since it only needs to detect once
        services.AddSingleton<IPlatformDetectionService, PlatformDetectionService>();

        // Native file service can be scoped as it relies on JS interop
        services.AddScoped<INativeFileService, NativeFileService>();

        return services;
    }
}
