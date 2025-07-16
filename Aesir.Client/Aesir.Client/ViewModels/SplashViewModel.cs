using System;
using System.Linq;
using System.Threading.Tasks;
using Aesir.Client.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Irihi.Avalonia.Shared.Contracts;
using Microsoft.Extensions.Logging;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents the splash screen's view model that initializes application state
/// and services. Provides behavior for displaying and managing the splash screen window.
/// </summary>
public partial class SplashViewModel: ObservableRecipient, IDialogContext
{
    /// <summary>
    /// Instance of <see cref="Microsoft.Extensions.Logging.ILogger"/> used for logging activities and errors
    /// within the <see cref="SplashViewModel"/>. This logger facilitates capturing logs for application
    /// diagnostics, debugging, and troubleshooting. It is initialized through dependency injection
    /// in the constructor of the <see cref="SplashViewModel"/> class.
    /// </summary>
    private readonly ILogger<SplashViewModel> _logger;

    /// <summary>
    /// Represents the instance of the <see cref="ApplicationState"/> class that
    /// maintains the current application state and data shared across different
    /// components of the Aesir client application.
    /// </summary>
    private readonly ApplicationState _appState;
    
    /// Represents the current progress value in the range of 0.0 to 100.0, used for tracking the state of an operation or process in the SplashViewModel.
    /// This field is observable and notifies the UI or other bound components when its value changes.
    [ObservableProperty] 
    private double _progress;

    /// <summary>
    /// Represents the current status message displayed in the Splash View.
    /// Typically used to inform the user about the application's initialization progress or other relevant updates.
    /// </summary>
    [ObservableProperty] 
    private string _status = "Starting...";

    /// <summary>
    /// Indicates whether an error condition exists in the SplashViewModel.
    /// </summary>
    [ObservableProperty]
    private bool _isError;

    /// Represents the view model for the splash screen of the application. Handles
    /// initialization tasks and manages the loading of the application state.
    public SplashViewModel(
        ILogger<SplashViewModel> logger, 
        ApplicationState appState)
    {
        _logger = logger;
        _appState = appState;
        
        DispatcherTimer.RunOnce(LoadApplication, TimeSpan.FromMilliseconds(20), DispatcherPriority.Default);
    }

    /// <summary>
    /// Initializes and loads the application by performing a series of asynchronous tasks such as retrieving available models,
    /// loading existing chat sessions, and preparing UI resources. This method also manages error handling during the process
    /// and updates progress and status accordingly. Invokes a close request upon successful completion or schedules a shutdown
    /// in case of errors.
    /// </summary>
    /// <remarks>
    /// This method is triggered automatically after a short delay upon initializing the SplashViewModel. It interacts with
    /// external services to determine the application's readiness and updates progress indicators.
    /// </remarks>
    /// <exception cref="Exception">
    /// An exception is logged if there is an error in fetching models or loading chat sessions, and the application
    /// transitions to an error state.
    /// </exception>
    private async void LoadApplication()
    {
        // NOTE: while some of this loading is helpful its more used to test connections to
        // model host and database.
        
        Progress += 0;
        
        try
        {
            Status = "Determining models available...";
            for (var i = 0; i < 5; i++)
            {
                try
                {
                    Progress += 1;

                    await _appState.LoadAvailableModelsAsync();
                    _appState.SelectedModel = _appState.AvailableModels.FirstOrDefault(m => m.IsChatModel);
                    
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

            for (var i = 0; i < 5; i++)
            {
                try
                {
                    Progress += 1;
                    
                    await _appState.LoadAvailableChatSessionsAsync();
                    
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
        
        Status = "Launching...";
        
        Progress = 100;
        
        await Task.Delay(TimeSpan.FromSeconds(2));
        
        RequestClose?.Invoke(this, true);
    }

    /// Shuts down the desktop application when a critical error occurs, such as
    /// failures during the initialization process or other unrecoverable issues.
    /// This method is invoked as part of the error handling mechanism when specific
    /// operations in the application cannot be completed successfully. It ensures
    /// proper termination of the application to prevent it from remaining in an
    /// inconsistent or unusable state.
    /// If the current application lifetime is of type `IClassicDesktopStyleApplicationLifetime`,
    /// the method will proceed to gracefully terminate the application by calling
    /// its `Shutdown` method. This approach ensures that any necessary cleanup
    /// operations associated with application lifetime are executed before the app exits.
    private void ShutdownForError()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop )
        {
            desktop.Shutdown();
        }
    }

    /// <summary>
    /// Triggers a request to close the current dialog context.
    /// </summary>
    /// <remarks>
    /// This method invokes the <see cref="RequestClose"/> event, signaling subscribers
    /// to close the associated dialog or view. The event is raised with a status of `false`.
    /// </remarks>
    public void Close()
    {
        RequestClose?.Invoke(this, false);
    }

    /// <summary>
    /// Event triggered to request closing the current dialog or view.
    /// </summary>
    /// <remarks>
    /// This event allows the SplashViewModel to notify subscribers that it should be closed,
    /// optionally providing additional information or a status flag.
    /// </remarks>
    public event EventHandler<object?>? RequestClose;
}