using System;
using System.IO;
using Aesir.Client.Desktop.Services;
using Aesir.Client.Services;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;

namespace Aesir.Client.Desktop;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // Determine the environment name
            var environmentName = args.Length > 0 ? args[0] : null;
            environmentName ??= Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        
            // Load app settings from a JSON file
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            // Load environment-specific app settings if environment name is provided
            if (!string.IsNullOrWhiteSpace(environmentName))
            {
                builder.AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);
            }

            builder.AddEnvironmentVariables();

            var configuration = builder.Build();
        
            // Add the configuration to the service collection of the app
            App.AddService(services =>
                {
                    services.AddSingleton<IConfiguration>(configuration)
                        .AddLogging(loggingBuilder =>
                        {
                            // configure Logging with NLog
                            loggingBuilder.ClearProviders();
                            loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Error);
                            loggingBuilder.AddNLog(configuration);
                        });

                    services.AddTransient<IPdfViewerService, PdfViewerService>();

                    services.AddSingleton<IAudioPlaybackService, AudioPlaybackService>();
                    services.AddSingleton<IAudioRecordingService, AudioRecordingService>();
                    services.AddSingleton<ISpeechService, SpeechService>();
                }
            );

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            LogManager.GetCurrentClassLogger().Fatal(ex, "Fatal error occurred during application startup");
            
            if(Application.Current != null)
                ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime!)
                    .TryShutdown(1);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont();
}