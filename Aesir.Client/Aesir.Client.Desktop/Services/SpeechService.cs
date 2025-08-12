using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Aesir.Client.Services;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Desktop.Services;

/// <summary>
/// Provides services for handling text-to-speech (TTS) and speech-to-text (STT) operations.
/// Enables audio playback, recording, and processing through SignalR integration.
/// Implements asynchronous operations for speaking, listening, and disposing resources.
/// </summary>
public class SpeechService : ISpeechService, IAsyncDisposable
{
    /// <summary>
    /// Provides logging capabilities for the <see cref="SpeechService"/> class.
    /// It is used to record events, warnings, errors, and other diagnostic information
    /// to aid in development and troubleshooting.
    /// </summary>
    private readonly ILogger<SpeechService> _logger;

    /// <summary>
    /// An instance of <see cref="IAudioPlaybackService"/> used for handling audio playback-related functionalities,
    /// including streaming audio data, managing playback states, and stopping playback when required.
    /// </summary>
    private readonly IAudioPlaybackService _audioPlaybackService;

    /// <summary>
    /// An instance of <see cref="IAudioRecordingService"/> that provides audio capture functionalities,
    /// enabling the SpeechService to handle tasks such as starting and stopping recording,
    /// and detecting silence events during audio processing.
    /// </summary>
    private readonly IAudioRecordingService _audioRecordingService;

    /// <summary>
    /// Represents a private SignalR hub connection dedicated for Text-to-Speech (TTS) operations
    /// within the <see cref="SpeechService"/> class. This connection is responsible for
    /// communicating with the remote TTS service to generate audio streams from text input.
    /// It also includes support for automatic reconnection and gracefully handling connection closures.
    /// </summary>
    private readonly HubConnection _ttsConnection;

    /// <summary>
    /// A SignalR hub connection used for managing real-time speech-to-text (STT) operations
    /// in the <see cref="SpeechService"/> class.
    /// </summary>
    /// <remarks>
    /// This connection streams audio data to the server and asynchronously receives
    /// transcribed text data for speech recognition.
    /// </remarks>
    /// <seealso cref="Microsoft.AspNetCore.SignalR.Client.HubConnection"/>
    private readonly HubConnection _sttConnection;

    /// <summary>
    /// A <see cref="CancellationTokenSource"/> used to signal cancellation of
    /// ongoing speech-to-text listening operations in the <see cref="SpeechService"/> class.
    /// </summary>
    private CancellationTokenSource? _stopListeningCts;

    /// <summary>
    /// Provides speech processing capabilities, including text-to-speech (TTS) and speech-to-text (STT) operations.
    /// This service utilizes SignalR connections to communicate with inference services for TTS and STT functionalities.
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
    /// Asynchronously synthesizes the provided text into speech audio and plays it back.
    /// </summary>
    /// <param name="text">The text content to be converted into speech.</param>
    /// <returns>A task that represents the asynchronous speech synthesis and playback operation.</returns>
    public async Task SpeakAsync(string text)
    {
        if (_ttsConnection.State == HubConnectionState.Disconnected)
            await _ttsConnection.StartAsync();

        var channelReader = await _ttsConnection.StreamAsChannelAsync<byte[]>("GenerateAudio", text, 0.9f);
        await _audioPlaybackService.PlayStreamAsync(channelReader.ReadAllAsync());
    }

    /// <summary>
    /// Stops any ongoing text-to-speech operation by halting audio playback
    /// and disconnecting from the speech service if a connection is established.
    /// </summary>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    public async Task StopSpeakingAsync()
    {
        if (_audioPlaybackService.IsPlaying)
            _audioPlaybackService.Stop();

        if (_ttsConnection.State == HubConnectionState.Connected)
            await _ttsConnection.StopAsync();
    }

    /// <summary>
    /// Asynchronously listens for speech input, processes it via a speech-to-text (STT) service,
    /// and returns a list of recognized utterances. Supports pause control based on detected silence.
    /// </summary>
    /// <param name="shouldPauseOnSilence">
    /// A function that determines whether to pause listening when a period of silence is detected.
    /// The function receives the silence duration in milliseconds as input and returns a boolean indicating
    /// whether listening should pause.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a list of strings representing
    /// the recognized utterances from the speech input.
    /// </returns>
    public async Task<IList<string>> ListenAsync(Func<int, bool>? shouldPauseOnSilence)
    {
        _stopListeningCts = new CancellationTokenSource();
        var localCancelToken = _stopListeningCts.Token;
        
        if (_sttConnection.State == HubConnectionState.Disconnected)
            await _sttConnection.StartAsync(localCancelToken);
        
        var utterances = new List<string>();
        
        _audioRecordingService.SilenceDetected += OnSilenceDetected;
        
        var audioChunkStream = _audioRecordingService.StartRecordingAsync(_stopListeningCts.Token);
        await foreach (var chunk in _sttConnection.StreamAsync<string>("ProcessAudioStream", audioChunkStream).WithCancellation(localCancelToken))
        {
            utterances.Add(chunk);
        }
        
        _audioRecordingService.SilenceDetected -= OnSilenceDetected;
        
        return utterances;

        void OnSilenceDetected(object? sender, SilenceDetectedEventArgs e)
        {
            if (shouldPauseOnSilence is not null && shouldPauseOnSilence(e.SilenceDurationMs))
            {
                _logger.LogInformation("Silence detected, pausing listening");
                _audioRecordingService.StopAsync();
            }
        }
    }

    /// <summary>
    /// Stops the speech-to-text listening process and releases associated resources.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation of stopping the listening process.</returns>
    public async Task StopListeningAsync()
    {
        if (_stopListeningCts is { IsCancellationRequested: false })
            await _stopListeningCts.CancelAsync();

        if (_audioRecordingService.IsRecording)
            await _audioRecordingService.StopAsync();

        if (_sttConnection.State == HubConnectionState.Connected)
            await _sttConnection.StopAsync();
    }

    /// <summary>
    /// Asynchronously releases the resources used by the SpeechService, including managing the disposal
    /// of the connections to the TTS and STT SignalR hubs.
    /// </summary>
    /// <returns>A ValueTask representing the completion of the asynchronous resource disposal operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await _ttsConnection.DisposeAsync();
        await _sttConnection.DisposeAsync();
    }
}