using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.ML.Tokenizers;
using Microsoft.SemanticKernel.Text;

namespace Aesir.Modules.Documents.Services.DocumentLoaders;

/// <summary>
/// Provides text chunking functionality for dividing large text documents into smaller, manageable chunks.
/// Uses BERT tokenizer for accurate token counting with mxbai-embed-large-v1 and similar models.
/// </summary>
/// <param name="tokensPerParagraph">The maximum number of tokens per paragraph chunk.</param>
/// <param name="tokensPerLine">The maximum number of tokens per line chunk.</param>
/// <param name="maxTokensHardLimit">Hard limit for embedding model context size (512 for BERT models).</param>
[Experimental("SKEXP0050")]
public class DocumentChunker(int tokensPerParagraph = 300, int tokensPerLine = 100, int maxTokensHardLimit = 450)
{
    /// <summary>
    /// Lazy-loaded BERT tokenizer instance using the embedded vocabulary.
    /// </summary>
    private static readonly Lazy<BertTokenizer> LazyTokenizer = new(LoadBertTokenizer);

    /// <summary>
    /// Gets the shared BERT tokenizer instance.
    /// </summary>
    public static BertTokenizer Tokenizer => LazyTokenizer.Value;

    /// <summary>
    /// Loads the BERT tokenizer from the embedded vocabulary resource.
    /// </summary>
    private static BertTokenizer LoadBertTokenizer()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "Aesir.Modules.Documents.Resources.bert-vocab.txt";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Could not find embedded resource '{resourceName}'. " +
                "Ensure bert-vocab.txt is included as an embedded resource.");

        return BertTokenizer.Create(stream);
    }

    /// <summary>
    /// Counts the number of tokens in the provided text using the BERT tokenizer.
    /// </summary>
    /// <param name="text">The text for which the tokens will be counted.</param>
    /// <returns>The total number of tokens in the input text.</returns>
    public int CountTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        return Tokenizer.CountTokens(text);
    }

    /// <summary>
    /// Static method to count tokens using the shared tokenizer.
    /// </summary>
    /// <param name="text">The text for which the tokens will be counted.</param>
    /// <returns>The total number of tokens in the input text.</returns>
    public static int CountTokensStatic(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        return Tokenizer.CountTokens(text);
    }

    /// <summary>
    /// Chunks the provided text into smaller segments based on the configured token limits.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="chunkHeader">Optional header to prepend to each chunk.</param>
    /// <returns>A list of text chunks.</returns>
    public List<string> ChunkText(string text, string? chunkHeader = null)
    {
        var lines = TextChunker.SplitPlainTextLines(text, tokensPerLine, CountTokens);
        var chunks = TextChunker.SplitPlainTextParagraphs(lines, tokensPerParagraph,
            overlapTokens: (int)(tokensPerParagraph * 0.1),
            chunkHeader: chunkHeader, CountTokens);

        // Safety net: force-split any chunks that exceed the hard limit
        var result = new List<string>();
        foreach (var chunk in chunks)
        {
            var tokenCount = CountTokens(chunk);
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
        var headerTokens = string.IsNullOrEmpty(chunkHeader) ? 0 : CountTokens(chunkHeader);
        var targetTokens = maxTokensHardLimit - headerTokens - 20; // Leave buffer for safety

        // Strip existing header if present (it will be re-added)
        var content = chunk;
        if (!string.IsNullOrEmpty(chunkHeader) && chunk.StartsWith(chunkHeader))
        {
            content = chunk.Substring(chunkHeader.Length);
        }

        // BERT tokenizer typically produces ~1.2-1.5 tokens per word, ~0.25 tokens per char
        // Use conservative estimate of ~3 chars per token
        var charsPerToken = 3;
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
                var tokenCount = CountTokens(fullChunk);
                while (tokenCount > maxTokensHardLimit && segment.Length > 50)
                {
                    // Reduce segment size
                    segment = segment.Substring(0, (int)(segment.Length * 0.8));
                    fullChunk = string.IsNullOrEmpty(chunkHeader) ? segment : chunkHeader + segment;
                    tokenCount = CountTokens(fullChunk);
                    endPos = currentPos + segment.Length;
                }

                result.Add(fullChunk);
            }

            currentPos = endPos;
        }

        return result.Count > 0 ? result : [chunk]; // Return original if splitting fails
    }
}
