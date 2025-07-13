using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace Aesir.Api.Server.Models;

/// <summary>
/// Represents text data with vector embeddings for semantic search and retrieval.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify the text data.</typeparam>
[Experimental("SKEXP0001")]
public class AesirTextData<TKey>
{
    /// <summary>
    /// Gets or sets the unique key for identifying this text data record.
    /// </summary>
    [VectorStoreKey(StorageName = "key")]
    public required TKey Key { get; set; }

    /// <summary>
    /// Gets or sets the text content of the document.
    /// </summary>
    [TextSearchResultValue]
    [VectorStoreData(StorageName = "text", IsFullTextIndexed = true)]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets a description of the reference source for this text.
    /// </summary>
    [TextSearchResultName]
    [VectorStoreData(StorageName = "reference_description")]
    public string? ReferenceDescription { get; set; }

    /// <summary>
    /// Gets or sets a link or reference to the original source of this text.
    /// </summary>
    [TextSearchResultLink]
    [VectorStoreData(StorageName = "reference_link")]
    public string? ReferenceLink { get; set; }
    
    /// <summary>
    /// Gets or sets the vector embedding for the text content.
    /// </summary>
    [VectorStoreVector(1024, StorageName = "text_embedding")]
    public Embedding<float>? TextEmbedding { get; set; }
    
    /// <summary>
    /// Gets or sets the number of tokens in the text content.
    /// </summary>
    [VectorStoreData(StorageName = "token_count")]
    public int? TokenCount { get; set; }
}

/// <summary>
/// Represents text data for global documents with category classification.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify the text data.</typeparam>
[Experimental("SKEXP0001")]
public class AesirGlobalDocumentTextData<TKey> : AesirTextData<TKey>
{
    /// <summary>
    /// Gets or sets the category or classification for this global document.
    /// </summary>
    [VectorStoreData(StorageName = "category")]
    public string? Category { get; set; }
}

/// <summary>
/// Represents text data for conversation-specific documents.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify the text data.</typeparam>
[Experimental("SKEXP0001")]
public class AesirConversationDocumentTextData<TKey> : AesirTextData<TKey>
{
    /// <summary>
    /// Gets or sets the identifier of the conversation this document belongs to.
    /// </summary>
    [VectorStoreData(StorageName = "conversation_id")]
    public string? ConversationId { get; set; }
}