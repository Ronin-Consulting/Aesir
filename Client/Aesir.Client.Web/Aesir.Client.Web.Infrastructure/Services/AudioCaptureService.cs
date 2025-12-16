using Microsoft.JSInterop;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Service for capturing audio from the microphone using Web Audio API via JS interop.
/// Implements push-to-talk functionality for hands-free voice interaction.
/// </summary>
public class AudioCaptureService : IAudioCaptureService
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<AudioCaptureService>? _dotNetRef;
    private bool _disposed;
    private bool? _isSupported;
    private bool? _hasOpusSupport;

    /// <inheritdoc />
    public bool IsSupported => _isSupported ?? false;

    /// <inheritdoc />
    public bool HasOpusSupport => _hasOpusSupport ?? false;

    /// <inheritdoc />
    public AudioCaptureState State { get; private set; } = AudioCaptureState.NotInitialized;

    /// <inheritdoc />
    public event EventHandler<AudioChunkEventArgs>? OnAudioChunk;

    /// <inheritdoc />
    public event EventHandler<CaptureErrorEventArgs>? OnCaptureError;

    /// <inheritdoc />
    public event EventHandler<AudioCaptureState>? OnStateChanged;

    /// <summary>
    /// Creates a new AudioCaptureService.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime for interop.</param>
    public AudioCaptureService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public async Task<bool> InitializeAsync(int sampleRate = 16000)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AudioCaptureService));

        if (State != AudioCaptureState.NotInitialized)
            return State == AudioCaptureState.Ready;

        try
        {
            // Check capabilities first
            _isSupported = await _jsRuntime.InvokeAsync<bool>("aesirAudio.capabilities.isSupported");
            _hasOpusSupport = await _jsRuntime.InvokeAsync<bool>("aesirAudio.capabilities.hasOpusSupport");

            if (!_isSupported.Value)
            {
                SetState(AudioCaptureState.Error);
                return false;
            }

            // Create .NET reference for callbacks
            _dotNetRef = DotNetObjectReference.Create(this);

            // Initialize the audio system
            // Note: We only need to pass the dotNetRef here for callbacks
            // The full initialization happens when we start capture
            var result = await _jsRuntime.InvokeAsync<bool>(
                "aesirAudio.initialize",
                _dotNetRef,
                sampleRate,
                22050, // Playback sample rate - TTS typically uses 22kHz
                "capture" // Service type for callback routing
            );

            if (result)
            {
                SetState(AudioCaptureState.Ready);
                return true;
            }

            SetState(AudioCaptureState.Error);
            return false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to initialize audio capture: {ex.Message}");
            SetState(AudioCaptureState.Error);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> WarmUpAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AudioCaptureService));

        // Ensure initialized first
        if (State == AudioCaptureState.NotInitialized)
        {
            var initialized = await InitializeAsync();
            if (!initialized)
                return false;
        }

        try
        {
            // Call the JavaScript warm-up function to pre-initialize microphone
            return await _jsRuntime.InvokeAsync<bool>("aesirAudio.capture.warmUp");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to warm up audio capture: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> StartCaptureAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AudioCaptureService));

        if (State == AudioCaptureState.NotInitialized)
        {
            var initialized = await InitializeAsync();
            if (!initialized)
                return false;
        }

        if (State == AudioCaptureState.Capturing)
            return true;

        try
        {
            var result = await _jsRuntime.InvokeAsync<bool>("aesirAudio.capture.start");

            if (result)
            {
                SetState(AudioCaptureState.Capturing);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to start audio capture: {ex.Message}");
            OnCaptureError?.Invoke(this, new CaptureErrorEventArgs(ex.Message));
            return false;
        }
    }

    /// <inheritdoc />
    public async Task StopCaptureAsync()
    {
        if (_disposed)
            return;

        if (State != AudioCaptureState.Capturing)
            return;

        try
        {
            await _jsRuntime.InvokeVoidAsync("aesirAudio.capture.stop");
            SetState(AudioCaptureState.Ready);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to stop audio capture: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<string> GetMicrophonePermissionAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AudioCaptureService));

        try
        {
            return await _jsRuntime.InvokeAsync<string>(
                "aesirAudio.capabilities.getMicrophonePermission");
        }
        catch
        {
            return "prompt";
        }
    }

    /// <summary>
    /// Called from JavaScript when an audio chunk is captured.
    /// </summary>
    /// <param name="base64Data">Base64 encoded audio data (raw 16-bit PCM).</param>
    [JSInvokable("OnAudioChunk")]
    public void HandleAudioChunk(string base64Data)
    {
        if (_disposed)
            return;

        try
        {
            var data = Convert.FromBase64String(base64Data);
            // Now using raw 16-bit PCM format (16kHz, mono, little-endian)
            OnAudioChunk?.Invoke(this, new AudioChunkEventArgs(data, "pcm/16bit"));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to process audio chunk: {ex.Message}");
        }
    }

    /// <summary>
    /// Called from JavaScript when a capture error occurs.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    [JSInvokable("OnCaptureError")]
    public void HandleCaptureError(string errorMessage)
    {
        if (_disposed)
            return;

        SetState(AudioCaptureState.Error);
        OnCaptureError?.Invoke(this, new CaptureErrorEventArgs(errorMessage));
    }

    private void SetState(AudioCaptureState newState)
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
            await StopCaptureAsync();

            if (_dotNetRef != null)
            {
                await _jsRuntime.InvokeVoidAsync("aesirAudio.dispose");
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
