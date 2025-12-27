using System.Text;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Inference.OpenAI.Stopgap;

/// <summary>
/// STOPGAP: HTTP DelegatingHandler that intercepts OpenAI-compatible streaming responses
/// to extract reasoning_content before the SDK drops it.
///
/// This handler wraps the response stream to monitor SSE chunks and extract any
/// reasoning_content fields, making them available via the ReasoningContentCollector_Stopgap.
/// </summary>
public sealed class ReasoningContentHandler_Stopgap : DelegatingHandler
{
    private readonly ILogger<ReasoningContentHandler_Stopgap>? _logger;

    public ReasoningContentHandler_Stopgap(ILogger<ReasoningContentHandler_Stopgap>? logger = null)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (!IsStreamingResponse(response))
            return response;

        var collector = ReasoningContentCollector_Stopgap.Current.Value;
        if (collector == null)
        {
            _logger?.LogDebug("[Stopgap] No collector in AsyncLocal context, passing through");
            return response;
        }

        _logger?.LogDebug("[Stopgap] Intercepting streaming response for reasoning_content");

        // Read the original stream and wrap it
        var originalStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var originalHeaders = response.Content.Headers;

        var wrappedStream = new ReasoningInterceptStream_Stopgap(
            originalStream, collector, _logger);

        // Create new content with wrapped stream
        var newContent = new StreamContent(wrappedStream);

        // Copy over content headers
        foreach (var header in originalHeaders)
        {
            newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        response.Content = newContent;

        return response;
    }

    private static bool IsStreamingResponse(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            return false;

        var contentType = response.Content.Headers.ContentType?.MediaType;
        return contentType is "text/event-stream" or "application/x-ndjson";
    }
}

/// <summary>
/// STOPGAP: Stream wrapper that monitors SSE data, extracting reasoning_content
/// without modifying the stream content passed to the SDK.
/// </summary>
internal sealed class ReasoningInterceptStream_Stopgap : Stream
{
    private readonly Stream _innerStream;
    private readonly ReasoningContentCollector_Stopgap _collector;
    private readonly ILogger? _logger;
    private readonly StringBuilder _lineBuffer = new();
    private bool _reasoningEnded;

    public ReasoningInterceptStream_Stopgap(
        Stream innerStream,
        ReasoningContentCollector_Stopgap collector,
        ILogger? logger)
    {
        _innerStream = innerStream;
        _collector = collector;
        _logger = logger;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
        CancellationToken cancellationToken)
    {
        var bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);

        if (bytesRead > 0)
        {
            ProcessBytes(buffer.AsSpan(offset, bytesRead));
        }
        else
        {
            _collector.Complete();
        }

        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var bytesRead = await _innerStream.ReadAsync(buffer, cancellationToken);

        if (bytesRead > 0)
        {
            ProcessBytes(buffer.Span.Slice(0, bytesRead));
        }
        else
        {
            _collector.Complete();
        }

        return bytesRead;
    }

    private void ProcessBytes(ReadOnlySpan<byte> bytes)
    {
        var text = Encoding.UTF8.GetString(bytes);
        _lineBuffer.Append(text);

        var content = _lineBuffer.ToString();
        var lines = content.Split('\n');

        _lineBuffer.Clear();

        // If content doesn't end with newline, save incomplete line for next read
        if (!content.EndsWith('\n') && lines.Length > 0)
        {
            _lineBuffer.Append(lines[^1]);
            lines = lines[..^1];
        }

        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                ProcessLine(line.AsSpan());
            }
        }
    }

    private void ProcessLine(ReadOnlySpan<char> line)
    {
        // Try to extract reasoning content
        var reasoningContent = ReasoningContentSseParser_Stopgap.TryExtractReasoningContent(line);
        if (reasoningContent != null)
        {
            // Fire and forget - we don't want to block the stream
            _ = _collector.WriteAsync(reasoningContent);
            return;
        }

        // Check if we're transitioning from reasoning to content
        if (!_reasoningEnded && ReasoningContentSseParser_Stopgap.HasRegularContent(line))
        {
            _reasoningEnded = true;
            _collector.EndReasoning();
        }
    }

    // Stream pass-through implementations
    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => _innerStream.Length;
    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    public override void Flush() => _innerStream.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _innerStream.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _innerStream.Read(buffer, offset, count);
        if (bytesRead > 0)
        {
            ProcessBytes(buffer.AsSpan(offset, bytesRead));
        }
        else
        {
            _collector.Complete();
        }
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin) =>
        _innerStream.Seek(offset, origin);

    public override void SetLength(long value) => _innerStream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count) =>
        _innerStream.Write(buffer, offset, count);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerStream.Dispose();
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await _innerStream.DisposeAsync();
        await base.DisposeAsync();
    }
}
