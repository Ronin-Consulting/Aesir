using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace Aesir.Api.Server.Models;

[Experimental("SKEXP0001")]
public class AesirTextData<TKey>
{
    [VectorStoreRecordKey]
    public required TKey Key { get; set; }

    [TextSearchResultValue]
    [VectorStoreRecordData]
    public string? Text { get; set; }
    
    [TextSearchResultName]
    [VectorStoreRecordData]
    public string? ReferenceDescription { get; set; }

    [TextSearchResultLink]
    [VectorStoreRecordData]
    public string? ReferenceLink { get; set; }

    [VectorStoreRecordVector(768)]
    public ReadOnlyMemory<float> TextEmbedding { get; set; }
}