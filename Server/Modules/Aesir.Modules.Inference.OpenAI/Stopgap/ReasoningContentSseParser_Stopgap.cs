using System.Text.Json;

namespace Aesir.Modules.Inference.OpenAI.Stopgap;

/// <summary>
/// STOPGAP: Parses SSE chunks to extract reasoning_content from OpenAI-compatible responses.
///
/// Expected format from llama.cpp:
/// data: {"choices":[{"delta":{"reasoning_content":" says"}}],"model":"gpt-oss-20b-mxfp4.gguf",...}
/// </summary>
internal static class ReasoningContentSseParser_Stopgap
{
    private const string DataPrefix = "data: ";
    private const string DoneMarker = "[DONE]";

    public static string? TryExtractReasoningContent(ReadOnlySpan<char> line)
    {
        if (!line.StartsWith(DataPrefix.AsSpan()))
            return null;

        var jsonSpan = line.Slice(DataPrefix.Length);

        if (jsonSpan.SequenceEqual(DoneMarker.AsSpan()))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(jsonSpan.ToString());
            var root = doc.RootElement;

            if (!root.TryGetProperty("choices", out var choices))
                return null;

            foreach (var choice in choices.EnumerateArray())
            {
                if (!choice.TryGetProperty("delta", out var delta))
                    continue;

                if (delta.TryGetProperty("reasoning_content", out var reasoningContent))
                {
                    return reasoningContent.GetString();
                }
            }
        }
        catch (JsonException)
        {
            // Malformed JSON - skip silently
        }

        return null;
    }

    public static bool HasRegularContent(ReadOnlySpan<char> line)
    {
        if (!line.StartsWith(DataPrefix.AsSpan()))
            return false;

        var jsonSpan = line.Slice(DataPrefix.Length);

        if (jsonSpan.SequenceEqual(DoneMarker.AsSpan()))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(jsonSpan.ToString());
            var root = doc.RootElement;

            if (!root.TryGetProperty("choices", out var choices))
                return false;

            foreach (var choice in choices.EnumerateArray())
            {
                if (!choice.TryGetProperty("delta", out var delta))
                    continue;

                if (delta.TryGetProperty("content", out var content)
                    && content.ValueKind == JsonValueKind.String
                    && !string.IsNullOrEmpty(content.GetString()))
                {
                    return true;
                }
            }
        }
        catch (JsonException)
        {
            // Malformed JSON - skip silently
        }

        return false;
    }
}
