using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace Aesir.Client.Web.Modules.HandsFree.Services;

/// <summary>
/// SignalR-based service for speech-to-text and text-to-speech hub connections.
/// </summary>
public class SignalRSpeechService : ISignalRSpeechService
{
    private readonly IConfiguration _configuration;
    private HubConnection? _sttHub;
    private HubConnection? _ttsHub;
    private bool _disposed;

    /// <inheritdoc />
    public bool IsConnected =>
        _sttHub?.State == HubConnectionState.Connected &&
        _ttsHub?.State == HubConnectionState.Connected;

    /// <summary>
    /// Creates a new SignalRSpeechService.
    /// </summary>
    public SignalRSpeechService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SignalRSpeechService));

        if (IsConnected)
            return true;

        try
        {
            var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://aesir.localhost";

            // Create STT hub connection
            _sttHub = new HubConnectionBuilder()
                .WithUrl($"{baseUrl}/stthub")
                .WithAutomaticReconnect()
                .Build();

            // Create TTS hub connection
            _ttsHub = new HubConnectionBuilder()
                .WithUrl($"{baseUrl}/ttshub")
                .WithAutomaticReconnect()
                .Build();

            // Connect both hubs in parallel
            await Task.WhenAll(
                _sttHub.StartAsync(cancellationToken),
                _ttsHub.StartAsync(cancellationToken)
            );

            Console.WriteLine("Connected to STT and TTS hubs");
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to connect to speech hubs: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        var tasks = new List<Task>();

        if (_sttHub != null)
        {
            tasks.Add(_sttHub.StopAsync());
        }

        if (_ttsHub != null)
        {
            tasks.Add(_ttsHub.StopAsync());
        }

        await Task.WhenAll(tasks);
        Console.WriteLine("Disconnected from speech hubs");
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> StreamSpeechToTextAsync(
        IAsyncEnumerable<byte[]> audioChunks,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SignalRSpeechService));

        if (_sttHub?.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("STT hub is not connected. Call ConnectAsync first.");
        }

        // Stream audio to the STT hub and receive transcribed text
        // Using ProcessOpusStream for Opus-encoded audio (more efficient)
        var textStream = _sttHub.StreamAsync<string>(
            "ProcessOpusStream",
            audioChunks,
            cancellationToken);

        await foreach (var text in textStream.WithCancellation(cancellationToken))
        {
            yield return text;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<byte[]> StreamTextToSpeechAsync(
        string text,
        float speed = 1.0f,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SignalRSpeechService));

        if (_ttsHub?.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("TTS hub is not connected. Call ConnectAsync first.");
        }

        // Stream text to the TTS hub and receive audio chunks (WAV format)
        var audioStream = _ttsHub.StreamAsync<byte[]>(
            "GenerateAudio",
            text,
            speed,
            cancellationToken);

        await foreach (var audioChunk in audioStream.WithCancellation(cancellationToken))
        {
            yield return audioChunk;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<byte[]> StreamTextToSpeechOpusAsync(
        string text,
        float speed = 1.0f,
        int bitrate = 32000,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SignalRSpeechService));

        if (_ttsHub?.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("TTS hub is not connected. Call ConnectAsync first.");
        }

        // Stream text to the TTS hub and receive Opus-encoded audio frames
        var audioStream = _ttsHub.StreamAsync<byte[]>(
            "GenerateOpusAudio",
            text,
            speed,
            bitrate,
            cancellationToken);

        await foreach (var audioChunk in audioStream.WithCancellation(cancellationToken))
        {
            yield return audioChunk;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        await DisconnectAsync();

        if (_sttHub != null)
        {
            await _sttHub.DisposeAsync();
            _sttHub = null;
        }

        if (_ttsHub != null)
        {
            await _ttsHub.DisposeAsync();
            _ttsHub = null;
        }

        GC.SuppressFinalize(this);
    }
}
