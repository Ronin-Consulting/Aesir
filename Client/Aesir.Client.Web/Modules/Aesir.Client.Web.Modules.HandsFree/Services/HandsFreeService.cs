using System.Threading.Channels;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.HandsFree.Models;

namespace Aesir.Client.Web.Modules.HandsFree.Services;

/// <summary>
/// Orchestrates the hands-free voice interaction flow.
/// Implements the state machine: Idle → Listening → Processing → Speaking → Idle
/// </summary>
public class HandsFreeService : IHandsFreeService
{
    private readonly IAudioCaptureService _audioCapture;
    private readonly IAudioPlaybackService _audioPlayback;
    private readonly ISignalRSpeechService _speechService;
    private readonly IApiClient _apiClient;

    private readonly Channel<byte[]> _audioChannel;
    private CancellationTokenSource? _processingCts;
    private bool _disposed;
    private bool _initialized;

    /// <inheritdoc />
    public HandsFreeState State { get; private set; } = HandsFreeState.Idle;

    /// <inheritdoc />
    public Guid? CurrentAgentId { get; set; }

    /// <inheritdoc />
    public Guid? CurrentConversationId { get; set; }

    /// <inheritdoc />
    public string? LastTranscription { get; private set; }

    /// <inheritdoc />
    public string? LastResponse { get; private set; }

    /// <inheritdoc />
    public string? LastError { get; private set; }

    /// <inheritdoc />
    public event EventHandler<HandsFreeStateChangedEventArgs>? OnStateChanged;

    /// <inheritdoc />
    public event EventHandler<TranscriptionEventArgs>? OnTranscription;

    /// <inheritdoc />
    public event EventHandler<string>? OnResponseText;

    /// <summary>
    /// Creates a new HandsFreeService.
    /// </summary>
    public HandsFreeService(
        IAudioCaptureService audioCapture,
        IAudioPlaybackService audioPlayback,
        ISignalRSpeechService speechService,
        IApiClient apiClient)
    {
        _audioCapture = audioCapture;
        _audioPlayback = audioPlayback;
        _speechService = speechService;
        _apiClient = apiClient;

        // Create unbounded channel for audio chunks
        _audioChannel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        // Subscribe to audio capture events
        _audioCapture.OnAudioChunk += HandleAudioChunk;
        _audioCapture.OnCaptureError += HandleCaptureError;

        // Subscribe to playback events
        _audioPlayback.OnPlaybackComplete += HandlePlaybackComplete;
    }

