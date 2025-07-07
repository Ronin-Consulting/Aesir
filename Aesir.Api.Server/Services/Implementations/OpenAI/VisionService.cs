namespace Aesir.Api.Server.Services.Implementations.OpenAI;

/// <summary>
/// Provides vision AI services using the OpenAI backend.
/// </summary>
public class VisionService : IVisionService
{
    public Task<string> GetImageTextAsync(ReadOnlyMemory<byte> image, string mimeType, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Implement me please.");
    }
}

/// <summary>
/// Configuration class for vision model settings.
/// </summary>
public class VisionModelConfig
{
    public required string ModelId { get; set; }
}