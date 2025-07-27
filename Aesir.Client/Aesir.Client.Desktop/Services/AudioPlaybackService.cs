using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aesir.Client.Services;
using MiniAudioEx;

namespace Aesir.Client.Desktop.Services;

public sealed class AudioPlaybackService : IAudioPlaybackService
{
    private readonly AudioSource _source;
    private readonly ConcurrentQueue<AudioClip> _clipQueue = new();
    private readonly Thread _updateThread;
    private bool _running;
    private bool _playing;
    private AudioClip? _currentClip;
    private readonly object _lock = new object();
    private CancellationTokenSource? _cts;

    // Assumptions: All WAV chunks are 16-bit signed PCM, mono, 22050 Hz (based on Sherpa-ONNX VITS English model docs)
    private const uint SampleRate = 22050;
    private const uint Channels = 1;

    public AudioPlaybackService()
    {
        AudioContext.Initialize(SampleRate, Channels);

        _source = new AudioSource();
        _source.End += OnClipEnd;

        _running = true;
        _updateThread = new Thread(UpdateLoop);
        _updateThread.Start();
    }

    private void UpdateLoop()
    {
        while (_running)
        {
            AudioContext.Update();
            Thread.Sleep(10); // Adjust for CPU efficiency; call Update regularly
        }
    }

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

    public void Dispose()
    {
        Stop();
        _running = false;
        _updateThread.Join();
        _source.End -= OnClipEnd; // Unsubscribe event
        AudioContext.Deinitialize(); // Frees all allocated memory
    }
}