    /// <inheritdoc />
    public async Task<bool> InitializeAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HandsFreeService));

        if (_initialized)
            return true;

        try
        {
            // Initialize audio services
            var captureInit = await _audioCapture.InitializeAsync();
            var playbackInit = await _audioPlayback.InitializeAsync();

            if (!captureInit || !playbackInit)
            {
                SetState(HandsFreeState.Error, "Failed to initialize audio services");
                return false;
            }

            // Connect to SignalR hubs
            var hubsConnected = await _speechService.ConnectAsync();
            if (!hubsConnected)
            {
                SetState(HandsFreeState.Error, "Failed to connect to speech services");
                return false;
            }

            _initialized = true;
            SetState(HandsFreeState.Idle);
            return true;
        }
        catch (Exception ex)
        {
            SetState(HandsFreeState.Error, $"Initialization failed: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task StartListeningAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HandsFreeService));

        if (!_initialized)
        {
            var init = await InitializeAsync();
            if (!init)
                return;
        }

        // If we're speaking, interrupt first
        if (State == HandsFreeState.Speaking)
        {
            await InterruptAsync();
        }

        if (State != HandsFreeState.Idle)
            return;

        try
        {
            // Clear previous transcription
            LastTranscription = null;

            // Start capturing audio
            var started = await _audioCapture.StartCaptureAsync();
            if (started)
            {
                SetState(HandsFreeState.Listening);
            }
            else
            {
                SetState(HandsFreeState.Error, "Failed to start audio capture");
            }
        }
        catch (Exception ex)
        {
            SetState(HandsFreeState.Error, $"Failed to start listening: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task StopListeningAsync()
    {
        if (_disposed)
            return;

        if (State != HandsFreeState.Listening)
            return;

        try
        {
            // Stop audio capture
            await _audioCapture.StopCaptureAsync();

            // Complete the audio channel to signal end of stream
            _audioChannel.Writer.TryComplete();

            // Transition to processing
            SetState(HandsFreeState.Processing);

            // Start the processing pipeline
            _processingCts = new CancellationTokenSource();
            _ = ProcessSpeechAsync(_processingCts.Token);
        }
        catch (Exception ex)
        {
            SetState(HandsFreeState.Error, $"Failed to stop listening: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task InterruptAsync()
    {
        if (_disposed)
            return;

        // Cancel any ongoing processing
        _processingCts?.Cancel();

        // Stop audio capture if listening
        if (State == HandsFreeState.Listening)
        {
            await _audioCapture.StopCaptureAsync();
        }

        // Stop playback if speaking
        if (State == HandsFreeState.Speaking)
        {
            await _audioPlayback.StopAsync();
        }

        // Reset the audio channel
        ResetAudioChannel();

        SetState(HandsFreeState.Idle);
    }

    /// <inheritdoc />
    public async Task ResetAsync()
    {
        await InterruptAsync();
        LastError = null;
        LastTranscription = null;
        LastResponse = null;
    }

    private async Task ProcessSpeechAsync(CancellationToken cancellationToken)
    {
        try
        {
            var transcriptionBuilder = new System.Text.StringBuilder();

            // Note: In a real implementation, we would stream audio to STT
            // For now, we'll simulate the flow with collected audio

            // For actual implementation, the audio channel would feed into
            // the SignalR speech service. This is a simplified version.

            // Simulate STT processing - in production this would use:
            // await foreach (var text in _speechService.StreamSpeechToTextAsync(GetAudioStream(), cancellationToken))

            // For now, we'll use the collected audio and process it
            // This would be replaced with actual STT streaming

            await Task.Delay(500, cancellationToken); // Simulate processing time

            // Get transcription (this would come from actual STT)
            var transcription = LastTranscription ?? "Hello, how can you help me today?";
            LastTranscription = transcription;
            OnTranscription?.Invoke(this, new TranscriptionEventArgs(transcription, true));

            if (cancellationToken.IsCancellationRequested)
                return;

            // Send to chat API and get response
            var response = await SendToChatAsync(transcription, cancellationToken);

            if (string.IsNullOrEmpty(response))
            {
                SetState(HandsFreeState.Error, "No response from assistant");
                return;
            }

            LastResponse = response;
            OnResponseText?.Invoke(this, response);

            if (cancellationToken.IsCancellationRequested)
                return;

            // Generate TTS and play
            SetState(HandsFreeState.Speaking);
            await PlayResponseAsync(response, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Interrupted - handled by caller
        }
        catch (Exception ex)
        {
            SetState(HandsFreeState.Error, $"Processing failed: {ex.Message}");
        }
    }

    private async Task<string?> SendToChatAsync(string userMessage, CancellationToken cancellationToken)
    {
        try
        {
            if (!CurrentAgentId.HasValue)
            {
                return "Please select an agent first.";
            }

            // Create a chat request
            var request = new
            {
                agentId = CurrentAgentId.Value,
                conversationId = CurrentConversationId,
                message = userMessage
            };

            // Note: This would use the actual chat streaming endpoint
            // For now, we use a simple POST
            var response = await _apiClient.PostAsync<ChatResponseDto>(
                "/api/chat/send",
                request,
                cancellationToken);

            // Update conversation ID if new
            if (response?.ConversationId != null)
            {
                CurrentConversationId = response.ConversationId;
            }

            return response?.Content;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Chat API error: {ex.Message}");
            return null;
        }
    }

    private async Task PlayResponseAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            // Stream TTS audio and play
            await foreach (var audioChunk in _speechService.StreamTextToSpeechAsync(text, 1.0f, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await _audioPlayback.QueueAudioAsync(audioChunk, "wav");
            }

            // Start playback if not already playing
            await _audioPlayback.PlayAsync();
        }
        catch (OperationCanceledException)
        {
            // Interrupted
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"TTS playback error: {ex.Message}");
            SetState(HandsFreeState.Error, $"Playback failed: {ex.Message}");
        }
    }

    private void HandleAudioChunk(object? sender, AudioChunkEventArgs e)
    {
        if (State == HandsFreeState.Listening)
        {
            // Queue audio chunk for processing
            _audioChannel.Writer.TryWrite(e.Data);
        }
    }

    private void HandleCaptureError(object? sender, CaptureErrorEventArgs e)
    {
        SetState(HandsFreeState.Error, e.Message);
    }

    private void HandlePlaybackComplete(object? sender, EventArgs e)
    {
        if (State == HandsFreeState.Speaking)
        {
            SetState(HandsFreeState.Idle);
        }
    }

    private void ResetAudioChannel()
    {
        // Drain and reset the channel
        while (_audioChannel.Reader.TryRead(out _)) { }
    }

    private void SetState(HandsFreeState newState, string? errorMessage = null)
    {
        if (State == newState)
            return;

        var previousState = State;
        State = newState;
        LastError = errorMessage;

        OnStateChanged?.Invoke(this, new HandsFreeStateChangedEventArgs(
            previousState, newState, errorMessage));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Unsubscribe from events
        _audioCapture.OnAudioChunk -= HandleAudioChunk;
        _audioCapture.OnCaptureError -= HandleCaptureError;
        _audioPlayback.OnPlaybackComplete -= HandlePlaybackComplete;

        // Cancel any processing
        _processingCts?.Cancel();
        _processingCts?.Dispose();

        // Complete the channel
        _audioChannel.Writer.TryComplete();

        // Dispose services
        await _speechService.DisposeAsync();
        await _audioCapture.DisposeAsync();
        await _audioPlayback.DisposeAsync();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Simple DTO for chat response.
    /// </summary>
    private class ChatResponseDto
    {
        public Guid? ConversationId { get; set; }
        public string? Content { get; set; }
    }
}
