using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel.Text;
using Tiktoken.Encodings;
//using Encoder = Tiktoken.Encoder;

namespace Aesir.Api.Server.Services;

/// <summary>
/// Provides text chunking functionality for dividing large text documents into smaller, manageable chunks.
/// </summary>
/// <param name="tokensPerParagraph">The maximum number of tokens per paragraph chunk.</param>
/// <param name="tokensPerLine">The maximum number of tokens per line chunk.</param>
[Experimental("SKEXP0050")]
public class DocumentChunker(int tokensPerParagraph, int tokensPerLine)
{
    /// <summary>
    /// Gets the default encoding used for token counting.
    /// </summary>
    public static Encoding DefaultEncoding => new P50KBase();
    
    //private readonly Encoder _encoder = new(DefaultEncoding);
    
    /// <summary>
    /// Chunks the provided text into smaller segments based on the configured token limits.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="chunkHeader">Optional header to prepend to each chunk.</param>
    /// <returns>A list of text chunks.</returns>
    public List<string> ChunkText(string text, string? chunkHeader = null)
    {
        // var tokens = _encoder.Encode(text).ToList();
        // var chunks = new List<string>();
        // var start = 0;
        //
        // while (start < tokens.Count)
        // {
        //     var end = Math.Min(start + chunkSize, tokens.Count);
        //     var chunkTokens = tokens.GetRange(start, end - start);
        //     var chunkText = _encoder.Decode(chunkTokens);
        //     chunks.Add(chunkText);
        //     start += chunkSize - overlap; // Move forward, preserving overlap
        // }
        //
        // return chunks;
        
        var lines = TextChunker.SplitPlainTextLines(text, tokensPerLine);
        return TextChunker.SplitPlainTextParagraphs(lines, tokensPerParagraph, chunkHeader: chunkHeader);
    }
}