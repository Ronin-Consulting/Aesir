using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace Aesir.Api.Server.Models;

[Experimental("SKEXP0001")]
public class AesirTextData<TKey>
{
    [VectorStoreKey(StorageName = "key")]
    public required TKey Key { get; set; }

    [TextSearchResultValue]
    [VectorStoreData(StorageName = "text")]
    public string? Text { get; set; }

    [TextSearchResultName]
    [VectorStoreData(StorageName = "reference_description")]
    public string? ReferenceDescription { get; set; }

    [TextSearchResultLink]
    [VectorStoreData(StorageName = "reference_link")]
    public string? ReferenceLink { get; set; }
    
    [VectorStoreVector(768, StorageName = "text_embedding")]
    public Embedding<float>? TextEmbedding { get; set; }
    
    [VectorStoreData(StorageName = "token_count")]
    public int? TokenCount { get; set; }
}

[Experimental("SKEXP0001")]
public class AesirGlobalDocumentTextData<TKey> : AesirTextData<TKey>
{
    [VectorStoreData(StorageName = "category")]
    public string? Category { get; set; }
}

[Experimental("SKEXP0001")]
public class AesirConversationDocumentTextData<TKey> : AesirTextData<TKey>
{
    [VectorStoreData(StorageName = "conversation_id")]
    public string? ConversationId { get; set; }
}