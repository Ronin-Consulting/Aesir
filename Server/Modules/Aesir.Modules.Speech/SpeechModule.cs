using Aesir.Infrastructure.Modules;
using Aesir.Modules.Speech.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Speech;

/// <summary>
/// Speech module providing Text-to-Speech (TTS) and Speech-to-Text (STT) services.
/// </summary>
public class SpeechModule : ModuleBase
{
    public SpeechModule(ILogger logger) : base(logger)
    {
    }

    public override string Name => "Speech";

    public override string Version => "1.0.0";

    public override string? Description => "Provides Text-to-Speech and Speech-to-Text services using Whisper and VITS-Piper models";

    public override Task RegisterServicesAsync(IServiceCollection services)
    {
        Log("Registering TTS and STT services...");

        // Get model paths from database configuration (ConfigurationService is set by ModuleExtensions before this is called)
        var generalSettings = ConfigurationService!.GetGeneralSettingsAsync().GetAwaiter().GetResult();
        var ttsModelPath = generalSettings.TtsModelPath ??
                          throw new InvalidOperationException("TtsModelPath not configured in GeneralSettings");
        var sttModelPath = generalSettings.SttModelPath ??
                          throw new InvalidOperationException("SttModelPath not configured in GeneralSettings");
        var vadModelPath = generalSettings.VadModelPath ??
                          throw new InvalidOperationException("VadModelPath not configured in GeneralSettings");

        Log($"TTS model path: {ttsModelPath}");
        Log($"STT model path: {sttModelPath}");
        Log($"VAD model path: {vadModelPath}");

        // Register TTS text preprocessor for converting markdown to natural speech
        services.AddSingleton<ITtsTextPreprocessor, TtsTextPreprocessor>();

        // Register TTS service
        services.AddSingleton<ITtsService>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var textPreprocessor = sp.GetRequiredService<ITtsTextPreprocessor>();

            var useCudaValue = Environment.GetEnvironmentVariable("USE_CUDA");
            _ = bool.TryParse(useCudaValue, out var useCuda);

            var ttsConfig = TtsConfig.Default;
            ttsConfig.ModelPath = ttsModelPath;
            ttsConfig.CudaEnabled = useCuda;

            return new TtsService(loggerFactory.CreateLogger<TtsService>(), textPreprocessor, ttsConfig);
        });

        // Register STT service
        services.AddSingleton<ISttService>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            var useCudaValue = Environment.GetEnvironmentVariable("USE_CUDA");
            _ = bool.TryParse(useCudaValue, out var useCuda);

            var sttConfig = SttConfig.Default;
            sttConfig.WhisperModelPath = sttModelPath;
            sttConfig.VadModelPath = vadModelPath;
            sttConfig.CudaEnabled = useCuda;

            return new SttService(loggerFactory.CreateLogger<SttService>(), sttConfig);
        });

        Log("TTS and STT services registered successfully");

        return Task.CompletedTask;
    }

    public override void Initialize(IApplicationBuilder app)
    {
        Log("Speech module initialized successfully");
        // Note: SignalR hub mappings are handled in Program.cs after module discovery
    }
}
