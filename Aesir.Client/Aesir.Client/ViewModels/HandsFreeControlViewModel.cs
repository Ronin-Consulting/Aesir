using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

/// <summary>
/// ViewModel for the HandsFreeControl.
/// Manages the state, animations, and user interactions for hands-free voice mode.
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public partial class HandsFreeControlViewModel : ObservableRecipient, IDisposable
{
    private readonly ILogger<HandsFreeControlViewModel> _logger;
    private readonly IHandsFreeService _handsFreeService;
    private readonly INavigationService _navigationService;

    [ObservableProperty] private HandsFreeState _currentState;
    [ObservableProperty] private double _audioLevel = 1.0;
    [ObservableProperty] private bool _isProcessing;
    
    public ICommand ExitHandsFreeCommand { get; }
    
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

    protected override async void OnActivated()
    {
        await StartHandsFreeAsync();
    }

    /// <summary>
    /// Toggles hands-free mode on/off.
    /// </summary>
    private async Task ExitHandsFreeAsync()
    {
        await StopHandsFreeAsync();
        
        _navigationService.NavigateToChat();
    }
    
    private async Task StartHandsFreeAsync()
    {
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
        }
        finally
        {
            IsProcessing = false;
        }
    }
    
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
    /// Handles hands-free state changes.
    /// </summary>
    private void OnHandsFreeStateChanged(object? sender, HandsFreeStateChangedEventArgs e)
    {
        CurrentState = e.CurrentState;
    }

    /// <summary>
    /// Handles audio level changes during TTS playback.
    /// </summary>
    private void OnAudioLevelChanged(object? sender, AudioLevelEventArgs e)
    {
        // Scale audio level for visual effect (1.0 to 1.3 range)
        AudioLevel = e.IsAudioActive ? Math.Max(1.0, 1.0 + (e.AudioLevel * 0.3)) : 1.0;
    }

    
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsActive = false;
            
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