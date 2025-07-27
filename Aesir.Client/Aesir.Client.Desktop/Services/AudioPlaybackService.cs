using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aesir.Client.Services;
using MiniAudioEx;

namespace Aesir.Client.Desktop.Services;

/// <summary>
/// A service responsible for audio playback operations, utilizing audio streams for real-time playback and managing audio state.
/// </summary>
public sealed class AudioPlaybackService : IAudioPlaybackService
{
    /// <summary>
    /// Represents the audio source used for managing audio playback operations,
    /// including playing, stopping, and handling end-of-clip events.
    /// This instance is initialized upon creation of the service and is responsible
    /// for directly interfacing with audio playback hardware or software components.
    /// </summary>
    private readonly AudioSource _source;

    /// <summary>
    /// A thread-safe queue that holds audio clips to be played sequentially.
    /// This queue stores instances of <see cref="AudioClip"/> and is managed
    /// within the <see cref="AudioPlaybackService"/> class to handle
    /// audio playback in a controlled order.
    /// </summary>
    private readonly ConcurrentQueue<AudioClip> _clipQueue = new();

    /// <summary>
    /// A thread dedicated to managing and updating the audio context at regular intervals.
    /// This thread runs a loop which continuously updates the audio processing system
    /// to ensure smooth and uninterrupted playback of audio streams.
    /// </summary>
    private readonly Thread _updateThread;

    /// <summary>
    /// Indicates the running state of the audio playback service.
    /// When set to true, the service's update loop continues execution.
    /// When set to false, the update loop stops, and the service terminates its background operations.
    /// </summary>
    private bool _running;

    /// <summary>
    /// Indicates whether audio is currently being played.
    /// When set to true, audio playback is ongoing. When false,
    /// there is no active playback, either because playback has stopped,
    /// no audio is queued, or the current clip has ended.
    /// </summary>
    private bool _playing;

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
    private readonly object _lock = new object();

    /// <summary>
    /// Represents a cancellation token source used to manage the cancellation of the audio playback stream.
    /// This field is initialized when a new playback session is started by the <see cref="PlayStreamAsync(IAsyncEnumerable{byte[]})"/> method
    /// and is reset when playback is stopped via the <see cref="Stop"/> method.
    /// </summary>
    private CancellationTokenSource? _cts;

    // Assumptions: All WAV chunks are 16-bit signed PCM, mono, 22050 Hz (based on Sherpa-ONNX VITS English model docs)
    /// <summary>
    /// Defines the sampling rate (in Hz) used for audio playback.
    /// This determines the number of audio samples per second, critical for audio quality
    /// and compatibility with audio processing components.
    /// The value for this implementation is set to 22050 Hz, aligning with model specifications.
    /// </summary>
    private const uint SampleRate = 22050;

    /// <summary>
    /// Represents the number of audio channels to be used for playback.
    /// Common values include 1 for mono and 2 for stereo.
    /// In this implementation, it is set to 1 (mono), matching the audio format's characteristics.
    /// </summary>
    private const uint Channels = 1;

    /// <summary>
    /// Service for handling audio playback using streaming data and managing the playback lifecycle.
    /// </summary>
    public AudioPlaybackService()
    {
        AudioContext.Initialize(SampleRate, Channels);

        _source = new AudioSource();
        _source.End += OnClipEnd;

        _running = true;
        _updateThread = new Thread(UpdateLoop);
        _updateThread.Start();
    }

    /// <summary>
    /// Continuously updates the audio context at regular intervals to manage audio processing and playback.
    /// </summary>
    /// <remarks>
    /// This method runs in a separate thread and periodically calls the audio context update functionality.
    /// The thread remains active until the service is disposed or explicitly stopped, ensuring consistent
    /// audio playback capabilities.
    /// </remarks>
    private void UpdateLoop()
    {
        while (_running)
        {
            AudioContext.Update();
            Thread.Sleep(10); // Adjust for CPU efficiency; call Update regularly
        }
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
        }

        await foreach (var chunk in audioChunks.WithCancellation(_cts.Token))
        {
            var clip = new AudioClip(chunk); // Use constructor with byte[]; assume isUnique = false by default
            _clipQueue.Enqueue(clip);

            lock (_lock)
            {
                if (!_playing && _clipQueue.TryDequeue(out var firstClip))
                {
                    _currentClip = firstClip;
                    _source.Play(firstClip);
                    _playing = true;
                }
            }
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
            _currentClip?.Dispose(); // Dispose finished clip to free resources
            _currentClip = null;

            if (_clipQueue.TryDequeue(out var nextClip))
            {
                _currentClip = nextClip;
                _source.Play(nextClip);
            }
            else
            {
                _playing = false;
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
            _cts?.Cancel();
            _cts = null;

            _source.Stop();

            _currentClip?.Dispose();
            _currentClip = null;

            while (_clipQueue.TryDequeue(out var clip))
            {
                clip.Dispose(); // Dispose queued clips to free resources
            }

            _playing = false;
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
        Stop();
        _running = false;
        _updateThread.Join();
        _source.End -= OnClipEnd; // Unsubscribe event
        AudioContext.Deinitialize(); // Frees all allocated memory
    }
}