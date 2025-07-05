using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel.Text;
using Tiktoken.Encodings;
//using Encoder = Tiktoken.Encoder;

namespace Aesir.Api.Server.Services;

[Experimental("SKEXP0050")]
public class DocumentChunker(int tokensPerParagraph, int tokensPerLine)
{
    public static Encoding DefaultEncoding => new P50KBase();
    
    //private readonly Encoder _encoder = new(DefaultEncoding);
    
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