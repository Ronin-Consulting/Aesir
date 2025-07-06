namespace Aesir.Api.Server.Services.Implementations.OpenAI;

public class VisionService : IVisionService
{
    public Task<string> GetImageTextAsync(ReadOnlyMemory<byte> image, string mimeType, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Implement me please.");
    }
}

public class VisionModelConfig
{
    public required string ModelId { get; set; }
}