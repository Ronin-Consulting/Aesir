namespace Aesir.Api.Server.Services;

public interface ISttService
{
    IAsyncEnumerable<string> GenerateTextChunksAsync(IAsyncEnumerable<byte[]> audioStream, CancellationToken cancellationToken = default);
}