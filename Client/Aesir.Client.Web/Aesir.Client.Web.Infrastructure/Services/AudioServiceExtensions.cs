using Microsoft.Extensions.DependencyInjection;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Extension methods for registering audio services.
/// </summary>
public static class AudioServiceExtensions
{
    /// <summary>
    /// Adds audio capture and playback services to the service collection.
    /// Services are registered as scoped (one instance per user session).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAudioServices(this IServiceCollection services)
    {
        services.AddScoped<IAudioCaptureService, AudioCaptureService>();
        services.AddScoped<IAudioPlaybackService, AudioPlaybackService>();

        return services;
    }
}
