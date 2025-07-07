using System;
using System.Linq;
using System.Threading.Tasks;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Irihi.Avalonia.Shared.Contracts;
using Microsoft.Extensions.Logging;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Aesir.Client.ViewModels;

public partial class SplashViewModel: ObservableRecipient, IDialogContext
{
    private readonly ILogger<SplashViewModel> _logger;
    private readonly ApplicationState _appState;
    private readonly IModelService _modelService;
    private readonly IChatSessionManager _chatSessionManager;

    [ObservableProperty] 
    private double _progress;

    [ObservableProperty] 
    private string _status = "Starting...";

    [ObservableProperty]
    private bool _isError;
    
    public SplashViewModel(
        ILogger<SplashViewModel> logger, 
        ApplicationState appState, 
        IModelService modelService,
        IChatSessionManager chatSessionManager)
    {
        _logger = logger;
        _appState = appState;
        _modelService = modelService;
        _chatSessionManager = chatSessionManager;
        
        DispatcherTimer.RunOnce(LoadApplication, TimeSpan.FromMilliseconds(20), DispatcherPriority.Default);
    }

    private async void LoadApplication()
    {
        Progress += 0;
        
        try
        {
            Status = "Determining models available...";
            for (var i = 0; i < 5; i++)
            {
                try
                {
                    Progress += 1;
                    
                    var models = await _modelService.GetModelsAsync();
                    _appState.SelectedModel = models.FirstOrDefault(m => m.IsChatModel);
                    break;
                }
                catch
                {
                    if (i == 4) throw;
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load models");

            Status = "Error connecting to model host";
            IsError = true;

            DispatcherTimer.RunOnce(ShutdownForError, TimeSpan.FromSeconds(10), DispatcherPriority.Default);
            
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(2));
        Progress = 33;
        
        try
        {
            Status = "Loading existing chat sessions...";
            
            await _chatSessionManager.LoadChatSessionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load existing chat sessions");
            
            Status = "Error load existing chat sessions";
            IsError = true;
            
            DispatcherTimer.RunOnce(ShutdownForError, TimeSpan.FromSeconds(10), DispatcherPriority.Default);
            
            return;
        }
        
        await Task.Delay(TimeSpan.FromSeconds(2));
        Progress += 33;
        
        
        Status = "Preparing main window...";
        await Task.Delay(TimeSpan.FromSeconds(2));
        
        Progress = 100;
        
        await Task.Delay(TimeSpan.FromSeconds(2));
        
        RequestClose?.Invoke(this, true);
    }

    private void ShutdownForError()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop )
        {
            desktop.Shutdown();
        }
    }

    public void Close()
    {
        RequestClose?.Invoke(this, false);
    }

    public event EventHandler<object?>? RequestClose;
}