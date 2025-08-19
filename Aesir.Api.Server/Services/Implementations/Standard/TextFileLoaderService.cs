using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Aesir.Common.FileTypes;
using Aesir.Api.Server.Models;
using Markdig;
using Markdig.Renderers.Roundtrip;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Provides functionality to load and process text files into a structured format.
/// </summary>
/// <typeparam name="TKey">
/// The type of the unique identifier for the records. Must be non-nullable.
/// </typeparam>
/// <typeparam name="TRecord">
/// The type of the record used to store text content. Must derive from <see cref="AesirTextData{TKey}"/>.
/// </typeparam>
[Experimental("SKEXP0001")]
public class TextFileLoaderService<TKey, TRecord>(
    UniqueKeyGenerator<TKey> uniqueKeyGenerator,
    VectorStoreCollection<TKey, TRecord> vectorStoreRecordCollection,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    Func<RawContent, LoadTextFileRequest, TRecord> recordFactory,
    IModelsService modelsService,
    ILogger<TextFileLoaderService<TKey, TRecord>> logger
) : BaseDataLoaderService<TKey, TRecord>(uniqueKeyGenerator, vectorStoreRecordCollection, embeddingGenerator, modelsService, logger), ITextFileLoaderService<TKey, TRecord>
    where TKey : notnull
    where TRecord : AesirTextData<TKey>
{
    private readonly Func<RawContent, LoadTextFileRequest, TRecord> _recordFactory = recordFactory;

    /// <summary>
    /// Abstract base class for content type handlers.
    /// </summary>
    private abstract class ContentTypeHandler
    {
        public abstract Task<TRecord[]> ProcessContentAsync(
            string content,
            LoadTextFileRequest request,
            Func<RawContent, LoadTextFileRequest, TRecord> recordFactory);
    }

    /// <summary>
    /// Handler for JSON content processing.
    /// </summary>
    private class JsonContentHandler : ContentTypeHandler
    {
        public override async Task<TRecord[]> ProcessContentAsync(
            string content,
            LoadTextFileRequest request,
            Func<RawContent, LoadTextFileRequest, TRecord> recordFactory)
        {
            var rawContent = new RawContent
            {
                Text = content,
                PageNumber = 1
            };

            IJsonTextData CreateJsonRecord()
            {
                var record = recordFactory(rawContent, request);
                return record is not IJsonTextData data
                    ? throw new InvalidOperationException("Record is not of type IJsonTextData")
                    : data;
            }

            var jsonConverter = new JsonToAesirTextDataConverter<IJsonTextData>();
            return (await jsonConverter.ConvertJsonAsync(CreateJsonRecord, content, request.TextFileFileName).ConfigureAwait(false))
                .Cast<TRecord>().ToArray();
        }
    }

    /// <summary>
    /// Handler for XML content processing.
    /// </summary>
    private class XmlContentHandler : ContentTypeHandler
    {
        public override async Task<TRecord[]> ProcessContentAsync(
            string content,
            LoadTextFileRequest request,
            Func<RawContent, LoadTextFileRequest, TRecord> recordFactory)
        {
            var rawContent = new RawContent
            {
                Text = content,
                PageNumber = 1
            };

            IXmlTextData CreateXmlRecord()
            {
                var record = recordFactory(rawContent, request);
                return record is not IXmlTextData data
                    ? throw new InvalidOperationException("Record is not of type IXmlTextData")
                    : data;
            }

            var xmlConverter = new XmlToAesirTextDataConverter<IXmlTextData>();
            return (await xmlConverter.ConvertXmlAsync(CreateXmlRecord, content, request.TextFileFileName).ConfigureAwait(false))
                .Cast<TRecord>().ToArray();
        }
    }

    /// <summary>
    /// Handler for default text content processing (plain text, markdown, HTML).
    /// </summary>
    private class DefaultContentHandler : ContentTypeHandler
    {
        public override Task<TRecord[]> ProcessContentAsync(
            string content,
            LoadTextFileRequest request,
            Func<RawContent, LoadTextFileRequest, TRecord> recordFactory)
        {
            var rawContent = new RawContent
            {
                Text = content,
                PageNumber = 1
            };

            var textChunks = DocumentChunker.ChunkText(rawContent.Text!,
                $"File: {request.TextFileFileName}\nPage: {rawContent.PageNumber}");

            var records = textChunks.Select(chunk =>
            {
                var chunkContent = new RawContent
                {
                    Text = chunk,
                    PageNumber = 1
                };

                var record = recordFactory(chunkContent, request);
                record.Text ??= chunk;
                return record;
            }).ToArray();

            return Task.FromResult(records);
        }
    }

    /// Asynchronously loads a text file, processes its contents, and integrates the extracted data into a vector store collection.
    /// <param name="request">
    /// An instance of <see cref="LoadTextFileRequest"/> containing the details of the text file, such as its path, name, and additional properties.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests, enabling the operation to be canceled.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the provided <paramref name="request"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the specified file path or file content is invalid or cannot be processed.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown if the file type is unsupported for processing.
    /// </exception>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    public async Task LoadTextFileAsync(LoadTextFileRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        var actualContentType = GetAndValidateContentType(request.TextFileFileName!);
        
        await InitializeCollectionAsync(cancellationToken);
        await DeleteExistingRecordsAsync(request.TextFileFileName!, cancellationToken);
        
        var fileContent = await GetTextRawContentAsync(request.TextFileLocalPath!);
        var content = await ConvertContentAsync(fileContent, actualContentType);
        
        var handler = CreateContentHandler(actualContentType);
        var records = await handler.ProcessContentAsync(content, request, _recordFactory);
        var processedRecords = await ProcessRecordsInBatchesAsync(records, request.TextFileFileName!, request.BatchSize, cancellationToken);
        
        await UpsertRecordsAsync(processedRecords, cancellationToken);
        await UnloadModelsAsync();
    }

    /// <summary>
    /// Validates the request parameters.
    /// </summary>
    private static void ValidateRequest(LoadTextFileRequest request)
    {
        if (string.IsNullOrEmpty(request.TextFileLocalPath))
            throw new InvalidOperationException("TextFileLocalPath is empty");
        if (string.IsNullOrEmpty(request.TextFileFileName))
            throw new InvalidOperationException("TextFileFileName is empty");
    }

    /// <summary>
    /// Gets and validates the content type of the file.
    /// </summary>
    private static string GetAndValidateContentType(string fileName)
    {
        var supportedFileContentTypes = GetSupportedFileContentTypes();
        if (!fileName.ValidFileContentType(out var actualContentType, supportedFileContentTypes))
            throw new NotSupportedException(
                $"Only [{string.Join(", ", supportedFileContentTypes)}] files are currently supported and not: {actualContentType}");
        return actualContentType;
    }

    /// <summary>
    /// Converts file content based on the content type.
    /// </summary>
    private static async Task<string> ConvertContentAsync(string fileContent, string contentType)
    {
        return contentType switch
        {
            SupportedFileContentTypes.PlainTextContentType => await new PlainTextToMarkdownConverter().ConvertAsync(fileContent),
            SupportedFileContentTypes.MarkdownContentType => await new MarkdownToMarkdownConverter().ConvertAsync(fileContent),
            SupportedFileContentTypes.HtmlContentType => await new HtmlToMarkdownConverter().ConvertAsync(fileContent),
            SupportedFileContentTypes.JsonContentType => fileContent,
            SupportedFileContentTypes.XmlContentType => fileContent,
            _ => throw new NotSupportedException($"Content type {contentType} is not supported")
        };
    }

    /// <summary>
    /// Creates the appropriate content handler based on content type.
    /// </summary>
    private static ContentTypeHandler CreateContentHandler(string contentType)
    {
        return contentType switch
        {
            SupportedFileContentTypes.JsonContentType => new JsonContentHandler(),
            SupportedFileContentTypes.XmlContentType => new XmlContentHandler(),
            SupportedFileContentTypes.CsvContentType => throw new NotImplementedException(),
            _ => new DefaultContentHandler()
        };
    }


    /// Retrieves the list of file content types that are supported by the service.
    /// <returns>
    /// An array of strings representing the supported file content types.
    /// </returns>
    protected static string[] GetSupportedFileContentTypes()
    {
        return new[]
        {
            SupportedFileContentTypes.PlainTextContentType,
            SupportedFileContentTypes.MarkdownContentType,
            SupportedFileContentTypes.HtmlContentType,
            SupportedFileContentTypes.XmlContentType,
            SupportedFileContentTypes.JsonContentType
        };
    }

    /// Asynchronously retrieves the raw text content from a specified file path.
    /// <param name="textFileLocalPath">
    /// The local file path of the text file from which raw content is to be retrieved.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result contains the raw text content read from the file.
    /// </returns>
    protected virtual async Task<string> GetTextRawContentAsync(string textFileLocalPath)
    {
        return await File.ReadAllTextAsync(textFileLocalPath);
    }
}

