using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using Aesir.Client.Services;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Desktop.Services;

/// <summary>
/// Provides functionality for text-to-speech synthesis using SignalR for communication with a backend service.
/// Responsible for establishing and maintaining a connection with the TTS service, as well as streaming generated
/// audio data and playing it back using a specified audio playback service.
/// </summary>
public class SpeechService : ISpeechService
{
    /// <summary>
    /// Specifies the delay, in milliseconds, before retrying a failed connection attempt.
    /// This constant is used to introduce a wait period when handling reconnection logic
    /// to the text-to-speech service.
    /// </summary>
    private const int RetryDelay = 5000;

    /// <summary>
    /// A logger instance used for logging messages, warnings, and errors throughout the
    /// <see cref="SpeechService"/> class. This logger enables effective monitoring and debugging
    /// by capturing application performance, runtime issues, or connection-related events.
    /// </summary>
    private readonly ILogger<SpeechService> _logger;

    /// <summary>
    /// Service responsible for handling audio playback operations,
    /// such as playing audio streams generated during text-to-speech processing.
    /// </summary>
    private readonly IAudioPlaybackService _audioPlaybackService;

    /// <summary>
    /// Service responsible for handling audio recording operations,
    /// such as capturing audio streams for speech recognition processing.
    /// </summary>
    private readonly IAudioRecordingService _audioRecordingService;

    /// <summary>
    /// Represents the SignalR hub connection used to enable communication with the
    /// text-to-speech (TTS) service. This connection is responsible for sending
    /// text input to the server and receiving audio data streams for playback.
    /// The connection automatically attempts to reconnect if closed unexpectedly.
    /// </summary>
    private readonly HubConnection _ttsConnection;

    private readonly HubConnection _sttConnection;
    
    /// <summary>
    /// Indicates the current connection status to the TTS SignalR hub.
    /// A value of <c>true</c> signifies that the service is connected,
    /// while <c>false</c> signifies that it is not connected.
    /// Used internally to manage reconnection logic and ensure
    /// stable communication with the hub.
    /// </summary>
    private bool _isConnected;

    /// <summary>
    /// Provides text-to-speech and speech recognition functionality by interacting with remote services
    /// via a SignalR hub connection. Manages connection state and retries automatically when needed.
    /// </summary>
    public SpeechService(ILogger<SpeechService> logger,
        IConfiguration configuration,
        IAudioPlaybackService audioPlaybackService,
        IAudioRecordingService audioRecordingService)
    {
        _logger = logger;
        _audioPlaybackService = audioPlaybackService;
        _audioRecordingService = audioRecordingService;

        var ttsHubUrl = configuration.GetValue<string>("Inference:Tts");
        _ttsConnection = new HubConnectionBuilder()
            .WithUrl(ttsHubUrl ?? throw new InvalidOperationException(), options =>
            {
                options.HttpMessageHandlerFactory = (handler) =>
                {
                    if (handler is HttpClientHandler clientHandler)
                    {
                        clientHandler.ServerCertificateCustomValidationCallback =
                            (sender, certificate, chain, sslPolicyErrors) => true;
                    }
                    return handler;
                };
            })
            .WithAutomaticReconnect()
            .Build();
        
        _ttsConnection.Closed += async (error) =>
        {
            _isConnected = false;
            _logger.LogWarning("TTS Connection closed: {Error}", error);
            
            await Task.Delay(RetryDelay); // Simple retry delay
            await ConnectAsync();
        };
        
        var sttHubUrl = configuration.GetValue<string>("Inference:Stt");
        _sttConnection = new HubConnectionBuilder()
            .WithUrl(sttHubUrl ?? throw new InvalidOperationException(), options =>
            {
                options.HttpMessageHandlerFactory = (handler) =>
                {
                    if (handler is HttpClientHandler clientHandler)
                    {
                        clientHandler.ServerCertificateCustomValidationCallback =
                            (sender, certificate, chain, sslPolicyErrors) => true;
                    }
                    return handler;
                };
            })
            .WithAutomaticReconnect()
            .Build();
        
        _sttConnection.Closed += async (error) =>
        {
            _isConnected = false;
            _logger.LogWarning("STT Connection closed: {Error}", error);
            
            await Task.Delay(RetryDelay); // Simple retry delay
            await ConnectAsync();
        };
    }

    /// <summary>
    /// Establishes an asynchronous connection to the text-to-speech service Hub,
    /// ensuring a reliable connection with automatic retry logic on failure.
    /// </summary>
    /// <returns>A task that represents the asynchronous connection operation.</returns>
    private async Task ConnectAsync()
    {
        if (_isConnected) return;
        try
        {
            await _ttsConnection.StartAsync();
            await _sttConnection.StartAsync();
            _isConnected = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("TTS Connection closed: {Error}", ex);
            
            await Task.Delay(RetryDelay);
            await ConnectAsync(); // Retry
        }
    }

    /// <summary>
    /// Converts the provided text to speech and plays the audio asynchronously.
    /// </summary>
    /// <param name="text">The text to be converted into speech and played.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SpeakAsync(string text)
    {
        await ConnectAsync();

        var channelReader = await _ttsConnection.StreamAsChannelAsync<byte[]>("GenerateAudio", text, 0.9f);
        await _audioPlaybackService.PlayStreamAsync(channelReader.ReadAllAsync());
    }

    public async IAsyncEnumerable<string> RecognizeSpeechAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await ConnectAsync();

        // Start the audio recording stream
        var audioStream = _audioRecordingService.StartRecordingAsync(cancellationToken);
        
        // Start streaming audio to the server and receive text recognition results
        var textChannelReader = await _sttConnection.StreamAsChannelAsync<string>("ProcessAudioStream", 
            audioStream, cancellationToken);
        
        // Process the recognition results
        while (await textChannelReader.WaitToReadAsync(cancellationToken))
        {
            while (textChannelReader.TryRead(out var recognizedText))
            {
                if (!string.IsNullOrEmpty(recognizedText))
                {
                    yield return recognizedText;
                }
            }
        }
        
        // Ensure recording is stopped when done
        _audioRecordingService.StopRecording();
    }
}