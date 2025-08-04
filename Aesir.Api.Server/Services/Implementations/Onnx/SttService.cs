using SherpaOnnx;

namespace Aesir.Api.Server.Services.Implementations.Onnx;

/// <summary>
/// The SttService class implements speech-to-text (STT) functionality, enabling the conversion
/// of audio input into text output. It supports streaming text generation and is designed to
/// operate with an online STT engine using configurable settings such as model path and hardware acceleration.
/// </summary>
public class SttService : ISttService
{
    /// <summary>
    /// Provides logging capabilities for the <see cref="SttService"/> class.
    /// </summary>
    private readonly ILogger<SttService> _logger;

    /// <summary>
    /// Holds the instance of the speech-to-text engine utilized for converting audio inputs into recognized text.
    /// </summary>
    private readonly OnlineRecognizer _sttEngine;

    /// <summary>
    /// Initializes a new instance of the SttService class.
    /// </summary>
    /// <param name="logger">The logger instance for logging operations.</param>
    /// <param name="modelPath">The path to the STT model file.</param>
    /// <param name="useCuda">Whether to use CUDA acceleration.</param>
    public SttService(ILogger<SttService> logger, string? modelPath, bool useCuda)
    {
        _logger = logger;

        if (string.IsNullOrEmpty(modelPath))
        {
            throw new ArgumentNullException(nameof(modelPath), "Model path cannot be null or empty");
        }

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Model file not found at path: {modelPath}");
        }

        var encoderPath = Path.Combine(Path.GetDirectoryName(modelPath) ?? throw new InvalidOperationException(),
            "encoder-epoch-99-avg-1.onnx");
        var decoderPath = Path.Combine(Path.GetDirectoryName(modelPath) ?? throw new InvalidOperationException(),
            "decoder-epoch-99-avg-1.onnx");
        var joinerPath = Path.Combine(Path.GetDirectoryName(modelPath) ?? throw new InvalidOperationException(),
            "joiner-epoch-99-avg-1.onnx");
        var tokensPath = Path.Combine(Path.GetDirectoryName(modelPath) ?? throw new InvalidOperationException(),
            "tokens.txt");
        
        if (!File.Exists(encoderPath))
        {
            throw new FileNotFoundException($"Encoder model file not found at path: {encoderPath}");
        }
        if (!File.Exists(decoderPath))
        {
            throw new FileNotFoundException($"Decoder model file not found at path: {decoderPath}");
        }
        if (!File.Exists(joinerPath))
        {
            throw new FileNotFoundException($"Joiner model file not found at path: {joinerPath}");
        }
        if (!File.Exists(tokensPath))
        {
            throw new FileNotFoundException($"Tokens file not found at path: {tokensPath}");
        }
        
        _logger.LogInformation("Initializing TTS engine with model: {ModelPath}", modelPath);
        
        var config = new OnlineRecognizerConfig
        {
            FeatConfig = new FeatureConfig
            {
                SampleRate = 48000,
                FeatureDim = 80
            },
            ModelConfig = new OnlineModelConfig
            {
                Transducer = new OnlineTransducerModelConfig
                {
                    Encoder = encoderPath,
                    Decoder = decoderPath,
                    Joiner = joinerPath
                },
                Tokens = tokensPath,
                NumThreads = 4,
                Provider = useCuda ? "cuda" : "cpu"
            }
        };
        _sttEngine = new OnlineRecognizer(config);
    }

    public async IAsyncEnumerable<string> GenerateTextChunksAsync(IAsyncEnumerable<byte[]> audioStream)
    {
        // Create online stream for real-time recognition
        var stream = _sttEngine.CreateStream();
        var lastText = "";
        
        await foreach (var chunk in audioStream)
        {
            // Convert byte data to float samples (assuming 16-bit PCM audio)
            var samples = ConvertBytesToFloats(chunk);
            
            // Feed audio chunk to the recognizer
            stream.AcceptWaveform(48000, samples);
            
            // Check for partial results while streaming
            while (_sttEngine.IsReady(stream))
            {
                _sttEngine.Decode(stream);
            }
            
            // Get current recognition result
            var result = _sttEngine.GetResult(stream);
            
            // Check if we have new text to yield
            if (!string.IsNullOrEmpty(result.Text) && result.Text != lastText)
            {
                // Extract only the new portion of text
                var newText = result.Text;
                if (newText.StartsWith(lastText))
                {
                    newText = newText.Substring(lastText.Length).Trim();
                }
                
                if (!string.IsNullOrEmpty(newText))
                {
                    yield return newText;
                    lastText = result.Text;
                }
            }
        }
        
        // Process any remaining audio and get final result
        stream.InputFinished();
        while (_sttEngine.IsReady(stream))
        {
            _sttEngine.Decode(stream);
        }
        
        var finalResult = _sttEngine.GetResult(stream);
        if (!string.IsNullOrEmpty(finalResult.Text) && finalResult.Text != lastText)
        {
            var newText = finalResult.Text;
            if (newText.StartsWith(lastText))
            {
                newText = newText.Substring(lastText.Length).Trim();
            }
            
            if (!string.IsNullOrEmpty(newText))
            {
                yield return newText;
            }
        }
    }

    /// <summary>
    /// Converts byte array (16-bit PCM) to float array for audio processing.
    /// </summary>
    /// <param name="audioBytes">The audio data as bytes.</param>
    /// <returns>Float array representing audio samples.</returns>
    private static float[] ConvertBytesToFloats(byte[] audioBytes)
    {
        var samples = new float[audioBytes.Length / 2];
        for (int i = 0; i < samples.Length; i++)
        {
            var sample = BitConverter.ToInt16(audioBytes, i * 2);
            samples[i] = sample / 32768.0f;
        }
        return samples;
    }
}