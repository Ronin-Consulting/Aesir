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
    /// Gets or sets the identifier key associated with this text data record.
    /// </summary>
    [VectorStoreKey(StorageName = "key")]
    public required TKey Key { get; set; }

    /// <summary>
    /// Gets or sets the textual content associated with this data record.
    /// </summary>
    [TextSearchResultValue]
    [VectorStoreData(StorageName = "text", IsFullTextIndexed = true)]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the description associated with the reference of this text data record.
    /// </summary>
    [TextSearchResultName]
    [VectorStoreData(StorageName = "reference_description")]
    public string? ReferenceDescription { get; set; }

    /// <summary>
    /// Gets or sets the external reference link associated with this text data record.
    /// </summary>
    [TextSearchResultLink]
    [VectorStoreData(StorageName = "reference_link")]
    public string? ReferenceLink { get; set; }

    /// <summary>
    /// Gets or sets the text embedding representation as a vector,
    /// which can be used for semantic understanding and similarity calculations.
    /// </summary>
    [VectorStoreVector(1024, StorageName = "text_embedding")]
    public Embedding<float>? TextEmbedding { get; set; }

    /// <summary>
    /// Gets or sets the number of tokens associated with the text data.
    /// </summary>
    [VectorStoreData(StorageName = "token_count")]
    public int? TokenCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp indicating when the text data was created.
    /// </summary>
    [VectorStoreData(StorageName = "created_at", IsIndexed = true)]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents text data for global documents with an additional property for category classification.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify the text data.</typeparam>
[Experimental("SKEXP0001")]
public class AesirGlobalDocumentTextData<TKey> : AesirTextData<TKey>
{
    /// <summary>
    /// Gets or sets the category classification for the text data record.
    /// </summary>
    [VectorStoreData(StorageName = "category")]
    public string? Category { get; set; }
}

/// <summary>
/// Represents conversation-specific text data with additional metadata for semantic search and retrieval.
/// Inherits from <see cref="AesirTextData{TKey}"/>.
/// </summary>
/// <typeparam name="TKey">The type of the key used to uniquely identify the text data.</typeparam>
[Experimental("SKEXP0001")]
public class AesirConversationDocumentTextData<TKey> : AesirTextData<TKey>
{
    /// <summary>
    /// Gets or sets the identifier associated with a specific conversation.
    /// </summary>
    [VectorStoreData(StorageName = "conversation_id")]
    public string? ConversationId { get; set; }
}

/// <summary>
/// Represents a contract for JSON-based text data, providing properties for
/// text content, JSON path, node type, and parent information.
/// </summary>
public interface IJsonTextData
{
    /// <summary>
    /// Gets or sets the textual content associated with the data record.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the JSON path used to locate specific data within a JSON structure.
    /// </summary>
    public string? JsonPath { get; set; }

    /// <summary>
    /// Gets or sets the type of the node in the JSON structure. This property is used to provide
    /// contextual information on how to interpret the JSON data, such as whether the node represents
    /// a value, an object, or an array.
    /// </summary>
    public string? NodeType { get; set; }

    /// <summary>
    /// Gets or sets the information about the parent node or element in the JSON hierarchy
    /// from which the current text data record originates.
    /// </summary>
    public string? ParentInfo { get; set; }
}

/// <summary>
/// Represents conversation-specific text data, integrating JSON metadata for enriched context and search capabilities.
/// </summary>
/// <typeparam name="TKey">The type of the key used to uniquely identify the text data.</typeparam>
[Experimental("SKEXP0001")]
public class AesirConversationJsonTextData<TKey> : AesirConversationDocumentTextData<TKey>, IJsonTextData
{
    /// <summary>
    /// Gets or sets the JSONPath expression that identifies a specific location within a JSON document.
    /// </summary>
    [VectorStoreData(StorageName = "json_path", IsFullTextIndexed = true, IsIndexed = true)]
    public string? JsonPath { get; set; }

    /// <summary>
    /// Gets or sets the type of the node within the JSON structure.
    /// </summary>
    [VectorStoreData(StorageName = "node_type", IsIndexed = true)]
    public string? NodeType { get; set; }

    /// <summary>
    /// Gets or sets information related to the parent structure or entity of this text data node.
    /// </summary>
    [VectorStoreData(StorageName = "parent_info")]
    public string? ParentInfo { get; set; }
}

/// <summary>
/// Represents text data for global documents with JSON-specific metadata and
/// hierarchical relationships, enabling category classification, semantic search, and retrieval.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify the text data.</typeparam>
[Experimental("SKEXP0001")]
public class AesirGlobalJsonTextData<TKey> : AesirGlobalDocumentTextData<TKey>, IJsonTextData
{
    /// <summary>
    /// Gets or sets the JSONPath of the text data, which specifies the location within the JSON structure.
    /// </summary>
    [VectorStoreData(StorageName = "json_path", IsFullTextIndexed = true, IsIndexed = true)]
    public string? JsonPath { get; set; }

    /// <summary>
    /// Gets or sets the type of the node associated with the JSON data.
    /// This is used for categorization or identification within the context of processed text data.
    /// </summary>
    [VectorStoreData(StorageName = "node_type", IsIndexed = true)]
    public string? NodeType { get; set; }

    /// <summary>
    /// Gets or sets information about the parent node or structure associated with the current entity.
    /// </summary>
    [VectorStoreData(StorageName = "parent_info")]
    public string? ParentInfo { get; set; }
}

/// <summary>
/// Defines the structure for XML text data with associated metadata and path information.
/// </summary>
public interface IXmlTextData
{
    /// <summary>
    /// Gets or sets the text data content.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the file path or navigation path to the XML data associated with the object.
    /// </summary>
    public string? XmlPath { get; set; }

    /// <summary>
    /// Gets or sets the type of the XML node.
    /// </summary>
    public string? NodeType { get; set; }

    /// <summary>
    /// Gets or sets the information about the parent node in the XML structure.
    /// </summary>
    public string? ParentInfo { get; set; }
}

/// <summary>
/// Represents conversation-specific XML text data with additional metadata, such as XML path, node type, and parent information,
/// for advanced semantic search and retrieval.
/// Inherits from <see cref="AesirConversationDocumentTextData{TKey}"/> and implements <see cref="IXmlTextData"/>.
/// </summary>
/// <typeparam name="TKey">The type of the key used to uniquely identify the text data.</typeparam>
[Experimental("SKEXP0001")]
public class AesirConversationXmlTextData<TKey> : AesirConversationDocumentTextData<TKey>, IXmlTextData
{
    /// <summary>
    /// Gets or sets the path of the associated XML element or node.
    /// </summary>
    [VectorStoreData(StorageName = "xml_path", IsFullTextIndexed = true, IsIndexed = true)]
    public string? XmlPath { get; set; }

    /// <summary>
    /// Gets or sets the type of the node within the XML structure, typically representing
    /// the role or classification of the element in the document hierarchy.
    /// </summary>
    [VectorStoreData(StorageName = "node_type", IsIndexed = true)]
    public string? NodeType { get; set; }

    /// <summary>
    /// Gets or sets information about the parent node in the XML structure.
    /// Typically used to provide context or hierarchy information for the current node.
    /// </summary>
    [VectorStoreData(StorageName = "parent_info")]
    public string? ParentInfo { get; set; }
}