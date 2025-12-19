using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.HandsFree.Models;
using Aesir.Common.Models;

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
    private readonly IChatApiService _chatApiService;
    private readonly IChatSessionNotifier _chatSessionNotifier;
    private readonly IChatPreferencesService _chatPreferencesService;
    private readonly IAgentToolsService _agentToolsService;
    private readonly IConfigurationApiService _configurationApiService;

    private Channel<byte[]> _audioChannel;
    private CancellationTokenSource? _processingCts;
    private bool _disposed;
    private bool _initialized;
    private int _lastRecordingChunkCount;

    // Minimum chunks required for a valid recording (~500ms at 128ms per chunk)
    // Less than this is likely just an interrupt tap, not an actual recording
    private const int MinimumRecordingChunks = 4;

    // Maintain conversation history for multi-turn conversations
    private AesirConversation? _conversation;

    // Cache current agent for system prompt initialization
    private AesirAgentBase? _currentAgent;

    /// <inheritdoc />
    public HandsFreeState State { get; private set; } = HandsFreeState.Idle;

    /// <inheritdoc />
    public Guid? CurrentAgentId { get; set; }

    /// <inheritdoc />
    public Guid? CurrentConversationId { get; set; }

    /// <inheritdoc />
    public string? CurrentSessionTitle { get; private set; }

    /// <inheritdoc />
    public bool HasExchangedMessages { get; private set; }

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
        IChatApiService chatApiService,
        IChatSessionNotifier chatSessionNotifier,
        IChatPreferencesService chatPreferencesService,
        IAgentToolsService agentToolsService,
        IConfigurationApiService configurationApiService)
    {
        _audioCapture = audioCapture;
        _audioPlayback = audioPlayback;
        _speechService = speechService;
        _chatApiService = chatApiService;
        _chatSessionNotifier = chatSessionNotifier;
        _chatPreferencesService = chatPreferencesService;
        _agentToolsService = agentToolsService;
        _configurationApiService = configurationApiService;

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

            // Pre-initialize microphone to eliminate first-recording latency
            // This requests mic permission and sets up the audio pipeline ahead of time
            var warmedUp = await _audioCapture.WarmUpAsync();
            if (!warmedUp)
            {
                Console.WriteLine("[HandsFree] Warning: Microphone warm-up failed, first recording may have latency");
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

            // Reset the audio channel to ensure fresh state for new recording
            ResetAudioChannel();

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

    /// <inheritdoc />
    public async Task DeactivateAsync()
    {
        if (_disposed)
            return;

        // Reset state first
        await ResetAsync();

        // Clear conversation state for clean slate on next activation
        CurrentAgentId = null;
        CurrentConversationId = null;
        CurrentSessionTitle = null;
        HasExchangedMessages = false;
        _conversation = null;
        _currentAgent = null;

        // Release the microphone and audio resources
        await _audioCapture.ReleaseAsync();

        // Disconnect SignalR hubs
        await _speechService.DisconnectAsync();

        // Reset initialization state so InitializeAsync can be called again
        _initialized = false;

        Console.WriteLine("[HandsFree] Deactivated - audio resources released");
    }

    private async Task ProcessSpeechAsync(CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine("[HandsFree] ProcessSpeechAsync started");

            var transcriptionBuilder = new System.Text.StringBuilder();
            var chunkCount = 0;

            // Stream collected audio chunks to STT service and collect transcription
            Console.WriteLine("[HandsFree] Starting STT streaming...");
            await foreach (var textChunk in _speechService.StreamSpeechToTextAsync(
                GetAudioChunksAsync(cancellationToken), cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                transcriptionBuilder.Append(textChunk);

                // Emit partial transcription updates for real-time feedback
                OnTranscription?.Invoke(this, new TranscriptionEventArgs(
                    transcriptionBuilder.ToString(), false));
            }

            var transcription = transcriptionBuilder.ToString();

            // Handle empty transcription (silence or STT failure)
            if (string.IsNullOrWhiteSpace(transcription))
            {
                // If recording was very short (just an interrupt tap), silently return to Idle
                if (_lastRecordingChunkCount < MinimumRecordingChunks)
                {
                    Console.WriteLine($"[HandsFree] Recording too short ({_lastRecordingChunkCount} chunks), treating as interrupt");
                    SetState(HandsFreeState.Idle);
                    return;
                }

                SetState(HandsFreeState.Error, "No speech detected. Please try again.");
                return;
            }

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

            // Initialize conversation with system prompt if this is a new conversation
            if (_conversation == null)
            {
                _conversation = new AesirConversation
                {
                    Id = CurrentConversationId?.ToString() ?? Guid.NewGuid().ToString(),
                    Messages = new List<AesirChatMessage>()
                };

                // Fetch agent if not already loaded
                if (_currentAgent == null && CurrentAgentId.HasValue)
                {
                    var agentResult = await _configurationApiService.GetAgentAsync(CurrentAgentId.Value, cancellationToken);
                    if (agentResult.IsSuccess && agentResult.Value != null)
                    {
                        _currentAgent = agentResult.Value;
                    }
                }

                // Add system message based on agent's prompt persona
                // This ensures {{currentDateTime}} placeholder is present for server-side rendering
                if (_currentAgent != null)
                {
                    var systemMessage = AesirChatMessage.NewSystemMessage(
                        _currentAgent.ChatPromptPersona,
                        _currentAgent.ChatCustomPromptContent);
                    _conversation.Messages.Insert(0, systemMessage);
                }
            }

            // Add the user message to conversation
            _conversation.Messages.Add(AesirChatMessage.NewUserMessage(userMessage));

            // Get tools and think level from shared preferences (inherits from main chat)
            var disabledToolIds = await _chatPreferencesService.GetDisabledToolIdsAsync(CurrentConversationId);
            var thinkLevel = await _chatPreferencesService.GetThinkLevelAsync(CurrentConversationId);

            // Get agent tools as ToolRequest objects
            var allToolRequests = await _agentToolsService.GetAgentToolRequestsAsync(CurrentAgentId.Value, cancellationToken);
            var agentTools = await _agentToolsService.GetAgentToolsAsync(CurrentAgentId.Value, cancellationToken);

            // Filter tools by disabled IDs and build the enabled tools set
            var enabledTools = new HashSet<ToolRequest>();
            for (var i = 0; i < allToolRequests.Count; i++)
            {
                var toolRequest = allToolRequests[i];
                var agentTool = i < agentTools.Count ? agentTools[i] : null;

                // Only add tool if it's not in the disabled set
                if (agentTool?.Id == null || !disabledToolIds.Contains(agentTool.Id.Value))
                {
                    enabledTools.Add(toolRequest);
                }
            }

            // Determine if thinking should be enabled based on the think level
            var enableThinking = GetEffectiveThinkingEnabled(thinkLevel);

            // Build the chat request
            // TODO: Replace hardcoded user ID with proper user context service from Infrastructure
            var request = new AesirAgentChatRequestBase
            {
                AgentId = CurrentAgentId.Value,
                ChatSessionId = CurrentConversationId,
                ChatSessionUpdatedAt = DateTimeOffset.Now,
                Conversation = _conversation,
                User = "blangford@gmail.com",
                ClientDateTime = DateTime.Now.ToString("F", new CultureInfo("en-US")),
                Title = string.Empty,
                EnableThinking = enableThinking,
                ThinkValue = thinkLevel,
                Tools = enabledTools
            };

            // Send request to chat API
            var result = await _chatApiService.ChatAsync(request, cancellationToken);

            if (!result.IsSuccess || result.Value?.AesirConversation == null)
            {
                Console.Error.WriteLine($"Chat API error: {result.Error}");
                return null;
            }

            // Update conversation from response
            _conversation = result.Value.AesirConversation;

            // Track if this is a new session
            var wasNewSession = !CurrentConversationId.HasValue;

            // Update conversation ID from response
            if (result.Value.ChatSessionId.HasValue)
            {
                CurrentConversationId = result.Value.ChatSessionId;

                // Notify that a new session was created (triggers sidebar refresh)
                if (wasNewSession)
                {
                    _chatSessionNotifier.NotifySessionCreated(CurrentConversationId.Value);
                }
            }

            // Update session title from response (AI-generated on first message)
            if (!string.IsNullOrEmpty(result.Value.Title))
            {
                CurrentSessionTitle = result.Value.Title;
            }

            // Mark that we've had a successful exchange
            HasExchangedMessages = true;

            // Get the last assistant message
            var assistantMessage = _conversation.Messages
                .LastOrDefault(m => m.Role == "assistant");

            return assistantMessage?.Content;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Chat API error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Determines if thinking should be enabled based on the think level.
    /// </summary>
    private static bool? GetEffectiveThinkingEnabled(ThinkValue? thinkLevel)
    {
        if (thinkLevel == null)
        {
            return null; // Let server/agent decide
        }

        // Check if the level indicates thinking should be disabled
        var levelString = thinkLevel.Value.ToString();
        if (string.Equals(levelString, "false", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Any other value (low, medium, high, true) means thinking is enabled
        return true;
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

    /// <summary>
    /// Converts the collected audio chunks from the channel into an async enumerable
    /// for streaming to the STT service.
    /// </summary>
    private async IAsyncEnumerable<byte[]> GetAudioChunksAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[HandsFree] GetAudioChunksAsync started - reading from channel");
        var chunkIndex = 0;

        await foreach (var chunk in _audioChannel.Reader.ReadAllAsync(cancellationToken))
        {
            chunkIndex++;
            Console.WriteLine($"[HandsFree] Yielding chunk {chunkIndex}: {chunk.Length} bytes");
            yield return chunk;
        }

        // Store the chunk count for minimum recording duration check
        _lastRecordingChunkCount = chunkIndex;
        Console.WriteLine($"[HandsFree] GetAudioChunksAsync completed - yielded {chunkIndex} chunks");
    }

    private void HandleAudioChunk(object? sender, AudioChunkEventArgs e)
    {
        Console.WriteLine($"[HandsFree] Received audio chunk: {e.Data.Length} bytes, State: {State}");

        if (State == HandsFreeState.Listening)
        {
            // Queue audio chunk for processing
            var written = _audioChannel.Writer.TryWrite(e.Data);
            Console.WriteLine($"[HandsFree] Wrote to channel: {written}");
        }
        else
        {
            Console.WriteLine($"[HandsFree] Ignoring chunk - not in Listening state");
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
        // Complete any existing channel to signal end of stream
        _audioChannel.Writer.TryComplete();

        // Create a fresh channel for the next recording session
        _audioChannel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
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
}
