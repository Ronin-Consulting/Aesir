using System.Text;
using Aesir.Client.Desktop.Services;
using Aesir.Client.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace Aesir.Client.Desktop.Tests;

/// <summary>
/// Contains unit tests for the audio-related services including recording, speech recognition,
/// and playback functionality utilized in the desktop client application.
/// </summary>
[TestClass]
public sealed class AudioTests
{
    /// <summary>
    /// Tests the Start and Stop functionality of the <see cref="AudioRecordingService"/>.
    /// Verifies that the service can start recording and stop recording gracefully.
    /// Additionally, ensures that the service responds correctly to cancellation tokens and
    /// handles task cancellation as expected.
    /// </summary>
    /// <returns>Asynchronous task representing the method execution.</returns>
    [TestMethod]
    public async Task Test_AudioRecordingService_StopAndStart()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        var logger = loggerFactory.CreateLogger<AudioRecordingService>();
        using var service = new AudioRecordingService(logger);

        service.IsRecording.Should().BeFalse();

        // Act & Assert: Test Start and Stop functionality
        var recordingStream = service.StartRecordingAsync();

        var consumptionTask = Task.Run(async () =>
        {
            // We must start consuming the enumerable for the recording to actually start
            await foreach (var _ in recordingStream)
            {
                // This loop will exit when StopRecording is called
            }
        });

        // A small delay to allow the async logic to execute
        await Task.Delay(50);
        service.IsRecording.Should().BeTrue("because StartRecordingAsync was called and is being consumed");

        service.StopAsync();

        // The consumption task should complete gracefully
        await consumptionTask.WaitAsync(TimeSpan.FromSeconds(1));
        service.IsRecording.Should().BeFalse("because StopRecording was called");

        // Act & Assert: Test Cancellation functionality
        var cts = new CancellationTokenSource();
        var secondRecordingStream = service.StartRecordingAsync(cts.Token);

        consumptionTask = Task.Run(async () =>
        {
            await foreach (var _ in secondRecordingStream.WithCancellation(cts.Token))
            {
                // This loop will exit when the token is canceled
            }
        }, cts.Token);

        await Task.Delay(50);
        service.IsRecording.Should().BeTrue("because a new recording was started");

        cts.Cancel();

        // Assert that the task was canceled as expected
        await FluentActions.Awaiting(() => consumptionTask)
            .Should().ThrowAsync<OperationCanceledException>();

