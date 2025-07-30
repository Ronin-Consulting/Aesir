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
/// Responsible for text-to-speech synthesis and speech recognition functionalities by leveraging SignalR for
/// communication with a backend service. Manages the integration between audio playback and recording
/// services, along with handling connection states and data streaming for seamless audio processing.
/// </summary>
public class SpeechService : ISpeechService
{
    /// <summary>
    /// Defines the delay duration, in milliseconds, before reattempting a failed connection
    /// to the text-to-speech service. This value helps manage reconnection intervals to avoid
    /// continuous retries in rapid succession.
    /// </summary>
    private const int RetryDelay = 5000;

    /// <summary>
    /// A logger instance utilized for capturing and recording log messages, warnings, and errors
    /// throughout the execution of the <see cref="SpeechService"/> class. This logger supports
    /// monitoring the application's behavior, diagnosing issues, and tracking important events such
    /// as connection lifecycle states and runtime errors.
    /// </summary>
    private readonly ILogger<SpeechService> _logger;

    /// <summary>
    /// Service responsible for managing audio playback, including streaming and playing back
    /// audio data generated in text-to-speech operations. Utilized to ensure seamless playback
    /// of audio streams during speech synthesis processes.
    /// </summary>
    private readonly IAudioPlaybackService _audioPlaybackService;

    /// <summary>
    /// Represents the audio recording service used for operations such as capturing audio streams for
    /// speech recognition or other audio processing tasks.
    /// This dependency is responsible for managing the audio input lifecycle, including starting
    /// and stopping recordings, and integrating with audio stream consumers.
    /// </summary>
    private readonly IAudioRecordingService _audioRecordingService;

    /// <summary>
    /// Represents the private SignalR hub connection used for interfacing with the
    /// text-to-speech (TTS) service. This connection facilitates the streaming of text inputs
    /// to the server for audio synthesis and receives corresponding audio data for playback
    /// while supporting automatic reconnection on unexpected disconnections.
    /// </summary>
    private readonly HubConnection _ttsConnection;

    /// <summary>
    /// Manages the SignalR connection for the speech-to-text (STT) service.
    /// Used to stream audio data to the backend STT service and receive
    /// text recognition outputs in real-time during speech recognition operations.
    /// </summary>
    private readonly HubConnection _sttConnection;

    /// <summary>
    /// Represents the current connection status to the SignalR hubs used for
    /// text-to-speech (TTS) and speech-to-text (STT) communication.
    /// A value of <c>true</c> indicates that the service is actively connected,
    /// while <c>false</c> means that it is disconnected or in the process of reconnecting.
    /// This variable helps manage the connection state across internal methods and ensures
    /// proper handling of connection-related operations.
    /// </summary>
    private bool _isConnected;

    /// <summary>
    /// Provides text-to-speech and speech recognition functionality through SignalR hub connections.
    /// Handles connection management, including automatic reconnection and retry logic.
    /// Facilitates integration with audio playback and recording services.
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
    /// Establishes an asynchronous connection to the text-to-speech and speech recognition services,
    /// ensuring a reliable connection with automatic retry logic upon failure.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation of establishing the connection.</returns>
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

    /// <summary>
    /// Asynchronously streams recognized speech text from an audio input in real-time.
    /// Establishes a connection with the speech recognition server, processes audio input,
    /// and provides the recognized text as an asynchronous enumerable.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// An asynchronous enumerable that yields recognized text segments as strings.
    /// Each segment represents a portion of the processed speech input.
    /// </returns>
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