using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace Aesir.Infrastructure.Models;

/// <summary>
/// Encapsulates textual content and its associated vector embeddings to facilitate semantic operations such as search and retrieval.
/// </summary>
/// <typeparam name="TKey">Specifies the type of the unique identifier associated with the text data.</typeparam>
[Experimental("SKEXP0001")]
public class AesirTextData<TKey>
{
    /// <summary>
    /// Represents the unique identifier key for the associated data entity.
    /// </summary>
    [VectorStoreKey(StorageName = "key")]
    public required TKey Key { get; set; }

    /// <summary>
    /// Gets or sets the text content associated with this data record.
    /// </summary>
    [TextSearchResultValue]
    [VectorStoreData(StorageName = "text", IsFullTextIndexed = true)]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the descriptive text providing detailed information about a specific reference.
    /// </summary>
    [TextSearchResultName]
    [VectorStoreData(StorageName = "reference_description")]
    public string? ReferenceDescription { get; set; }

    /// <summary>
    /// Gets or sets the reference link associated with this entity.
    /// </summary>
    [TextSearchResultLink]
    [VectorStoreData(StorageName = "reference_link")]
    public string? ReferenceLink { get; set; }

    /// <summary>
    /// Gets or sets the vector representation of the text for use in natural language processing or machine learning tasks.
    /// </summary>
    [VectorStoreVector(1024, StorageName = "text_embedding")]
    public Embedding<float>? TextEmbedding { get; set; }

    /// <summary>
    /// Gets or sets the total number of tokens processed or contained in the respective data context.
    /// </summary>
    [VectorStoreData(StorageName = "token_count")]
    public int? TokenCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp indicating when the entity was created.
    /// </summary>
    [VectorStoreData(StorageName = "created_at", IsIndexed = true)]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents global document text data, extending base text data with an additional
/// classification property for semantic organization.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify the text data.</typeparam>
[Experimental("SKEXP0001")]
public class AesirGlobalDocumentTextData<TKey> : AesirTextData<TKey>
{
    /// <summary>
    /// Represents a classification or grouping for a set of related data or items.
    /// </summary>
    [VectorStoreData(StorageName = "category")]
    public string? Category { get; set; }
}

/// <summary>
/// Represents conversation document text data, including vector embeddings
/// for semantic search and contextual information across conversations.
/// </summary>
/// <typeparam name="TKey">The type of the key used to uniquely identify the text data within the conversation document.</typeparam>
[Experimental("SKEXP0001")]
public class AesirConversationDocumentTextData<TKey> : AesirTextData<TKey>
{
    /// <summary>
    /// Gets or sets the unique identifier for the conversation associated with this record.
    /// </summary>
    [VectorStoreData(StorageName = "conversation_id")]
    public string? ConversationId { get; set; }
}

/// <summary>
/// Defines a contract for handling JSON-based text data, including properties for
/// text content, associated JSON path, node type, and parent metadata.
/// </summary>
public interface IJsonTextData
{
    /// <summary>
    /// Gets or sets the textual content associated with this data record.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the JSON path expression used to query or access specific data within a JSON structure.
    /// </summary>
    public string? JsonPath { get; set; }

    /// <summary>
    /// Gets or sets the type of the node, representing its classification or role within a structure or hierarchy.
    /// </summary>
    public string? NodeType { get; set; }

    /// <summary>
    /// Gets or sets the information associated with the parent entity.
    /// </summary>
    public string? ParentInfo { get; set; }
}

/// <summary>
/// Represents conversation data in JSON format for processing and analysis within the Aesir system.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify the conversation data.</typeparam>
[Experimental("SKEXP0001")]
public class AesirConversationJsonTextData<TKey> : AesirConversationDocumentTextData<TKey>, IJsonTextData
{
    /// <summary>
    /// Gets or sets the JSON path used to navigate and query the structure of a JSON document.
    /// </summary>
    [VectorStoreData(StorageName = "json_path", IsFullTextIndexed = true, IsIndexed = true)]
    public string? JsonPath { get; set; }

    /// <summary>
    /// Gets or sets the type of the node. This property is used to classify or
    /// define the category or kind of the node within the data structure.
    /// </summary>
    [VectorStoreData(StorageName = "node_type", IsIndexed = true)]
    public string? NodeType { get; set; }

    /// <summary>
    /// Gets or sets information regarding the parent associated with this text data.
    /// </summary>
    [VectorStoreData(StorageName = "parent_info")]
    public string? ParentInfo { get; set; }
}

/// <summary>
/// Represents text data for global documents with JSON-specific metadata,
/// supporting hierarchical relationships, semantic search, and retrieval operations.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify the text data.</typeparam>
[Experimental("SKEXP0001")]
public class AesirGlobalJsonTextData<TKey> : AesirGlobalDocumentTextData<TKey>, IJsonTextData
{
    /// <summary>
    /// Gets or sets the JSON path associated with this text data,
    /// used for identifying the location of specific elements within the JSON structure.
    /// </summary>
    [VectorStoreData(StorageName = "json_path", IsFullTextIndexed = true, IsIndexed = true)]
    public string? JsonPath { get; set; }

    /// <summary>
    /// Represents the type of a node in a data structure or hierarchy.
    /// </summary>
    [VectorStoreData(StorageName = "node_type", IsIndexed = true)]
    public string? NodeType { get; set; }

    /// <summary>
    /// Gets or sets information related to the parent entity or object.
    /// </summary>
    [VectorStoreData(StorageName = "parent_info")]
    public string? ParentInfo { get; set; }
}

/// <summary>
/// Defines a contract for handling XML-based text data with capabilities for processing, parsing, or manipulation.
/// </summary>
public interface IXmlTextData
{
    /// <summary>
    /// Gets or sets the textual content associated with this record.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the file path or location associated with the XML document.
    /// </summary>
    public string? XmlPath { get; set; }

