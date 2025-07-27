namespace Aesir.Api.Server.Services;

public interface ITtsService
{
    IAsyncEnumerable<byte[]> GenerateAudioChunksAsync(string text, float speed = 1.0f);
}