/// <summary>
/// Converts plain text into Markdown-formatted text.
/// </summary>
/// <remarks>
/// This class provides functionality for transforming plain text input into its Markdown-formatted equivalent
/// based on standard Markdown formatting rules.
/// </remarks>
internal class PlainTextToMarkdownConverter
{
    /// <summary>
    /// An instance of the <see cref="MarkdownPipeline"/> pre-configured with advanced extensions
    /// to enhance markdown processing, enabling comprehensive parsing and transformation of content.
    /// </summary>
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// Converts plain text into Markdown format asynchronously.
    /// <param name="plainText">
    /// The plain text content to be converted.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result contains the converted Markdown content as a string.
    /// </returns>
    public async Task<string> ConvertAsync(string plainText)
    {
        return await Task.FromResult(Markdown.ToPlainText(plainText, _pipeline));
    }
}

/// <summary>
/// Provides functionality to process and convert markdown text while preserving its markdown structure.
/// </summary>
internal class MarkdownToMarkdownConverter
{
    /// <summary>
    /// A private readonly instance of the <see cref="MarkdownPipeline"/>, configured to utilize
    /// advanced markdown processing extensions. Used for parsing and rendering markdown content
    /// within the <see cref="MarkdownToMarkdownConverter"/> class.
    /// </summary>
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// Asynchronously converts the input markdown text into a processed markdown text format.
    /// <param name="markdownText">
    /// The original markdown text to be converted.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// The task result contains the processed markdown text as a string.
    /// </returns>
    public async Task<string> ConvertAsync(string markdownText)
    {
        var document = Markdown.Parse(markdownText, _pipeline);

        await using var writer = new StringWriter();

        var renderer = new RoundtripRenderer(writer);
        _pipeline.Setup(renderer);
        renderer.Write(document);

        return await Task.FromResult(writer.ToString());
    }
}