    /// <summary>
    /// Represents the type of a node within a data structure or hierarchy.
    /// </summary>
    public string? NodeType { get; set; }

    /// <summary>
    /// Gets or sets the information related to the parent entity or object.
    /// </summary>
    public string? ParentInfo { get; set; }
}

/// <summary>
/// Represents XML-based conversation text data for processing and retrieval.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify the conversation text data.</typeparam>
[Experimental("SKEXP0001")]
public class AesirConversationXmlTextData<TKey> : AesirConversationDocumentTextData<TKey>, IXmlTextData
{
    /// <summary>
    /// Gets or sets the XML path associated with the text data,
    /// which provides the hierarchical location of the data within an XML document.
    /// </summary>
    [VectorStoreData(StorageName = "xml_path", IsFullTextIndexed = true, IsIndexed = true)]
    public string? XmlPath { get; set; }

    /// <summary>
    /// Represents the type of a node in a hierarchical or structured system.
    /// </summary>
    [VectorStoreData(StorageName = "node_type", IsIndexed = true)]
    public string? NodeType { get; set; }

    /// <summary>
    /// Gets or sets the information regarding the parent entity or object.
    /// </summary>
    [VectorStoreData(StorageName = "parent_info")]
    public string? ParentInfo { get; set; }
}

/// <summary>
/// Represents global XML-based text data for processing and analysis.
/// </summary>
/// <typeparam name="TKey">The type of the key used to uniquely identify the XML text data.</typeparam>
[Experimental("SKEXP0001")]
public class AesirGlobalXmlTextData<TKey> : AesirGlobalDocumentTextData<TKey>, IXmlTextData
{
    /// <summary>
    /// Gets or sets the XML path used to locate or identify the specific data or node within an XML structure.
    /// </summary>
    [VectorStoreData(StorageName = "xml_path", IsFullTextIndexed = true, IsIndexed = true)]
    public string? XmlPath { get; set; }

    /// <summary>
    /// Gets or sets the type of the node, representing its classification or category.
    /// </summary>
    [VectorStoreData(StorageName = "node_type", IsIndexed = true)]
    public string? NodeType { get; set; }

    /// <summary>
    /// Gets or sets the information related to the parent entity or object.
    /// </summary>
    [VectorStoreData(StorageName = "parent_info")]
    public string? ParentInfo { get; set; }
}

/// <summary>
/// Defines a contract for text data sourced from CSV files, including properties
/// for text content, the original CSV path, node type, and parent information.
/// This interface is intended to support scenarios involving wide or complex CSV
/// structures by enabling text chunking and metadata retention, often utilized
/// in retrieval-augmented generation (RAG) pipelines.
/// </summary>
public interface ICsvTextData
{
    /// <summary>
    /// Gets or sets the textual content associated with this data object.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the path to the CSV file associated with the text data.
    /// This property is used to identify and locate the original CSV file
    /// from which the text data was parsed or extracted.
    /// </summary>
    public string? CsvPath { get; set; }

    /// <summary>
    /// Gets or sets the type of the node associated with the text data, providing
    /// metadata that can be used for categorization or hierarchical structure within
    /// the text data model.
    /// </summary>
    public string? NodeType { get; set; }

    /// <summary>
    /// Gets or sets the metadata related to the parent of this data entity.
    /// This property is used to store information about the hierarchical
    /// relationship or origin within its data structure.
    /// </summary>
    public string? ParentInfo { get; set; }
}

/// <summary>
/// Represents conversation data sourced from CSV input, structured for processing in Aesir operations.
/// </summary>
[Experimental("SKEXP0001")]
public class AesirConversationCsvTextData<TKey> : AesirConversationDocumentTextData<TKey>, ICsvTextData
{
    /// <summary>
    /// Gets or sets the file path to the CSV document associated with this text data.
    /// </summary>
    [VectorStoreData(StorageName = "csv_path", IsFullTextIndexed = true, IsIndexed = true)]
    public string? CsvPath { get; set; }

    /// <summary>
    /// Gets or sets the type identifier for the node, representing its classification
    /// or role within the context of the associated CSV text data.
    /// </summary>
    [VectorStoreData(StorageName = "node_type", IsIndexed = true)]
    public string? NodeType { get; set; }

    /// <summary>
    /// Gets or sets the metadata or identifier of the parent entity associated
    /// with the current text data record. This allows for hierarchical or
    /// relational mapping of child records to their parent context.
    /// </summary>
    [VectorStoreData(StorageName = "parent_info")]
    public string? ParentInfo { get; set; }
}

/// <summary>
/// Represents global CSV text data with metadata for semantic search and retrieval operations.
/// Inherits from <see cref="AesirGlobalDocumentTextData{TKey}" /> and implements <see cref="ICsvTextData" />.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify the text data.</typeparam>
[Experimental("SKEXP0001")]
public class AesirGlobalCsvTextData<TKey> : AesirGlobalDocumentTextData<TKey>, ICsvTextData
{
    /// <summary>
    /// Gets or sets the file path of the CSV file to be processed.
    /// </summary>
    [VectorStoreData(StorageName = "csv_path", IsFullTextIndexed = true, IsIndexed = true)]
    public string? CsvPath { get; set; }

    /// <summary>
    /// Gets or sets the type of the node associated with the text data,
    /// used to represent a specific categorization or role within the data structure.
    /// </summary>
    [VectorStoreData(StorageName = "node_type", IsIndexed = true)]
    public string? NodeType { get; set; }

    /// <summary>
    /// Gets or sets the information related to the parent entity or object.
    /// </summary>
    [VectorStoreData(StorageName = "parent_info")]
    public string? ParentInfo { get; set; }
}