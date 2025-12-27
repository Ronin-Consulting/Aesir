using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel.Text;
using Tiktoken;
using Tiktoken.Encodings;

namespace Aesir.Modules.Documents.Services.DocumentLoaders;

/// <summary>
/// Provides text chunking functionality for dividing large text documents into smaller, manageable chunks.
/// </summary>
/// <param name="tokensPerParagraph">The maximum number of tokens per paragraph chunk.</param>
/// <param name="tokensPerLine">The maximum number of tokens per line chunk.</param>
/// <param name="maxTokensHardLimit">Hard limit for embedding model context size.</param>
[Experimental("SKEXP0050")]
public class DocumentChunker(int tokensPerParagraph = 100, int tokensPerLine = 50, int maxTokensHardLimit = 120)
{
    /// <summary>
    /// Gets the default encoding used for token counting.
    /// </summary>
    public static Encoding DefaultEncoding => new Cl100KBase();

    private readonly Encoder _encoder = new(DefaultEncoding);

    /// <summary>
    /// Counts the number of tokens in the provided text using the default encoder.
    /// </summary>
    /// <param name="text">The text for which the tokens will be counted.</param>
    /// <returns>The total number of tokens in the input text.</returns>
    public int CountTokens(string text)
    {
        return _encoder.CountTokens(text);
    }

    /// <summary>
    /// Chunks the provided text into smaller segments based on the configured token limits.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="chunkHeader">Optional header to prepend to each chunk.</param>
    /// <returns>A list of text chunks.</returns>
    public List<string> ChunkText(string text, string? chunkHeader = null)
    {
        var lines = TextChunker.SplitPlainTextLines(text, tokensPerLine, s => _encoder.CountTokens(s));
        var chunks = TextChunker.SplitPlainTextParagraphs(lines, tokensPerParagraph,
            overlapTokens: (int)(tokensPerParagraph * 0.1),
            chunkHeader: chunkHeader, s => _encoder.CountTokens(s));

        // Safety net: force-split any chunks that exceed the hard limit
        var result = new List<string>();
        foreach (var chunk in chunks)
        {
            var tokenCount = _encoder.CountTokens(chunk);
            if (tokenCount <= maxTokensHardLimit)
            {
                result.Add(chunk);
            }
            else
            {
                result.AddRange(ForceSplitOversizedChunk(chunk, chunkHeader));
            }
        }

        return result;
    }

    /// <summary>
    /// Force splits an oversized chunk into smaller pieces using character-based splitting.
    /// This is a fallback for when TextChunker can't split content (e.g., no natural break points).
    /// </summary>
    private List<string> ForceSplitOversizedChunk(string chunk, string? chunkHeader)
    {
        var result = new List<string>();
        var headerTokens = string.IsNullOrEmpty(chunkHeader) ? 0 : _encoder.CountTokens(chunkHeader);
        var targetTokens = maxTokensHardLimit - headerTokens - 20; // Leave buffer for safety

        // Strip existing header if present (it will be re-added)
        var content = chunk;
        if (!string.IsNullOrEmpty(chunkHeader) && chunk.StartsWith(chunkHeader))
        {
            content = chunk.Substring(chunkHeader.Length);
        }

        // Approximate characters per token (typically ~4 chars per token for English)
        var charsPerToken = 4;
        var targetChars = targetTokens * charsPerToken;

        var currentPos = 0;
        while (currentPos < content.Length)
        {
            var endPos = Math.Min(currentPos + targetChars, content.Length);

            // Try to break at a space or punctuation if possible
            if (endPos < content.Length)
            {
                var breakPos = content.LastIndexOfAny([' ', '.', ',', ';', ':', '\n', '\r', '!', '?'], endPos, Math.Min(endPos - currentPos, 100));
                if (breakPos > currentPos)
                {
                    endPos = breakPos + 1;
                }
            }

            var segment = content.Substring(currentPos, endPos - currentPos).Trim();
            if (!string.IsNullOrWhiteSpace(segment))
            {
                var fullChunk = string.IsNullOrEmpty(chunkHeader) ? segment : chunkHeader + segment;

                // Verify token count and adjust if still too large
                var tokenCount = _encoder.CountTokens(fullChunk);
                while (tokenCount > maxTokensHardLimit && segment.Length > 50)
                {
                    // Reduce segment size
                    segment = segment.Substring(0, (int)(segment.Length * 0.8));
                    fullChunk = string.IsNullOrEmpty(chunkHeader) ? segment : chunkHeader + segment;
                    tokenCount = _encoder.CountTokens(fullChunk);
                    endPos = currentPos + segment.Length;
                }

                result.Add(fullChunk);
            }

            currentPos = endPos;
        }

        return result.Count > 0 ? result : [chunk]; // Return original if splitting fails
    }
}