/// <summary>
/// Provides functionality to convert HTML content into Markdown format.
/// </summary>
/// <remarks>
/// This class utilizes the ReverseMarkdown library for the conversion process
/// and supports customizable configurations for handling various HTML and Markdown-related behaviors.
/// </remarks>
internal class HtmlToMarkdownConverter
{
    /// Converts the provided HTML content to its Markdown equivalent using the specified configuration.
    /// <param name="html">
    /// The input HTML string to be converted to Markdown.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation that returns the converted Markdown string.
    /// </returns>
    public async Task<string> ConvertAsync(string html)
    {
        var config = new ReverseMarkdown.Config
        {
            UnknownTags = ReverseMarkdown.Config.UnknownTagsOption.Bypass,
            GithubFlavored = true,
            RemoveComments = true,
            SmartHrefHandling = true,
            TableWithoutHeaderRowHandling = ReverseMarkdown.Config.TableWithoutHeaderRowHandlingOption.EmptyRow
        };
        var converter = new ReverseMarkdown.Converter(config);

        return await Task.FromResult(converter.Convert(html));
    }
}

/// <summary>
/// A utility class that facilitates the conversion of JSON content into structured Aesir text data records.
/// This conversion involves flattening hierarchical JSON structures and mapping them to instances
/// of the specified type <typeparamref name="TRecord"/>.
/// </summary>
/// <typeparam name="TRecord">
/// The type of text data record that must implement the <see cref="IJsonTextData"/> interface.
/// </typeparam>
[Experimental("SKEXP0001")]
internal class JsonToAesirTextDataConverter<TRecord> where TRecord : IJsonTextData
{
    /// <summary>
    /// A static utility for splitting large text documents into smaller chunks
    /// based on token count constraints, such as tokens per paragraph and per line.
    /// Provides methods to facilitate text chunking operations while preserving structural integrity.
    /// </summary>
    private static readonly DocumentChunker DocumentChunker = new();

    /// Converts a JSON string into a list of records of the specified type asynchronously.
    /// <param name="recordFactory">
    /// A function that provides an instance of the record type for creating records from the JSON data.
    /// </param>
    /// <param name="jsonContent">
    /// A string containing the JSON data to be parsed and converted into records.
    /// </param>
    /// <param name="filename">
    /// The name of the file from which the JSON content originates, used for contextual processing.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result contains a list of records parsed from the JSON content.
    /// </returns>
    public async Task<List<TRecord>> ConvertJsonAsync(
        Func<TRecord> recordFactory, string jsonContent, string filename)
    {
        var root = JsonNode.Parse(jsonContent);
        List<TRecord> records = [];

        Flatten(root);

        return records;

        void Flatten(JsonNode? node, string prefix = "", string parentType = "root")
        {
            var nodeType = node switch
            {
                JsonObject => "object",
                JsonArray => "array",
                JsonValue => "value",
                _ => "unknown"
            };

            if (node is JsonObject obj)
            {
                foreach (var prop in obj)
                {
                    var jsonKey = string.IsNullOrEmpty(prefix) ? prop.Key : $"{prefix}:{prop.Key}";

                    if (prop.Value is JsonValue val)
                    {
                        var jsonValString = val.ToString();
                        var chunks = DocumentChunker.ChunkText(jsonValString, $"Key: {jsonKey}=");

                        foreach (var chunk in chunks)
                        {
                            var record = recordFactory();
                            record.Text = chunk;
                            record.JsonPath = jsonKey;
                            record.NodeType = "value";
                            record.ParentInfo = parentType;

                            records.Add(record);
                        }
                    }
                    else
                    {
                        Flatten(prop.Value, jsonKey, nodeType); // Recurse with updated parent
                    }
                }
            }
            else if (node is JsonArray arr)
            {
                for (var i = 0; i < arr.Count; i++)
                {
                    var jsonKey = $"{prefix}[{i}]";
                    if (arr[i] is JsonValue val)
                    {
                        var jsonValString = val.ToString();
                        var chunks = DocumentChunker.ChunkText(jsonValString, $"File: {filename}\nKey: {jsonKey}=");

                        foreach (var chunk in chunks)
                        {
                            var record = recordFactory();
                            record.Text = chunk;
                            record.JsonPath = jsonKey;
                            record.NodeType = "value";
                            record.ParentInfo = parentType;

                            records.Add(record);
                        }
                    }
                    else
                    {
                        Flatten(arr[i], jsonKey, nodeType);
                    }
                }
            }
            else if (node is JsonValue rootVal && string.IsNullOrEmpty(prefix))
            {
                var jsonValString = rootVal.ToString();
                var chunks = DocumentChunker.ChunkText(jsonValString, $"File: {filename}\nKey: root=");
                foreach (var chunk in chunks)
                {
                    var record = recordFactory();
                    record.Text = chunk;
                    record.JsonPath = "root";
                    record.NodeType = "value";
                    record.ParentInfo = "none";

                    records.Add(record);
                }
            }
        }
    }
}