        service.IsRecording.Should().BeFalse("because the cancellation token was signaled");
    }

    /// Tests the functionality of saving recorded audio to a WAV file in the AudioRecordingService.
    /// It verifies the entire process of recording, detecting silence, saving to a WAV file,
    /// and validating that the file is properly created and readable as a valid WAV file.
    /// <returns>
    /// Asserts that the WAV file is successfully created with valid audio data and
    /// ensures the file can be read without errors. Also verifies the detection of silence during recording.
    /// </returns>
    [TestMethod]
    public async Task Test_AudioRecordingService_SaveRecordingToWavFile()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        var logger = loggerFactory.CreateLogger<AudioRecordingService>();
        using var service = new AudioRecordingService(logger);

        var outputPath = Path.Combine(Path.GetTempPath(), $"test_recording_{Guid.NewGuid()}.wav");

        var waveFormat = new WaveFormat(16000, 16, 1);

        try
        {
            // Act

            var silenceUnits = new List<int>();

            service.SilenceDetected += (sender, args) => silenceUnits.Add(args.SilenceDurationMs);

            var recordingStream = service.StartRecordingAsync();

            var stopTask = Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(_ => service.StopAsync());

            using var memoryStream = new MemoryStream();
            await foreach (var buffer in recordingStream)
            {
                await memoryStream.WriteAsync(buffer);
            }

            await stopTask; // Ensure stop has been called

            // Write the collected raw PCM data to a WAV file.
            memoryStream.Position = 0;
            await using (var waveFileWriter = new WaveFileWriter(outputPath, waveFormat))
            {
                await memoryStream.CopyToAsync(waveFileWriter);
            }

            // Assert

            silenceUnits.Should().HaveCountGreaterThan(0);

            File.Exists(outputPath).Should().BeTrue("a .wav file should be created at the specified path.");

            var fileInfo = new FileInfo(outputPath);
            fileInfo.Length.Should().BeGreaterThan(44,
                "the file should contain audio data beyond the standard 44-byte WAV header.");

            // Further validation: ensure the file can be read by a WAV file reader.
            FluentActions.Invoking(() =>
            {
                using var reader = new WaveFileReader(outputPath);
                reader.TotalTime.Should().BeCloseTo(TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(250));
            }).Should().NotThrow("the created file should be a valid, readable WAV file.");
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                //File.Delete(outputPath);
            }
        }
    }

    /// <summary>
    /// Tests the <see cref="SpeechService.ListenAsync"/> method for recognizing speech asynchronously.
    /// This method sets up the necessary configuration, logging, and mocking for audio recording
    /// and playback services to ensure proper functionality of the speech recognition process.
    /// 
    /// This is an integration test.
    /// 
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous test operation. The test validates that speech
    /// recognition results are successfully processed and logged.
    /// </returns>
    [TestMethod]
    public async Task Test_SpeechService_RecognizeSpeechAsync()
    {
        // Arrange
        // "Tts": "https://aesir.localhost/ttshub",
        // "Stt": "https://aesir.localhost/stthub",
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([
                new KeyValuePair<string, string?>("Inference:Tts", "https://aesir.localhost/ttshub"),
                new KeyValuePair<string, string?>("Inference:Stt", "https://aesir.localhost/stthub")
            ])
            .AddEnvironmentVariables()
            .Build();

        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Debug)
        );
        var audioRecordingLogger = loggerFactory.CreateLogger<MockAudioRecordingService>();
        var audioPlaybackLogger = loggerFactory.CreateLogger<AudioPlaybackService>();
        var speechLogger = loggerFactory.CreateLogger<SpeechService>();
        var testlogger = loggerFactory.CreateLogger<AudioTests>();

        using var audioRecordingService = new MockAudioRecordingService(audioRecordingLogger);
        using var audioPlaybackService = new AudioPlaybackService(audioPlaybackLogger);

        await using var speechService =
            new SpeechService(speechLogger, configuration, audioPlaybackService, audioRecordingService);
        
        // Act
        var recognizedText = new StringBuilder();
        foreach (var recognized in await speechService.ListenAsync(OnHandleSilenceDetected))
        {
            recognizedText.Append(recognized);
        }
        testlogger.LogInformation("Recognized: {RecognizedText}", recognizedText);
        
        return;
        
        bool OnHandleSilenceDetected(int silenceDurationMs)
        {
            testlogger.LogInformation("Silence Heard: {SilenceDurationMs}", silenceDurationMs);

            return false;
        }
    }

    /// <summary>
    /// A mock implementation of the <see cref="IAudioRecordingService"/> interface.
    /// Provides functionality for simulating audio recording with mock data for testing purposes.
    /// </summary>
    public class MockAudioRecordingService(ILogger<MockAudioRecordingService> logger) : IAudioRecordingService
    {
        /// <summary>
        /// A public event triggered when a period of silence is detected during an audio recording session.
        /// </summary>
        /// <remarks>
        /// This event is part of the <see cref="IAudioRecordingService"/> interface. It can be used to notify consumers
        /// when silence has been detected in the audio stream being recorded. The event provides
        /// <see cref="SilenceDetectedEventArgs"/> as its argument to carry additional details about the detected silence.
        /// </remarks>
        public event EventHandler<SilenceDetectedEventArgs>? SilenceDetected;

        /// <summary>
        /// Releases the resources used by the object.
        /// This method is invoked to perform application-defined tasks associated with freeing, releasing,
        /// or resetting unmanaged and optionally managed resources.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Starts recording audio asynchronously and provides audio data chunks as an asynchronous stream.
        /// </summary>
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests.
        /// </param>
        /// <returns>
        /// An asynchronous enumerable sequence containing chunks of audio data as byte arrays.
        /// </returns>
        public async IAsyncEnumerable<byte[]> StartRecordingAsync(CancellationToken cancellationToken = default)
        {
            const string wavFileName = "kennedy.wav";

            await using var waveReader = new WaveFileReader(wavFileName);
            var sampleProvider = waveReader.ToSampleProvider().ToMono().ToWaveProvider16();
            var waveFormat = sampleProvider.WaveFormat;
            logger.LogDebug("Sample Rate: {SampleRate}, Channels: {Channels}, Bits Per Sample: {BitsPerSample}",
                waveFormat.SampleRate,
                waveFormat.Channels,
                waveFormat.BitsPerSample);

            const int samplesPerChunk = 11200; // Your desired chunk size
            const int bytesPerSample = 2; // 16-bit = 2 bytes per sample
            const int bufferSize = samplesPerChunk * bytesPerSample;

            var buffer = new byte[bufferSize];
            var chunkIndex = 0;

            // Read and process chunks
            while (true)
            {
                var bytesRead = sampleProvider.Read(buffer, 0, bufferSize);

                if (bytesRead == 0)
                    break; // End of file

                // Create a chunk with the actual bytes read
                var chunk = new byte[bytesRead];
                Array.Copy(buffer, 0, chunk, 0, bytesRead);

                logger.LogDebug("Chunk {ChunkIndex}: {BytesRead} bytes ({Samples} samples)",
                    chunkIndex, bytesRead, bytesRead / bytesPerSample);
                
                yield return chunk;
                
                chunkIndex++;
                
                // Add a small delay to simulate real-time recording (optional)
                await Task.Delay(100); // ~100ms delay to simulate real-time chunks
            }
        }
        
        public Task StopAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the audio recording service is currently recording.
        /// </summary>
        /// <remarks>
        /// This property allows consumers to check or configure the recording state
        /// of the audio recording service. A value of <c>true</c> signifies that the service
        /// is actively recording, while <c>false</c> indicates that recording is idle or stopped.
        /// </remarks>
        public bool IsRecording { get; set; }
    }
}