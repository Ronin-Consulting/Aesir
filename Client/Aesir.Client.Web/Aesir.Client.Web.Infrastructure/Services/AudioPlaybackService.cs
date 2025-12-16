using Microsoft.JSInterop;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Service for playing audio using Web Audio API via JS interop.
/// Handles TTS audio output for hands-free voice interaction.
/// </summary>
public class AudioPlaybackService : IAudioPlaybackService
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<AudioPlaybackService>? _dotNetRef;
    private bool _disposed;
    private bool? _isSupported;

    /// <inheritdoc />
    public bool IsSupported => _isSupported ?? false;

    /// <inheritdoc />
    public AudioPlaybackState State { get; private set; } = AudioPlaybackState.NotInitialized;

    /// <inheritdoc />
    public bool IsPlaying => State == AudioPlaybackState.Playing;

    /// <inheritdoc />
    public event EventHandler<AudioPlaybackState>? OnStateChanged;

    /// <inheritdoc />
    public event EventHandler? OnPlaybackComplete;

    /// <summary>
    /// Creates a new AudioPlaybackService.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime for interop.</param>
    public AudioPlaybackService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public async Task<bool> InitializeAsync(int sampleRate = 22050)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AudioPlaybackService));

        if (State != AudioPlaybackState.NotInitialized)
            return State == AudioPlaybackState.Ready || State == AudioPlaybackState.Playing;

        try
        {
            // Check if Web Audio is supported
            _isSupported = await _jsRuntime.InvokeAsync<bool>("aesirAudio.capabilities.isSupported");

            if (!_isSupported.Value)
            {
                SetState(AudioPlaybackState.Error);
                return false;
            }

            // Create .NET reference for callbacks
            _dotNetRef = DotNetObjectReference.Create(this);

            // Initialize the audio system
            // Note: The audio system might already be initialized by the capture service
            // This is fine - the JS code handles multiple init calls gracefully
            var result = await _jsRuntime.InvokeAsync<bool>(
                "aesirAudio.initialize",
                _dotNetRef,
                16000, // Capture sample rate
                sampleRate, // Playback sample rate
                "playback" // Service type for callback routing
            );

            if (result)
            {
                SetState(AudioPlaybackState.Ready);
                return true;
            }

            SetState(AudioPlaybackState.Error);
            return false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to initialize audio playback: {ex.Message}");
            SetState(AudioPlaybackState.Error);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> QueueAudioAsync(byte[] audioData, string format = "wav")
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AudioPlaybackService));

        if (State == AudioPlaybackState.NotInitialized)
        {
            var initialized = await InitializeAsync();
            if (!initialized)
                return false;
        }

        try
        {
            // Convert to base64
            var base64Data = Convert.ToBase64String(audioData);

            // Queue the audio chunk
            var result = await _jsRuntime.InvokeAsync<bool>(
                "aesirAudio.playback.queueChunk",
                base64Data,
                format
            );

            if (result && State == AudioPlaybackState.Ready)
            {
                SetState(AudioPlaybackState.Buffering);
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to queue audio: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task PlayAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AudioPlaybackService));

        if (State == AudioPlaybackState.NotInitialized)
            return;

        try
        {
            await _jsRuntime.InvokeVoidAsync("aesirAudio.playback.play");
            SetState(AudioPlaybackState.Playing);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to start playback: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        if (_disposed)
            return;

        if (State == AudioPlaybackState.NotInitialized)
            return;

        try
        {
            await _jsRuntime.InvokeVoidAsync("aesirAudio.playback.stop");
            SetState(AudioPlaybackState.Ready);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to stop playback: {ex.Message}");
        }
    }

    /// <summary>
    /// Called from JavaScript when playback completes.
    /// </summary>
    [JSInvokable("OnPlaybackComplete")]
    public void HandlePlaybackComplete()
    {
        if (_disposed)
            return;

        SetState(AudioPlaybackState.Ready);
        OnPlaybackComplete?.Invoke(this, EventArgs.Empty);
    }

    private void SetState(AudioPlaybackState newState)
    {
        if (State != newState)
        {
            State = newState;
            OnStateChanged?.Invoke(this, newState);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            await StopAsync();

            if (_dotNetRef != null)
            {
                // Don't call dispose on the JS side - might be shared with capture service
                _dotNetRef.Dispose();
                _dotNetRef = null;
            }
        }
        catch
        {
            // Ignore disposal errors
        }

        GC.SuppressFinalize(this);
    }
}
