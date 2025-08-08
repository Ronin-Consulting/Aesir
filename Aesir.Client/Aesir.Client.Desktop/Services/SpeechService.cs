using System;
using System.Collections.Generic;
using System.Net.Http;
using Aesir.Client.Services;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Desktop.Services;

/// <summary>
/// Provides text-to-speech (TTS) and speech-to-text (STT) functionality using SignalR connections.
/// It handles the generation of audio streams, playback, and speech recording for processing.
/// </summary>
public class SpeechService : ISpeechService, IAsyncDisposable
{
    /// <summary>
    /// An instance of <see cref="ILogger{TCategoryName}"/> used for logging events, warnings, errors,
    /// and diagnostic information in the <see cref="SpeechService"/> class.
    /// </summary>
    private readonly ILogger<SpeechService> _logger;

    /// <summary>
    /// Represents the audio playback service used for handling tasks related to audio playback
    /// such as streaming audio data and controlling playback state.
    /// </summary>
    private readonly IAudioPlaybackService _audioPlaybackService;

    /// <summary>
    /// Provides the necessary audio recording functionalities to the SpeechService,
    /// including starting and stopping audio capture, and detecting silence events
    /// during recording operations.
    /// </summary>
    private readonly IAudioRecordingService _audioRecordingService;

    /// <summary>
    /// Represents the SignalR hub connection used for Text-to-Speech (TTS) functionality.
    /// This connection enables communication with a remote service to generate audio
    /// streams from textual input and handle automatic reconnection or closed states.
    /// </summary>
    private readonly HubConnection _ttsConnection;

    /// <summary>
    /// Represents a SignalR hub connection used for speech-to-text (STT) operations within the SpeechService.
    /// </summary>
    /// <remarks>
    /// This hub connection facilitates audio streaming to the server for real-time speech recognition and
    /// handles the asynchronous reception of recognized text data.
    /// </remarks>
    /// <seealso cref="Microsoft.AspNetCore.SignalR.Client.HubConnection"/>
    private readonly HubConnection _sttConnection;

    /// <summary>
    /// Provides speech-related services including text-to-speech (TTS) and speech-to-text (STT) functionalities.
    /// This service leverages SignalR hubs for interaction with corresponding inference endpoints.
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

        _ttsConnection.Closed += (error) =>
        {
            _logger.LogWarning("TTS Connection closed: {Result}", error?.ToString() ?? "Gracefully");
            return Task.CompletedTask;
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

        _sttConnection.Closed += (error) =>
        {
            _logger.LogWarning("TTS Connection closed: {Result}", error?.ToString() ?? "Gracefully");
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Asynchronously converts the provided text into speech audio using a text-to-speech service and plays it back.
    /// </summary>
    /// <param name="text">The text to be synthesized into speech.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SpeakAsync(string text)
    {
        if (_ttsConnection.State == HubConnectionState.Disconnected)
            await _ttsConnection.StartAsync();

        var channelReader = await _ttsConnection.StreamAsChannelAsync<byte[]>("GenerateAudio", text, 0.9f);
        await _audioPlaybackService.PlayStreamAsync(channelReader.ReadAllAsync());
    }

    /// <summary>
    /// Stops the active text-to-speech task if a connection to the speech service is currently active.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StopSpeaking()
    {
        if (_ttsConnection.State == HubConnectionState.Connected)
            await _ttsConnection.StopAsync();
    }

    /// <summary>
    /// Starts streaming audio to the server for speech-to-text processing and yields recognized text results asynchronously.
    /// </summary>
    /// <param name="silenceDetectedAction">
    /// An optional callback action invoked when silence is detected, receiving the duration of silence in milliseconds.
    /// </param>
    /// <returns>
    /// An asynchronous stream of recognized text segments as they are processed.
    /// </returns>
    public async IAsyncEnumerable<string> ListenAsync(Action<int>? silenceDetectedAction = null)
    {
        if (_sttConnection.State == HubConnectionState.Disconnected)
            await _sttConnection.StartAsync();

        // Start the audio recording stream
        var audioStream = _audioRecordingService.StartRecordingAsync();

        _audioRecordingService.SilenceDetected += OnHandleSilenceDetected;

        // Start streaming audio to the server and receive text recognition results
        var textChannelReader = await _sttConnection.StreamAsChannelAsync<string>("ProcessAudioStream",
            audioStream);

        await foreach (var utterance in textChannelReader.ReadAllAsync())
        {
            _logger.LogDebug("Listening and heard: {Utterance}", utterance);
            if (!string.IsNullOrEmpty(utterance))
            {
                yield return utterance;
            }
        }

        // Ensure recording is stopped when done
        _audioRecordingService.StopRecording();
        _audioRecordingService.SilenceDetected -= OnHandleSilenceDetected;
        yield break;

        void OnHandleSilenceDetected(object? sender, SilenceDetectedEventArgs args)
        {
            silenceDetectedAction?.Invoke(args.SilenceDurationMs);
        }
    }

    /// <summary>
    /// Stops the speech-to-text listening operation.
    /// </summary>
    /// <remarks>
    /// This method halts the ongoing audio recording process if it is active
    /// and disconnects the speech-to-text service connection if it is currently connected.
    /// It ensures that all associated resources or operations related to listening
    /// are properly stopped.
    /// </remarks>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    public async Task StopListening()
    {
        if (_audioRecordingService.IsRecording)
            _audioRecordingService.StopRecording();

        if (_sttConnection.State == HubConnectionState.Connected)
            await _sttConnection.StopAsync();
    }

    /// <summary>
    /// Asynchronously disposes the resources used by the SpeechService, including the TTS and STT hub connections.
    /// </summary>
    /// <returns>A ValueTask representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await _ttsConnection.DisposeAsync();
        await _sttConnection.DisposeAsync();
    }
}