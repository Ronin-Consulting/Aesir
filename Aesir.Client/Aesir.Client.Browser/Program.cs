using System;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Aesir.Client;
using Aesir.Client.Browser.Services;
using Aesir.Client.Services;
using Avalonia.Logging;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: SupportedOSPlatform("browser")]

internal sealed partial class Program
{
    private static string _environmentName = null!;
    private static string _baseUrl = null!;
    
    // Entry point for the application.  See main.js for the browser entry point.
    private static Task Main(string[] args)
    {
        // The first argument is the base URL of the API
        _baseUrl = args.Length > 0 ? args[0] : throw new ArgumentException("Base URL is required");
        // The second argument is the environment name
        _environmentName = args.Length == 2 ? args[1] : string.Empty;

        // Load the app settings.  These are platform specific and are loaded from the server.
        LoadAppSettings();
        
        // register services that are platform specific
        App.AddService(services => 
            services.AddSingleton<ISpeechService, BrowserSpeechService>()
        );
        
        //Trace.Listeners.Add(new ConsoleTraceListener());

        return AppBuilder.Configure<App>()
            .LogToTrace(LogEventLevel.Error)
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }
    
    private static async void LoadAppSettings()
    {
        var environmentNameNormalized = 
            !string.IsNullOrWhiteSpace(_environmentName) ? $".{_environmentName}" : string.Empty;
        
        var settingsJsonString = await _baseUrl
            .AppendPathSegment($"appsettings{environmentNameNormalized}.json")
            .GetStringAsync();
        
        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(settingsJsonString));
        var configuration = 
            new ConfigurationBuilder()
                .AddJsonStream(memoryStream)
                .AddEnvironmentVariables()
                .Build();
        
        // Add the configuration to the service collection of the app
        App.AddService(services => 
            services.AddSingleton<IConfiguration>(configuration)
                .AddLogging(builder => builder
                    .SetMinimumLevel(LogLevel.Error)
                )
        );
        
        Console.WriteLine(settingsJsonString);
    }
}