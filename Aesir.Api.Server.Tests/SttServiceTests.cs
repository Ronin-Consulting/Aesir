using Aesir.Api.Server.Services.Implementations.Onnx;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using Whisper.net.Ggml;

namespace Aesir.Api.Server.Tests;

/// <summary>
/// Unit test class for validating the functionality of the SttService class, which provides
/// speech-to-text capabilities utilizing Whisper and Silero VAD models.
/// </summary>
[TestClass]
public class SttServiceTests
{
    /// <summary>
    /// Validates the functionality of stopping and starting the Speech-to-Text (Stt) service
    /// by processing audio input, calculating buffer sizes, and verifying the recognition of text chunks.
    /// </summary>
    /// <returns>
    /// A Task representing the asynchronous operation for the test.
    /// The result indicates whether the Stt service processes audio input correctly
    /// and recognizes text without errors in stop and start scenarios.
    /// </returns>
    [TestMethod]
    public async Task Test_SttService_StopAndStart()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Trace)
        );

        var currentDirectory = Directory.GetCurrentDirectory();
        var testlogger = loggerFactory.CreateLogger<SttServiceTests>();
        testlogger.LogInformation("Current working directory: {Directory}", currentDirectory);
        
        const GgmlType ggmlType = GgmlType.Base;
        var whisperModelPath = "Assets/whisper/ggml-base.bin";
        var vadModelPath = "Assets/vad/silero_vad.onnx";
        if (!File.Exists(whisperModelPath))
        {
            await using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(ggmlType);
            await using var fileWriter = File.OpenWrite(whisperModelPath);
            await modelStream.CopyToAsync(fileWriter);
        }

        var sttConfig = new SttConfig()
        {
            WhisperModelPath = whisperModelPath,
            VadModelPath = vadModelPath
        };
        
        var logger = loggerFactory.CreateLogger<SttService>();
        using var service = new SttService(logger, sttConfig);
        
        const string wavFileName = "kennedy.wav";
        
        await using var waveReader = new WaveFileReader(wavFileName);
        
        
        var sampleProvider = waveReader.ToSampleProvider().ToMono().ToWaveProvider16();
        var waveFormat = sampleProvider.WaveFormat;
        testlogger.LogDebug("Sample Rate: {SampleRate}, Channels: {Channels}, Bits Per Sample: {BitsPerSample}", 
            waveFormat.SampleRate,
            waveFormat.Channels,
            waveFormat.BitsPerSample);
        
        // Calculate buffer size based on the resampled audio
        var durationSeconds = waveReader.TotalTime.TotalSeconds;
        var resampledSamples = (long)(durationSeconds * 16000); // 16kHz mono
        var bufferSize = (int)(resampledSamples * 2); // 16-bit = 2 bytes per sample

        var samples = new byte[bufferSize];
        
        testlogger.LogInformation("Total samples {TotalSamples} bytes from resampled audio", samples.Length);
        
        var bytesRead= sampleProvider.Read(samples, 0, samples.Length);
        
        testlogger.LogInformation("Read {BytesRead} bytes from resampled audio", bytesRead);
        
        // Trim the array to actual data if needed
        if (bytesRead < samples.Length)
        {
            Array.Resize(ref samples, bytesRead);
        }
        
        var textRecognized = string.Empty;
        await foreach (var chunk in service.GenerateTextChunksAsync(AsyncEnumerable.Repeat(samples, 1)))
        {
            textRecognized += chunk;
        }
        
        testlogger.LogInformation("We got: {TextRecognized}", textRecognized);
    }
}