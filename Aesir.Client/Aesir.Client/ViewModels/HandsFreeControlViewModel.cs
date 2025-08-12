using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

/// <summary>
/// ViewModel responsible for managing the hands-free control functionality.
/// Handles lifecycle events, commands, and interactions with services related to hands-free operations.
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public partial class HandsFreeControlViewModel : ObservableRecipient, IDisposable
{
    /// <summary>
    /// Logger instance used for logging messages, errors, and debugging information
    /// within the HandsFreeControlViewModel class.
    /// </summary>
    private readonly ILogger<HandsFreeControlViewModel> _logger;

    /// <summary>
    /// An instance of the <see cref="IHandsFreeService"/> interface used to manage
    /// hands-free voice interaction functionality. Provides capabilities such as
    /// starting or stopping hands-free mode, monitoring its state, and responding
    /// to relevant events like state changes and audio level updates.
    /// </summary>
    private readonly IHandsFreeService _handsFreeService;

    /// <summary>
    /// Service responsible for handling navigation between views or features
    /// within the application. Provides methods to navigate to specific
    /// destinations such as Chat, Tools, Agents, or Hands-Free modes.
    /// </summary>
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Represents the current operational state of the hands-free interaction system in the HandsFreeControlViewModel.
    /// Tracks and updates the state for user interactions and state transitions, such as idle, listening, processing commands,
    /// speaking, or encountering an error.
    /// </summary>
    [ObservableProperty] private HandsFreeState _currentState;

    /// <summary>
    /// Represents the current audio level for hands-free voice control.
    /// Used to monitor and adjust the volume or intensity of audio input/output
    /// in the HandsFreeControl view model.
    /// </summary>
    [ObservableProperty] private double _audioLevel = 1.0;

    /// <summary>
    /// Represents the current processing state of the hands-free control.
    /// When set to true, indicates that a process is ongoing (e.g., voice command recognition or execution).
    /// When set to false, indicates that no active processing is happening.
    /// </summary>
    [ObservableProperty] private bool _isProcessing;

    /// <summary>
    /// Gets the command invoked to exit hands-free mode.
    /// This command triggers the necessary logic to stop hands-free
    /// functionality and navigate back to the chat interface.
    /// </summary>
    public ICommand ExitHandsFreeCommand { get; }

    /// <summary>
    /// ViewModel for managing the hands-free control feature, including state handling, animations, and user interactions.
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
        
        // Subscribe to hands-free service events
        _handsFreeService.StateChanged += OnHandsFreeStateChanged;
        _handsFreeService.AudioLevelChanged += OnAudioLevelChanged;
    }

    /// <summary>
    /// Called when the ViewModel is activated.
    /// Initiates the process to start the hands-free mode asynchronously.
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
    /// Initiates hands-free mode by ensuring all necessary preconditions are met
    /// and starting the hands-free service if not already active.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation of starting hands-free mode.</returns>
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
    /// Stops the hands-free mode, ensuring any active hands-free state is deactivated.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation of stopping hands-free mode.</returns>
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
    /// Handles events raised when the hands-free state changes.
    /// Updates the current state based on the event arguments.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Provides data for the hands-free state change event.</param>
    private void OnHandsFreeStateChanged(object? sender, HandsFreeStateChangedEventArgs e)
    {
        CurrentState = e.CurrentState;
    }

    /// <summary>
    /// Handles changes in audio level during hands-free mode TTS playback.
    /// Scales and adjusts the audio level for visual feedback purposes.
    /// </summary>
    /// <param name="sender">The source of the event, typically the hands-free service.</param>
    /// <param name="e">The event data containing the audio level information and activity state.</param>
    private void OnAudioLevelChanged(object? sender, AudioLevelEventArgs e)
    {
        // Scale audio level for visual effect (1.0 to 1.3 range)
        AudioLevel = e.IsAudioActive ? Math.Max(1.0, 1.0 + (e.AudioLevel * 0.3)) : 1.0;
    }


    /// <summary>
    /// Releases the unmanaged resources used by the HandsFreeControlViewModel and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">A boolean value indicating whether to release managed resources (true) or only unmanaged resources (false).</param>
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
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}