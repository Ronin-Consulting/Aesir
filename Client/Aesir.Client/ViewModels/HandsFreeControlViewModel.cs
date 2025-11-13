using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

/// <summary>
/// ViewModel responsible for managing hands-free control functionality in the application.
/// Implements lifecycle management, service interactions, and provides commands for starting, pausing, and exiting hands-free mode.
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public partial class HandsFreeControlViewModel : ObservableRecipient, IDisposable
{
    /// <summary>
    /// Private readonly logger instance used to log messages, errors, and debugging
    /// information throughout the HandsFreeControlViewModel class.
    /// </summary>
    private readonly ILogger<HandsFreeControlViewModel> _logger;

    /// <summary>
    /// Instance of the <see cref="IHandsFreeService"/> interface utilized to handle hands-free
    /// operations including starting or stopping hands-free mode and monitoring related
    /// events such as state changes or audio level updates.
    /// </summary>
    private readonly IHandsFreeService _handsFreeService;

    /// <summary>
    /// Navigation service used for directing the application to various views or features.
    /// Facilitates transitions between different application components, such as Chat, Tools,
    /// Agents, or Hands-Free modes.
    /// </summary>
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Represents the current state of the hands-free interaction system, indicating
    /// the current mode of operation such as idle, listening, processing, speaking,
    /// or error, in the HandsFreeControlViewModel class.
    /// </summary>
    [ObservableProperty] private HandsFreeState _currentState;

    /// <summary>
    /// Indicates whether the hands-free control functionality is currently idle.
    /// </summary>
    [ObservableProperty] private bool _isIdle;

    /// <summary>
    /// Represents the current audio level for hands-free voice control.
    /// This property is used to track and manage the audio intensity
    /// required for processing hands-free interactions.
    /// </summary>
    [ObservableProperty] private double _audioLevel = 1.0;

    /// <summary>
    /// Indicates whether a process related to hands-free control is currently active.
    /// True denotes that a process is in progress (e.g., voice command handling),
    /// while false signifies that no processing is occurring.
    /// </summary>
    [ObservableProperty] private bool _isProcessing;

    [ObservableProperty] private string _currentUtteranceText;
    
    /// <summary>
    /// Command used to exit the hands-free mode within the application.
    /// It triggers the associated logic to stop all hands-free operations and handle any necessary cleanup.
    /// </summary>
    public ICommand ExitHandsFreeCommand { get; }

    /// <summary>
    /// Command that pauses the hands-free functionality by invoking the associated
    /// asynchronous operation in the HandsFreeControlViewModel. Typically used to
    /// momentarily stop hands-free interactions while keeping the functionality
    /// initialized for resumption.
    /// </summary>
    public ICommand PauseHandsFreeCommand { get; }

    /// <summary>
    /// Command responsible for initiating hands-free mode operations within the application.
    /// Triggers the execution of tasks or services required to start hands-free functionality.
    /// </summary>
    public ICommand StartHandsFreeCommand { get; }

    /// <summary>
    /// ViewModel responsible for managing the hands-free control functionalities, including state changes, handling user commands, and managing audio levels.
    /// </summary>
    public HandsFreeControlViewModel(
        ILogger<HandsFreeControlViewModel> logger,
        IHandsFreeService handsFreeService,
        INavigationService navigationService)
    {
        _logger = logger;
        _handsFreeService = handsFreeService;
        _navigationService = navigationService;

        ExitHandsFreeCommand = new AsyncRelayCommand(ExitHandsFreeAsync);
        PauseHandsFreeCommand = new AsyncRelayCommand(PauseHandsFreeAsync);
        StartHandsFreeCommand = new AsyncRelayCommand(StartHandsFreeAsync);
        
        // Subscribe to hands-free service events
        _handsFreeService.StateChanged += OnHandsFreeStateChanged;
        _handsFreeService.AudioLevelChanged += OnAudioLevelChanged;
        _handsFreeService.UtteranceTextRecognized += OnUtteranceTextRecognized;
    }

    /// <summary>
    /// Called when the ViewModel is activated.
    /// Triggers the asynchronous initialization of hands-free mode by invoking the appropriate startup workflows.
    /// </summary>
    protected override async void OnActivated()
    {
        await StartHandsFreeAsync();
    }

    /// <summary>
    /// Exits hands-free mode and navigates to the Chat view.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ExitHandsFreeAsync()
    {
        await StopHandsFreeAsync();

        _navigationService.NavigateToChat();
    }

    /// <summary>
    /// Asynchronously pauses the hands-free control functionality.
    /// Ensures any active hands-free operations are stopped.
    /// </summary>
    /// <returns>A task that represents the asynchronous pause operation.</returns>
    private async Task PauseHandsFreeAsync()
    {
        await StopHandsFreeAsync();
    }

    /// <summary>
    /// Starts hands-free mode by stopping any active instance, ensuring preconditions are met,
    /// and initializing the hands-free service if not already running.
    /// </summary>
    /// <returns>A task representing the asynchronous operation of starting hands-free mode.</returns>
    private async Task StartHandsFreeAsync()
    {
        await StopHandsFreeAsync();

        if (IsProcessing) return;

        try
        {
            IsProcessing = true;

            if (!_handsFreeService.IsHandsFreeActive)
            {
                await _handsFreeService.StartHandsFreeMode();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting hands-free mode");
            CurrentState = HandsFreeState.Error;
            
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Stops the hands-free mode, ensuring any active hands-free state is properly deactivated.
    /// </summary>
    /// <returns>A task representing the asynchronous operation of stopping the hands-free mode.</returns>
    private async Task StopHandsFreeAsync()
    {
        if (!IsProcessing) return;

        try
        {
            IsProcessing = false;

            if (_handsFreeService.IsHandsFreeActive)
            {
                await _handsFreeService.StopHandsFreeMode();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stoping hands-free mode");
            CurrentState = HandsFreeState.Error;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Handles the event triggered when the hands-free state changes.
    /// Updates the relevant properties based on the new state.
    /// </summary>
    /// <param name="sender">The object that raised the event.</param>
    /// <param name="e">Event arguments containing details about the hands-free state change.</param>
    private void OnHandsFreeStateChanged(object? sender, HandsFreeStateChangedEventArgs e)
    {
        CurrentState = e.CurrentState;

        IsIdle = CurrentState == HandsFreeState.Idle;
    }

    /// <summary>
    /// Handles the event triggered when the audio level changes during hands-free mode.
    /// Adjusts the audio level dynamically for visual feedback or other purposes.
    /// </summary>
    /// <param name="sender">The source of the event, generally the hands-free service.</param>
    /// <param name="e">An instance of <see cref="AudioLevelEventArgs"/> containing information about the audio level and its activity state.</param>
    private void OnAudioLevelChanged(object? sender, AudioLevelEventArgs e)
    {
        // Scale audio level for visual effect (1.0 to 1.3 range)
        AudioLevel = e.IsAudioActive ? Math.Max(1.0, 1.0 + (e.AudioLevel * 0.3)) : 1.0;
    }

    private void OnUtteranceTextRecognized(object? sender, UtteranceTextEventArgs e)
    {
        CurrentUtteranceText = e.Text;
    }

    /// <summary>
    /// Releases resources used by the HandsFreeControlViewModel, both managed and unmanaged.
    /// </summary>
    /// <param name="disposing">Indicates whether to release managed resources (true) or only unmanaged resources (false).</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsActive = false;

            _handsFreeService.StateChanged -= OnHandsFreeStateChanged;
            _handsFreeService.AudioLevelChanged -= OnAudioLevelChanged;
        }
    }

    /// <summary>
    /// Releases the resources used by the HandsFreeControlViewModel.
    /// Ensures proper cleanup of both managed and unmanaged resources
    /// to prevent memory leaks and other resource-related issues.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}