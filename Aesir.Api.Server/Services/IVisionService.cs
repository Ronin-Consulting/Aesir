namespace Aesir.Api.Server.Services;

public interface IVisionService
{
    Task<string> GetImageTextAsync(ReadOnlyMemory<byte> image, string mimeType, CancellationToken cancellationToken = default);
}