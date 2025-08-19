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
/// Provides functionality to load and process text files into a structured format by converting raw content into domain-specific record representations.
/// </summary>
/// <typeparam name="TKey">
/// The type of the unique identifier for the records. Must be a non-nullable type.
/// </typeparam>
/// <typeparam name="TRecord">
/// The type of the record used to store processed text content. Must inherit from <see cref="AesirTextData{TKey}"/>.
/// </typeparam>
/// <remarks>
/// This class integrates functionality for generating unique identifiers, managing vector-based storage, generating embeddings, and processing model-driven operations.
/// </remarks>
[Experimental("SKEXP0001")]
public class TextFileLoaderService<TKey, TRecord>(
    UniqueKeyGenerator<TKey> uniqueKeyGenerator,
    VectorStoreCollection<TKey, TRecord> vectorStoreRecordCollection,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    Func<RawContent, LoadTextFileRequest, TRecord> recordFactory,
    IModelsService modelsService,
    ILogger<TextFileLoaderService<TKey, TRecord>> logger
) : BaseDataLoaderService<TKey, TRecord>(uniqueKeyGenerator, vectorStoreRecordCollection, embeddingGenerator,
    modelsService, logger), ITextFileLoaderService<TKey, TRecord>
    where TKey : notnull
    where TRecord : AesirTextData<TKey>
{
    /// <summary>
    /// A factory delegate used to create instances of <typeparamref name="TRecord"/> objects,
    /// based on the raw content and file loading request provided.
    /// Facilitates the structured transformation of raw text content into strongly typed record representations.
    /// </summary>
    private readonly Func<RawContent, LoadTextFileRequest, TRecord> _recordFactory = recordFactory;

    /// <summary>
    /// Serves as an abstract base class for handling different content types during text file processing.
    /// </summary>
    private abstract class ContentTypeHandler
    {
        /// Asynchronously processes the provided raw content using a specified content handler, transforming it into a collection of records.
        /// <param name="content">
        /// The raw content to be processed.
        /// </param>
        /// <param name="request">
        /// An instance of <see cref="LoadTextFileRequest"/> containing detailed information about the loading operation,
        /// such as metadata and configuration parameters.
        /// </param>
        /// <param name="recordFactory">
        /// A function used to create instances of <typeparamref name="TRecord"/>
        /// using the provided raw content and request details.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if any required argument, such as <paramref name="content"/>, <paramref name="request"/>,
        /// or <paramref name="recordFactory"/>, is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the content cannot be processed or does not follow a valid format.
        /// </exception>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains
        /// an array of <typeparamref name="TRecord"/> objects generated from the processed content.
        /// </returns>
        public abstract Task<TRecord[]> ProcessContentAsync(
            string content,
            LoadTextFileRequest request,
            Func<RawContent, LoadTextFileRequest, TRecord> recordFactory);
    }

    /// <summary>
    /// Provides specialized handling and processing of JSON content within the context of loading text files.
    /// </summary>
    private class JsonContentHandler : ContentTypeHandler
    {
        /// Asynchronously processes the content of a text file and converts it into records using the provided record factory.
        /// <param name="content">
        /// The string content of the text file to be processed.
        /// </param>
        /// <param name="request">
        /// An instance of <see cref="LoadTextFileRequest"/> containing metadata about the text file, such as name and other properties.
        /// </param>
        /// <param name="recordFactory">
        /// A function that creates a record of type <typeparamref name="TRecord"/> using instances of <see cref="RawContent"/> and <see cref="LoadTextFileRequest"/>.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the record created by the factory is not of type <see cref="IJsonTextData"/>.
        /// </exception>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation that returns an array of records of type <typeparamref name="TRecord"/>.
        /// </returns>
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
            return (await jsonConverter.ConvertJsonAsync(CreateJsonRecord, content, request.TextFileFileName)
                    .ConfigureAwait(false))
                .Cast<TRecord>().ToArray();
        }
    }

    /// <summary>
    /// Provides an implementation for handling XML content within the data loading pipeline.
    /// </summary>
    private class XmlContentHandler : ContentTypeHandler
    {
        /// Asynchronously processes XML content and transforms it into an array of records.
        /// <param name="content">
        /// The XML content in string format to process.
        /// </param>
        /// <param name="request">
        /// An instance of <see cref="LoadTextFileRequest"/> containing details about the file, such as the file name.
        /// </param>
        /// <param name="recordFactory">
        /// A factory method that creates instances of <see cref="TRecord"/> based on the provided raw content and request.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the record created by the factory is not of type <see cref="IXmlTextData"/>.
        /// </exception>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation, containing an array of <see cref="TRecord"/> objects created from the XML content.
        /// </returns>
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
            return (await xmlConverter.ConvertXmlAsync(CreateXmlRecord, content, request.TextFileFileName)
                    .ConfigureAwait(false))
                .Cast<TRecord>().ToArray();
        }
    }

    /// <summary>
    /// Handles processing of default content types such as plain text, markdown, and HTML.
    /// </summary>
    private class DefaultContentHandler : ContentTypeHandler
    {
        /// Processes the provided text content, chunks it into manageable parts, and generates records using the specified factory function.
        /// <param name="content">
        /// The text content to be processed.
        /// </param>
        /// <param name="request">
        /// An instance of <see cref="LoadTextFileRequest"/> containing details about the original text file.
        /// </param>
        /// <param name="recordFactory">
        /// A factory function used to generate records from the chunked text and the provided file request details.
        /// </param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> containing an array of <typeparamref name="TRecord"/> representing the processed records.
        /// </returns>
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

    /// Asynchronously loads and processes a text file, storing its content into a vector store collection.
    /// <param name="request">
    /// An instance of <see cref="LoadTextFileRequest"/> containing details of the text file, such as its filename, file path, and additional processing configurations.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests, allowing the operation to be canceled.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="request"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the content type or file content cannot be correctly identified or processed.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the specified file path does not exist or is inaccessible.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the file's format or content type is unsupported.
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
        var processedRecords =
            await ProcessRecordsInBatchesAsync(records, request.TextFileFileName!, request.BatchSize,
                cancellationToken);

        await UpsertRecordsAsync(processedRecords, cancellationToken);
        await UnloadModelsAsync();
    }

    /// Validates the request parameters ensuring they meet the required criteria.
    /// <param name="request">
    /// An instance of <see cref="LoadTextFileRequest"/> representing the details of the request,
    /// including the local file path and file name of the text file.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the <see cref="LoadTextFileRequest.TextFileLocalPath"/> or
    /// <see cref="LoadTextFileRequest.TextFileFileName"/> is null or empty.
    /// </exception>
    private static void ValidateRequest(LoadTextFileRequest request)
    {
        if (string.IsNullOrEmpty(request.TextFileLocalPath))
            throw new InvalidOperationException("TextFileLocalPath is empty");
        if (string.IsNullOrEmpty(request.TextFileFileName))
            throw new InvalidOperationException("TextFileFileName is empty");
    }

    /// <summary>
    /// Determines and validates the content type of a given file based on its name.
    /// </summary>
    /// <param name="fileName">
    /// The name or path of the file to validate.
    /// </param>
    /// <exception cref="NotSupportedException">
    /// Thrown if the file's content type is not among the supported types.
    /// </exception>
    /// <returns>
    /// A string representing the validated content type of the file.
    /// </returns>
    private static string GetAndValidateContentType(string fileName)
    {
        var supportedFileContentTypes = GetSupportedFileContentTypes();
        if (!fileName.ValidFileContentType(out var actualContentType, supportedFileContentTypes))
            throw new NotSupportedException(
                $"Only [{string.Join(", ", supportedFileContentTypes)}] files are currently supported and not: {actualContentType}");
        return actualContentType;
    }

    /// Asynchronously converts the provided file content into a standardized format based on its content type.
    /// <param name="fileContent">
    /// The raw file content as a string that requires conversion.
    /// </param>
    /// <param name="contentType">
    /// The content type of the file, determining the appropriate conversion process. Supported content types include plain text, Markdown, HTML, JSON, and XML.
    /// </param>
    /// <exception cref="NotSupportedException">
    /// Thrown when the specified <paramref name="contentType"/> is unsupported for conversion.
    /// </exception>
    /// <returns>
    /// A <see cref="Task{String}"/> representing the asynchronous conversion operation. The result is the standardized content as a string.
    /// </returns>
    private static async Task<string> ConvertContentAsync(string fileContent, string contentType)
    {
        return contentType switch
        {
            SupportedFileContentTypes.PlainTextContentType => await new PlainTextToMarkdownConverter().ConvertAsync(
                fileContent),
            SupportedFileContentTypes.MarkdownContentType => await new MarkdownToMarkdownConverter().ConvertAsync(
                fileContent),
            SupportedFileContentTypes.HtmlContentType => await new HtmlToMarkdownConverter().ConvertAsync(fileContent),
            SupportedFileContentTypes.JsonContentType => fileContent,
            SupportedFileContentTypes.XmlContentType => fileContent,
            _ => throw new NotSupportedException($"Content type {contentType} is not supported")
        };
    }

    /// <summary>
    /// Creates the appropriate content handler based on the specified content type.
    /// </summary>
    /// <param name="contentType">
    /// A string representing the MIME type of the content (e.g., JSON, XML, or CSV).
    /// </param>
    /// <exception cref="NotImplementedException">
    /// Thrown if the content type is CSV, indicating that a handler for this file type is not yet implemented.
    /// </exception>
    /// <returns>
    /// An instance of a class derived from <see cref="ContentTypeHandler"/> that handles the specified content type.
    /// </returns>
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
    /// The local file path of the text file from which the raw content will be retrieved.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result contains the raw text content of the file as a string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the provided <paramref name="textFileLocalPath"/> is null or empty.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown if the file specified by <paramref name="textFileLocalPath"/> does not exist.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if an I/O error occurs while attempting to read the file.
    /// </exception>
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
/// based on standard Markdown conversion rules.
/// </remarks>
internal class PlainTextToMarkdownConverter
{
    /// <summary>
    /// An instance of the <see cref="MarkdownPipeline"/> configured with advanced extensions
    /// to facilitate markdown processing, supporting detailed parsing and formatting of markdown content.
    /// </summary>
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>
    /// Asynchronously converts plain text into Markdown-formatted text.
    /// </summary>
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
    /// A private readonly instance of the <see cref="MarkdownPipeline"/> configured with advanced
    /// extensions for enhanced processing of markdown content, utilized specifically within the
    /// <see cref="MarkdownToMarkdownConverter"/> to parse and render markdown documents.
    /// </summary>
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>
    /// Asynchronously converts the input markdown text into a processed markdown text format.
    /// </summary>
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
/// This class leverages the ReverseMarkdown library for the conversion process to transform HTML
/// into Markdown text. It supports various configuration options to customize the output,
/// such as handling unknown tags, enabling GitHub-flavored Markdown, and managing table formatting.
/// </remarks>
internal class HtmlToMarkdownConverter
{
    /// Converts the provided content from one format to Markdown based on the specific content type.
    /// <param name="fileContent">
    /// The input content as a string to be converted.
    /// </param>
    /// <param name="contentType">
    /// The type of the input content, such as plain text, HTML, or markdown.
    /// </param>
    /// <exception cref="NotSupportedException">
    /// Thrown if the specified <paramref name="contentType"/> is not supported for conversion.
    /// </exception>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation that returns the converted content as a Markdown string.
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
/// A utility class designed for converting JSON content into structured instances of <typeparamref name="TRecord"/>.
/// This conversion process involves flattening complex JSON data and mapping it to the specified record type,
/// providing a standardized format for text data processing.
/// </summary>
/// <typeparam name="TRecord">
/// The type of the text data record to which the JSON content will be mapped. Must implement the <see cref="IJsonTextData"/> interface.
/// </typeparam>
[Experimental("SKEXP0001")]
internal class JsonToAesirTextDataConverter<TRecord> where TRecord : IJsonTextData
{
    /// <summary>
    /// A static instance of the <see cref="DocumentChunker"/> class, configured for splitting large text documents
    /// into manageable chunks based on specified token constraints, such as tokens per paragraph and tokens per line.
    /// This utility aims to preserve the contextual integrity of the original document while ensuring optimized text handling.
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
    /// A utility class designed to split large text documents into smaller chunks
    /// for efficient text processing, based on specified token limits.
    /// </summary>
    private static readonly DocumentChunker DocumentChunker = new();

    /// Asynchronously converts XML content into a collection of records using a specified factory function,
    /// with each record representing flattened XML structures enriched with metadata such as node type and parent information.
    /// <param name="recordFactory">
    /// A factory function responsible for creating instances of <typeparamref name="TRecord"/>.
    /// </param>
    /// <param name="xmlContent">
    /// A string containing the XML content to be parsed and transformed into records.
    /// </param>
    /// <param name="filename">
    /// The name of the XML file being converted, potentially used for logging or additional metadata purposes.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation, resulting in a list of records of type <typeparamref name="TRecord"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="recordFactory"/> or <paramref name="xmlContent"/> is null.
    /// </exception>
    /// <exception cref="System.Xml.XmlException">
    /// Thrown when <paramref name="xmlContent"/> contains invalid XML that cannot be parsed.
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