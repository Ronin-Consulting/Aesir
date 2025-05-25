using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace Aesir.Api.Server.Models;

[Experimental("SKEXP0001")]
public class AesirTextData<TKey>
{
    [VectorStoreKey]
    public required TKey Key { get; set; }

    [TextSearchResultValue]
    [VectorStoreData]
    public string? Text { get; set; }

    [TextSearchResultName]
    [VectorStoreData]
    public string? ReferenceDescription { get; set; }

    [TextSearchResultLink]
    [VectorStoreData]
    public string? ReferenceLink { get; set; }

    [VectorStoreVector(768)]
    public Embedding<float>? TextEmbedding { get; set; }
}