using Aesir.Client.Web.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aesir.Client.Web.Infrastructure.Http;

/// <summary>
/// Extension methods for configuring the API client.
/// </summary>
public static class ApiClientExtensions
{
    /// <summary>
    /// Adds the AESIR API client and typed services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseUrl">The base URL of the AESIR API.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAesirApiClient(
        this IServiceCollection services,
        string baseUrl)
    {
        services.AddHttpClient<IApiClient, ApiClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // Register typed API services
        services.AddScoped<IConfigurationApiService, ConfigurationApiService>();
        services.AddScoped<IChatApiService, ChatApiService>();
        services.AddScoped<IModelApiService, ModelApiService>();
        services.AddScoped<IResearchTeamApiService, ResearchTeamApiService>();

        return services;
    }

    /// <summary>
    /// Adds the AESIR API client to the service collection with configuration action.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure the HttpClient.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAesirApiClient(
        this IServiceCollection services,
        Action<HttpClient> configure)
    {
        services.AddHttpClient<IApiClient, ApiClient>(configure);

        // Register typed API services
        services.AddScoped<IConfigurationApiService, ConfigurationApiService>();
        services.AddScoped<IChatApiService, ChatApiService>();
        services.AddScoped<IModelApiService, ModelApiService>();
        services.AddScoped<IResearchTeamApiService, ResearchTeamApiService>();

        return services;
    }
}
