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
using CommunityToolkit.Mvvm.DependencyInjection;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        var serviceProvider = ConfigureServices(this);
        var mainViewModel = serviceProvider.GetService<MainViewViewModel>();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            //BindingPlugins.DataValidators.RemoveAt(0);

            desktop.MainWindow = new MainWindow().WithViewModel(mainViewModel!);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainWindow().WithViewModel(mainViewModel!);
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
            var appState = new ApplicationState
            {
                IsActive = true
            };

            return appState;
        });
        AppServices.AddSingleton<MainViewViewModel>();
        AppServices.AddSingleton<ChatHistoryViewModel>();
        AppServices.AddSingleton<ISpeechService,NoOpSpeechService>();
        AppServices.AddSingleton<IMarkdownService,MarkdigMarkdownService>();
        AppServices.AddSingleton<IChatService,ChatService>();
        AppServices.AddSingleton<IChatHistoryService, ChatHistoryService>();
        AppServices.AddSingleton<IModelService, ModelService>();
        AppServices.AddTransient<IDocumentCollectionService, DocumentCollectionService>();
        AppServices.AddSingleton<IFileUploadService, FileUploadService>();
        AppServices.AddSingleton<IChatSessionManager, ChatSessionManager>();
        AppServices.AddSingleton<IContentProcessingService, ContentProcessingService>();
        
        AppServices.AddTransient<SystemMessageViewModel>();
        AppServices.AddTransient<UserMessageViewModel>();
        AppServices.AddTransient<AssistantMessageViewModel>();
        AppServices.AddTransient<ChatHistoryButtonViewModel>();
        AppServices.AddTransient<FileToUploadViewModel>();
        
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
}