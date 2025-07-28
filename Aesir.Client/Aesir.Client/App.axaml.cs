using System;
using System.Net.Http;
using Aesir.Client.Controls;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Aesir.Client.Services.Implementations.MarkdigMarkdown;
using Aesir.Client.Services.Implementations.NoOp;
using Aesir.Client.Services.Implementations.Standard;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Aesir.Client.ViewModels;
using Aesir.Client.Views;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace Aesir.Client;

public partial class App : Application
{
    private static bool _iocConfigured;
    private static readonly ServiceCollection AppServices = [];
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Skip the normal host/DI pipeline when the previewer is running
        if (Design.IsDesignMode)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime designDesktop)
            {
                var designServiceProvider = ConfigureDesignServices(this);
                
                var designModel = designServiceProvider.GetService<MainWindowViewModel>();
                designDesktop.MainWindow = new MainWindow().WithViewModel(designModel);
            }
        
            base.OnFrameworkInitializationCompleted();
            return;
        }

        var serviceProvider = ConfigureServices(this);
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splashModel = serviceProvider.GetService<SplashViewModel>();
            desktop.MainWindow = new AesirSplashWindow().WithViewModel(splashModel);
            
            desktop.Exit += OnExit;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            var mainWindowViewModel = serviceProvider.GetService<MainWindowViewModel>();
            singleViewPlatform.MainView = new MainWindow().WithViewModel(mainWindowViewModel!);
        }
        
        base.OnFrameworkInitializationCompleted();
    }
    
    public static void AddService(Action<ServiceCollection> configureServices)
    {
        if(_iocConfigured)
        {
            throw new InvalidOperationException("Cannot add services after IoC has been configured");
        }
        
        configureServices(AppServices);
    }
    
    private static ServiceProvider ConfigureServices(Application? application)
    {
        // setup shared services
        AppServices.AddSingleton<ApplicationState>(p =>
        {
            var appState = new ApplicationState(
                p.GetRequiredService<IModelService>(),
                p.GetRequiredService<IChatHistoryService>()
            )
            {
                IsActive = true
            };

            return appState;
        });
        AppServices.AddSingleton<MainWindowViewModel>();
        AppServices.AddSingleton<MainViewViewModel>();
        AppServices.AddSingleton<ChatHistoryViewModel>();
        AppServices.TryAddSingleton<ISpeechService,NoOpSpeechService>();
        AppServices.AddSingleton<IMarkdownService,MarkdigMarkdownService>();
        AppServices.AddSingleton<IChatService,ChatService>();
        AppServices.AddSingleton<IChatHistoryService, ChatHistoryService>();
        AppServices.AddSingleton<IModelService, ModelService>();
        AppServices.AddSingleton<IDocumentCollectionService, DocumentCollectionService>();
        AppServices.AddSingleton<IChatSessionManager, ChatSessionManager>();
        AppServices.AddSingleton<IContentProcessingService, ContentProcessingService>();
        AppServices.AddSingleton<INavigationService, NavigationService>();
        AppServices.AddSingleton<INotificationService, NotificationService>();
        
        AppServices.AddTransient<SystemMessageViewModel>();
        AppServices.AddTransient<UserMessageViewModel>();
        AppServices.AddTransient<AssistantMessageViewModel>();
        AppServices.AddTransient<ChatHistoryButtonViewModel>();
        AppServices.AddTransient<FileToUploadViewModel>();
        AppServices.AddTransient<SplashViewModel>();
        AppServices.AddTransient<ToolsViewViewModel>();
        AppServices.AddTransient<AgentsViewViewModel>();
        
        var delay = Backoff.DecorrelatedJitterBackoffV2(
            medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 5, fastFirst: true);

        var policy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(delay);
        
        AppServices.AddSingleton<IFlurlClientCache>(_ => new FlurlClientCache()
            // all clients:
            .WithDefaults(builder =>
                builder
                    .ConfigureInnerHandler(i => 
                        i.ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                    )
                    .WithTimeout(240)
                    .AddMiddleware(() => new PolicyHttpMessageHandler(policy))
            )
        );
        
        AppServices.AddSingleton<IDialogService, DialogService>();
        
        var serviceProvider = AppServices.BuildServiceProvider();
        
        Ioc.Default.ConfigureServices(serviceProvider);

        _iocConfigured = true;
        
        return serviceProvider;
    }

    private static ServiceProvider ConfigureDesignServices(Application? application)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<ApplicationState>(p =>
            {
                var appState = new ApplicationState(
                    new NoOpModelService(),
                    new NoOpChatHistoryService()
                )
                {
                    IsActive = true
                };

                return appState;
            })
            .AddSingleton<ISpeechService, NoOpSpeechService>()
            .AddSingleton<IChatSessionManager, NoOpChatSessionManager>()
            .AddSingleton<INavigationService, NoOpNavigationService>()
            .AddSingleton<INotificationService, NoOpNotificationService>()
            .AddLogging()
            .AddSingleton<FileToUploadViewModel>()
            // …add ONLY what the open XAML needs
            .BuildServiceProvider();

        Ioc.Default.ConfigureServices(serviceProvider);
        
        return serviceProvider;
    }
    
    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        var audioPlaybackService = Ioc.Default.GetService<IAudioPlaybackService>();
        audioPlaybackService?.Dispose();
        
        var audioRecordingService = Ioc.Default.GetService<IAudioRecordingService>();
        audioRecordingService?.Dispose();
    }

}