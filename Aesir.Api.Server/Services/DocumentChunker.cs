using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel.Text;
using Tiktoken;
using Tiktoken.Encodings;
//using Encoder = Tiktoken.Encoder;

namespace Aesir.Api.Server.Services;

/// <summary>
/// Provides text chunking functionality for dividing large text documents into smaller, manageable chunks.
/// </summary>
/// <param name="tokensPerParagraph">The maximum number of tokens per paragraph chunk.</param>
/// <param name="tokensPerLine">The maximum number of tokens per line chunk.</param>
[Experimental("SKEXP0050")]
public class DocumentChunker(int tokensPerParagraph = 384, int tokensPerLine = 128)
{
    /// <summary>
    /// Gets the default encoding used for token counting.
    /// </summary>
    public static Encoding DefaultEncoding => new Cl100KBase();
    
    private readonly Encoder _encoder = new(DefaultEncoding);
    
    /// <summary>
    /// Chunks the provided text into smaller segments based on the configured token limits.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="chunkHeader">Optional header to prepend to each chunk.</param>
    /// <returns>A list of text chunks.</returns>
    public List<string> ChunkText(string text, string? chunkHeader = null)
    {
        var lines = TextChunker.SplitPlainTextLines(text, tokensPerLine, s => _encoder.CountTokens(s));
        return TextChunker.SplitPlainTextParagraphs(lines, tokensPerParagraph, 
            overlapTokens: (int)(tokensPerParagraph * 0.2), 
            chunkHeader: chunkHeader, s => _encoder.CountTokens(s));
    }
}