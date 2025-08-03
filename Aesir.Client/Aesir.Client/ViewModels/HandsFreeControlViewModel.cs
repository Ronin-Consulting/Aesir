using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Services;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

/// <summary>
/// ViewModel for the HandsFreeControl.
/// Manages the state, animations, and user interactions for hands-free voice mode.
/// </summary>
public partial class HandsFreeControlViewModel : ObservableRecipient, IDisposable
{
    private readonly ILogger<HandsFreeControlViewModel> _logger;
    private readonly IHandsFreeService _handsFreeService;

    [ObservableProperty] private string _stateText = "Ready";
    [ObservableProperty] private string _stateClass = "idle";
    [ObservableProperty] private IBrush _iconColor = Brushes.Gray;
    [ObservableProperty] private IBrush _stateTextColor = Brushes.Gray;
    [ObservableProperty] private string _toggleButtonText = "Start Hands-Free";
    [ObservableProperty] private IBrush _toggleButtonBackground = Brushes.Green;
    [ObservableProperty] private double _audioLevel = 1.0;
    [ObservableProperty] private bool _isProcessing;
    
    public ICommand ToggleHandsFreeCommand { get; }
    public ICommand ShowSettingsCommand { get; }
    
    public HandsFreeControlViewModel(
        ILogger<HandsFreeControlViewModel> logger,
        IHandsFreeService handsFreeService)
    {
        _logger = logger;
        _handsFreeService = handsFreeService;

        ToggleHandsFreeCommand = new AsyncRelayCommand(ToggleHandsFreeAsync);
        ShowSettingsCommand = new RelayCommand(ShowSettings);

        // Subscribe to hands-free service events
        _handsFreeService.StateChanged += OnHandsFreeStateChanged;
        _handsFreeService.AudioLevelChanged += OnAudioLevelChanged;
    }

    protected override async void OnActivated()
    {
        //await ToggleHandsFreeAsync();
    }

    /// <summary>
    /// Toggles hands-free mode on/off.
    /// </summary>
    private async Task ToggleHandsFreeAsync()
    {
        if (_isProcessing) return;

        try
        {
            _isProcessing = true;

            if (_handsFreeService.IsHandsFreeActive)
            {
                await _handsFreeService.StopHandsFreeMode();
            }
            else
            {
                await _handsFreeService.StartHandsFreeMode();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling hands-free mode");
            UpdateUIForState(HandsFreeState.Error, ex.Message);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    /// <summary>
    /// Shows settings dialog (placeholder for future implementation).
    /// </summary>
    private void ShowSettings()
    {
        _logger.LogInformation("Settings requested (not implemented yet)");
        // TODO: Implement settings dialog
    }

    /// <summary>
    /// Handles hands-free state changes.
    /// </summary>
    private void OnHandsFreeStateChanged(object? sender, HandsFreeStateChangedEventArgs e)
    {
        UpdateUIForState(e.CurrentState, e.ErrorMessage);
    }

    /// <summary>
    /// Handles audio level changes during TTS playback.
    /// </summary>
    private void OnAudioLevelChanged(object? sender, AudioLevelEventArgs e)
    {
        // Scale audio level for visual effect (1.0 to 1.3 range)
        AudioLevel = e.IsAudioActive ? Math.Max(1.0, 1.0 + (e.AudioLevel * 0.3)) : 1.0;
    }

    /// <summary>
    /// Updates the UI elements based on the current hands-free state.
    /// </summary>
    private void UpdateUIForState(HandsFreeState state, string? errorMessage = null)
    {
        switch (state)
        {
            case HandsFreeState.Idle:
                StateText = "Ready";
                StateClass = "idle";
                IconColor = Brushes.Gray;
                StateTextColor = Brushes.Gray;
                ToggleButtonText = "Start Hands-Free";
                ToggleButtonBackground = Brushes.Green;
                AudioLevel = 1.0;
                break;

            case HandsFreeState.Listening:
                StateText = "Listening...";
                StateClass = "listening";
                IconColor = Brushes.White;
                StateTextColor = new SolidColorBrush(Color.FromRgb(74, 144, 226)); // Light blue
                ToggleButtonText = "Stop Hands-Free";
                ToggleButtonBackground = Brushes.Red;
                AudioLevel = 1.0;
                break;

            case HandsFreeState.Processing:
                StateText = "Processing...";
                StateClass = "processing";
                IconColor = Brushes.White;
                StateTextColor = new SolidColorBrush(Color.FromRgb(255, 179, 71)); // Orange
                ToggleButtonText = "Stop Hands-Free";
                ToggleButtonBackground = Brushes.Red;
                AudioLevel = 1.0;
                break;

            case HandsFreeState.Speaking:
                StateText = "Speaking...";
                StateClass = "speaking";
                IconColor = Brushes.White;
                StateTextColor = new SolidColorBrush(Color.FromRgb(46, 204, 113)); // Green
                ToggleButtonText = "Stop Hands-Free";
                ToggleButtonBackground = Brushes.Red;
                // AudioLevel will be updated by OnAudioLevelChanged
                break;

            case HandsFreeState.Error:
                StateText = string.IsNullOrEmpty(errorMessage) ? "Error" : $"Error: {errorMessage}";
                StateClass = "error";
                IconColor = Brushes.White;
                StateTextColor = new SolidColorBrush(Color.FromRgb(255, 107, 107)); // Red
                ToggleButtonText = "Start Hands-Free";
                ToggleButtonBackground = Brushes.Green;
                AudioLevel = 1.0;
                break;

            default:
                _logger.LogWarning("Unknown hands-free state: {State}", state);
                break;
        }

        _logger.LogDebug("UI updated for state: {State}", state);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _handsFreeService.StateChanged -= OnHandsFreeStateChanged;
            _handsFreeService.AudioLevelChanged -= OnAudioLevelChanged;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}