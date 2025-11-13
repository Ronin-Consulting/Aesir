using System.Runtime.CompilerServices;
using Aesir.Modules.Speech.Services;
using Microsoft.AspNetCore.SignalR;

namespace Aesir.Modules.Speech.Hubs;

/// <summary>
/// Defines a SignalR Hub that processes real-time audio streams, converting them into text
/// by leveraging the functionalities provided by <see cref="ISttService"/>.
/// </summary>
public class SttHub(ISttService sttService) : Hub
{
    /// <summary>
    /// Processes an audio stream by converting audio frames into text chunks asynchronously.
    /// </summary>
    /// <param name="audioFrames">An asynchronous stream of audio frame data represented as byte arrays.</param>
    /// <param name="cancellationToken">Cancellation token to handle client disconnections or additional cancellation signals.</param>
    /// <returns>An asynchronous enumerable of text chunks generated from the processed audio frames.</returns>
    public async IAsyncEnumerable<string> ProcessAudioStream(
        IAsyncEnumerable<byte[]> audioFrames,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Combine the provided cancellation token with the connection abort token
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            Context.ConnectionAborted);

        IAsyncEnumerator<string>? enumerator = null;
        
        try
        {
            enumerator = sttService.GenerateTextChunksAsync(audioFrames, combinedCts.Token).GetAsyncEnumerator(combinedCts.Token);
            
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                }
                catch (OperationCanceledException)
                {
                    // Client disconnected - exit gracefully
                    yield break;
                }
                catch (HubException ex) when (ex.Message.Contains("Stream canceled by client"))
                {
                    // This is also an expected exception when the client cancels the incoming stream.
                    // We break the loop to exit gracefully.
                    break;
                }
                
                if (!hasNext)
                    break;

                // Check for cancellation before yielding
                if (combinedCts.Token.IsCancellationRequested)
                    yield break;

                yield return enumerator.Current;
            }
        }
        finally
        {
            if (enumerator != null)
            {
                try
                {
                    await enumerator.DisposeAsync();
                }
                catch (OperationCanceledException)
                {
                    // Ignore cancellation during disposal
                }
            }
        }
    }
}