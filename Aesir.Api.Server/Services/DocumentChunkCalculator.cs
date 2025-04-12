using System.Text;
using System.Text.RegularExpressions;
using LangChain.DocumentLoaders;

namespace Aesir.Api.Server.Services;

public partial class DocumentChunkCalculator(IDocumentLoader documentLoader, DataSource dataSource)
{
    private const int BaseChunkSizeWords = 200; // Base size, adjustable per document
    
    public async Task<int> CalculateChunkSizeAsync(CancellationToken cancellationToken = default)
    {
        var documents = await documentLoader.LoadAsync(dataSource, cancellationToken: cancellationToken);

        var stringBuilder = new StringBuilder();
        
        foreach (var document in documents)
        {
            stringBuilder.Append($"{document.PageContent}\n");
        }
        
        return CalculateChunkSize(stringBuilder.ToString());
    }
    
    public async Task<int> CalculateChunkOverlapAsync(int chunkSize, CancellationToken cancellationToken = default)
    {
        // 1.	Small Chunks (50-150 tokens):
        //     •	Overlap: 20-30% (e.g., 10-50 tokens)
        //     •	Why: Smaller chunks lose context more easily, so a larger overlap is helpful to capture connecting details between chunks.
        // 2.	Medium Chunks (150-300 tokens):
        //     •	Overlap: 15-20% (e.g., 30-60 tokens)
        //     •	Why: These chunks often capture a single idea or paragraph, so a moderate overlap is enough to carry context forward.
        // 3.	Large Chunks (300-500+ tokens):
        //     •	Overlap: 10-15% (e.g., 30-75 tokens)
        //     •	Why: Larger chunks tend to contain substantial context on their own, so less overlap is usually needed to maintain continuity.

        await Task.CompletedTask;
        
        if (chunkSize < 50)
        {
            return chunkSize / 5;  // 20% overlap for small chunks
        }
        else if (chunkSize < 150)
        {
            return chunkSize / 4;  // 25% overlap for small chunks
        }
        else if (chunkSize < 300)
        {
            return chunkSize / 3;  // 33% overlap for medium chunks
        }
        else if (chunkSize < 500)
        {
            return chunkSize / 4;  // 25% overlap for large chunks
        }
        else
        {
            return chunkSize / 5;  // 20% overlap for extra-large chunks
        }
    }
    
    private int CalculateChunkSize(string document)
    {
        // Heuristic 1: Adjust based on document length
        var wordCount = CountWords(document);
        if (wordCount < 500)
        {
            return BaseChunkSizeWords / 2;  // Smaller chunks for short documents
        }
        else if (wordCount > 2000)
        {
            return BaseChunkSizeWords * 2;  // Larger chunks for long documents
        }

        // Heuristic 2: Adjust based on sentence complexity (e.g., average sentence length)
        var avgSentenceLength = AverageSentenceLength(document);
        if (avgSentenceLength > 20)
        {
            return BaseChunkSizeWords * 3 / 2;  // Longer chunks for complex sentences
        }

        return BaseChunkSizeWords;  // Default chunk size
    }
    
    private static int AverageSentenceLength(string document)
    {
        var sentences = SentenceRegex().Split(document);
        var totalWords = sentences.Sum(CountWords);
        return totalWords / Math.Max(sentences.Length, 1);
    }
    
    private static int CountWords(string text)
    {
        return text.Split([' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length;
    }

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceRegex();
}