/// <summary>
/// Provides functionality to convert XML content into a structured representation of Aesir text data records.
/// </summary>
/// <typeparam name="TRecord">
/// The type of the records being generated from the XML content. Must implement <see cref="IXmlTextData"/>.
/// </typeparam>
[Experimental("SKEXP0001")]
internal class XmlToAesirTextDataConverter<TRecord> where TRecord : IXmlTextData
{
    /// <summary>
    /// A utility class utilized for splitting large text documents into smaller, manageable chunks
    /// based on configurable token limits, aiding in efficient text processing workflows.
    /// </summary>
    private static readonly DocumentChunker DocumentChunker = new();

    /// Asynchronously converts the provided XML content into a collection of records using the specified record factory.
    /// Each record represents flattened XML content with associated metadata, including path, node type, and parent information.
    /// <param name="recordFactory">
    /// A factory function used to create instances of <typeparamref name="TRecord"/>.
    /// </param>
    /// <param name="xmlContent">
    /// The XML content as a string to be parsed and converted into records.
    /// </param>
    /// <param name="filename">
    /// The name of the XML file being processed. This may be used for logging or additional metadata.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation that contains a list of records of type <typeparamref name="TRecord"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="recordFactory"/> or <paramref name="xmlContent"/> is null.
    /// </exception>
    /// <exception cref="System.Xml.XmlException">
    /// Thrown if <paramref name="xmlContent"/> contains invalid XML and cannot be parsed.
    /// </exception>
    public async Task<List<TRecord>> ConvertXmlAsync(Func<TRecord> recordFactory, string xmlContent, string filename)
    {
        var document = XDocument.Parse(xmlContent);
        List<TRecord> records = [];
        Flatten(document.Root, "");
        return records;

        void Flatten(XElement? element, string prefix, string parentType = "root")
        {
            if (element == null) return;

            var nodeType = "element";
            var elementName = element.Name.LocalName;
            var xmlPath = string.IsNullOrEmpty(prefix) ? elementName : $"{prefix}/{elementName}";

            // Process attributes
            foreach (var attr in element.Attributes())
            {
                var attrPath = $"{xmlPath}/@{attr.Name.LocalName}";
                var chunks = DocumentChunker.ChunkText(attr.Value, $"File: {filename}\nAttribute: {attrPath}=");
                foreach (var chunk in chunks)
                {
                    var record = recordFactory();
                    record.Text = chunk;
                    record.XmlPath = attrPath; // Using JsonPath for consistency with JSON converter
                    record.NodeType = "value";
                    record.ParentInfo = nodeType;
                    records.Add(record);
                }
            }

            // Process text content
            var textContent = element.Value.Trim();
            if (!string.IsNullOrEmpty(textContent) && !element.HasElements)
            {
                var chunks = DocumentChunker.ChunkText(textContent, $"File: {filename}\nElement: {xmlPath}=");
                foreach (var chunk in chunks)
                {
                    var record = recordFactory();
                    record.Text = chunk;
                    record.XmlPath = xmlPath;
                    record.NodeType = "value";
                    record.ParentInfo = nodeType;
                    records.Add(record);
                }
            }

            // Process child elements
            foreach (var child in element.Elements())
            {
                Flatten(child, xmlPath, nodeType);
            }
        }
    }
}