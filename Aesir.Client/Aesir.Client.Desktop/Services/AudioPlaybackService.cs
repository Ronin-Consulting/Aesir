using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aesir.Client.Services;
using Microsoft.Extensions.Logging;
using MiniAudioEx;

namespace Aesir.Client.Desktop.Services;

/// <summary>
/// Configuration class for audio playback service settings, allowing for easy tuning and overrides.
/// </summary>
public class AudioPlaybackConfig
{
    public static AudioPlaybackConfig Default => new();
    
    /// <summary>
    /// Defines the sampling rate (in Hz) used for audio playback.
    /// This determines the number of audio samples per second, critical for audio quality
    /// and compatibility with audio processing components.
    /// </summary>
    public uint SampleRate { get; set; } = 22050;
    
    /// <summary>
    /// Represents the number of audio channels to be used for playback.
    /// Common values include 1 for mono and 2 for stereo.
    /// </summary>
    public uint Channels { get; set; } = 1;
    
    /// <summary>
    /// Buffer size for audio chunk validation (bytes per sample for 16-bit PCM).
    /// </summary>
    public int ValidationBufferSize { get; set; } = 2;
}

/// <summary>
/// A service responsible for audio playback operations, utilizing audio streams for real-time playback and managing audio state.
/// </summary>
public sealed class AudioPlaybackService(
    ILogger<AudioPlaybackService> logger,
    AudioPlaybackConfig? config = null) : IAudioPlaybackService
{
    private readonly ILogger<AudioPlaybackService> _logger = logger;
    private readonly AudioPlaybackConfig _config = config ?? AudioPlaybackConfig.Default;

    /// <summary>
    /// Represents the audio source used for managing audio playback operations,
    /// including playing, stopping, and handling end-of-clip events.
    /// This instance is initialized upon creation of the service and is responsible
    /// for directly interfacing with audio playback hardware or software components.
    /// </summary>
    private AudioSource? _source;

    /// <summary>
    /// A thread-safe queue that holds audio clips to be played sequentially.
    /// This queue stores instances of <see cref="AudioClip"/> and is managed
    /// within the <see cref="AudioPlaybackService"/> class to handle
    /// audio playback in a controlled order.
    /// </summary>
    private readonly ConcurrentQueue<AudioClip> _clipQueue = new();
    
    /// <summary>
    /// Indicates whether audio is currently being played.
    /// When set to true, audio playback is ongoing. When false,
    /// there is no active playback, either because playback has stopped,
    /// no audio is queued, or the current clip has ended.
    /// </summary>
    public bool IsPlaying { get; private set; }

    /// <summary>
    /// Represents the currently active audio clip being played by the audio playback service.
    /// This variable holds a reference to the audio clip currently being processed or played.
    /// If no audio is being played, the value will be null.
    /// </summary>
    /// <remarks>
    /// The _currentClip is updated when a new audio clip begins playback
    /// and is set to null when playback stops or the clip is disposed.
    /// Proper locking is used to ensure thread safety during modifications.
    /// </remarks>
    private AudioClip? _currentClip;

    /// <summary>
    /// Provides a synchronization lock used to ensure thread-safe operations
    /// on shared resources within the AudioPlaybackService, such as playback state
    /// and the audio clip queue.
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// Represents a cancellation token source used to manage the cancellation of the audio playback stream.
    /// This field is initialized when a new playback session is started by the <see cref="PlayStreamAsync(IAsyncEnumerable{byte[]})"/> method
    /// and is reset when playback is stopped via the <see cref="Stop"/> method.
    /// </summary>
    private CancellationTokenSource? _cts;

    // Assumptions: All WAV chunks are 16-bit signed PCM, configurable sample rate and channels
    /// <summary>
    /// Gets the sampling rate (in Hz) used for audio playback from configuration.
    /// </summary>
    private uint SampleRate => _config.SampleRate;

    /// <summary>
    /// Gets the number of audio channels to be used for playback from configuration.
    /// </summary>
    private uint Channels => _config.Channels;

    private AudioApp? _audioApp;
    
    /// <summary>
    /// Initializes the audio playback service components.
    /// </summary>
    private void Initialize()
    {
        try
        {
            _logger.LogInformation("Audio context initialized with sample rate {SampleRate} Hz and {Channels} channel(s)", SampleRate, Channels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize audio context");
            throw;
        }

        if(_audioApp != null) return; // Already initialized
        
        _audioApp = new AudioApp(SampleRate, Channels);
        _audioApp.Loaded += AudioContextLoaded;
        
        Task.Run(() => _audioApp.Run(),_cts!.Token);
        
        _logger.LogInformation("AudioPlaybackService initialized successfully");
    }

    private void AudioContextLoaded()
    {
        _source = new AudioSource();
        _source.End += OnClipEnd;        
    }

    /// <summary>
    /// Plays audio from a provided asynchronous stream of audio chunks.
    /// </summary>
    /// <param name="audioChunks">An asynchronous enumerable of byte arrays representing chunks of audio data.</param>
    /// <returns>A task representing the asynchronous operation of audio playback.</returns>
    public async Task PlayStreamAsync(IAsyncEnumerable<byte[]> audioChunks)
    {
        lock (_lock)
        {
            Stop(); // Stop any ongoing playback
            _cts = new CancellationTokenSource();
            
            Initialize();
        }
        
        _logger.LogInformation("Starting audio stream playback");
        
        try
        {
            var chunkCount = 0;
            await foreach (var chunk in audioChunks.WithCancellation(_cts.Token))
            {
                ValidateChunk(chunk);
                
                var clip = new AudioClip(chunk); // Use constructor with byte[]; assume isUnique = false by default
                _clipQueue.Enqueue(clip);
                chunkCount++;
                
                lock (_lock)
                {
                    if (!IsPlaying && _clipQueue.TryDequeue(out var firstClip))
                    {
                        _currentClip = firstClip;
                        _source!.Play(firstClip);
                        IsPlaying = true;
                        _logger.LogDebug("Started playing first audio clip");
                    }
                }
            }
            
            _logger.LogInformation("Audio stream playback completed. Processed {ChunkCount} chunks", chunkCount);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Audio stream playback was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during audio stream playback");
        }
    }

    private void ValidateChunk(byte[] chunk)
    {
        // Example: Check if chunk size is consistent with 16-bit PCM
        if (chunk.Length % _config.ValidationBufferSize != 0)
        {
            _logger.LogWarning("Invalid chunk size: {Length} bytes, not aligned to 16-bit PCM", chunk.Length);
        }
    }
    
    /// <summary>
    /// Handles the event triggered when an audio clip ends playback.
    /// This method releases resources associated with the completed clip, updates the playback state,
    /// and starts playback of the next queued clip if available. If no clips remain in the queue,
    /// playback is set to an inactive state.
    /// </summary>
    private void OnClipEnd()
    {
        lock (_lock)
        {
            // Wait for audio source to confirm buffer is no longer in use
            while (_source!.IsPlaying)
            {
                Thread.Sleep(1);
            }
            
            _currentClip?.Dispose(); // Dispose finished clip to free resources
            _currentClip = null;

            if (_clipQueue.TryDequeue(out var nextClip))
            {
                _currentClip = nextClip;
                _source.Play(nextClip);
                _logger.LogDebug("Playing next audio clip from queue");
            }
            else
            {
                IsPlaying = false;
                _logger.LogDebug("Audio playback queue is empty, playback stopped");
            }
        }
    }

    /// <summary>
    /// Stops the audio playback and releases any resources associated with the current playback session.
    /// This method stops the current audio playback, cancels any ongoing audio stream operations,
    /// clears the playback queue, and disposes of any active audio clips to free resources.
    /// It ensures that all playback-related activities are halted, resetting the playback state.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            if (!IsPlaying && _cts == null)
            {
                return; // Already stopped
            }
            
            _logger.LogInformation("Stopping audio playback");
            
            _cts?.Cancel();
            _cts = null;

            if (_source != null) _source.Stop();

            _currentClip?.Dispose();
            _currentClip = null;
            
            var disposedClips = 0;
            while (_clipQueue.TryDequeue(out var clip))
            {
                clip.Dispose(); // Dispose queued clips to free resources
                disposedClips++;
            }

            if (_source != null) _source.End -= OnClipEnd; // Unsubscribe event
            
            _audioApp!.Loaded -= AudioContextLoaded;

            _source = null;
            _audioApp = null;
            
            IsPlaying = false;
            
            if (disposedClips > 0)
            {
                _logger.LogDebug("Disposed {DisposedClips} queued audio clips", disposedClips);
            }
            
            _logger.LogInformation("Audio playback stopped successfully");
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="AudioPlaybackService"/> instance.
    /// </summary>
    /// <remarks>
    /// This method stops any ongoing audio playback, deinitializes the audio context,
    /// disposes of any queued audio clips, and terminates the update thread.
    /// It ensures that all unmanaged resources and allocated memory are properly freed.
    /// </remarks>
    public void Dispose()
    {
        _logger.LogInformation("Disposing AudioPlaybackService");
        
        Stop();
        
        try
        {
            AudioContext.Deinitialize(); // Frees all allocated memory
            _logger.LogDebug("Audio context deinitialized");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error deinitializing audio context during disposal");
        }
        
        _logger.LogInformation("AudioPlaybackService disposed successfully");
    }
}