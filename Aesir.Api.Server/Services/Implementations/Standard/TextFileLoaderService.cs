using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Aesir.Common.FileTypes;
using Aesir.Api.Server.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Markdig;
using Markdig.Renderers.Roundtrip;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Provides functionality for loading and processing text files, transforming raw file content into structured domain-specific record representations.
/// </summary>
/// <typeparam name="TKey">
/// The type of the unique identifier for the records. Must be a non-nullable type.
/// </typeparam>
/// <typeparam name="TRecord">
/// The type of the domain-specific record used to represent processed text content. Must inherit from <see cref="AesirTextData{TKey}"/>.
/// </typeparam>
/// <remarks>
/// This service is designed to integrate support for unique identifier generation, vector-based record storage, embedding generation, and model-driven operations.
/// It enables efficient processing of text file content while providing utility methods for content-type validation and asynchronous file loading.
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
    /// A delegate providing a mechanism to generate instances of <typeparamref name="TRecord"/>
    /// using raw content and file loading request parameters.
    /// Enables the conversion of raw input data into strongly typed record representations
    /// for further processing in the file loading pipeline.
    /// </summary>
    private readonly Func<RawContent, LoadTextFileRequest, TRecord> _recordFactory = recordFactory;

    /// <summary>
    /// Represents an abstract base class for processing content of various types during text file operations.
    /// Defines the structure for concrete handlers that process content based on specific formats, such as JSON, XML, or other types.
    /// </summary>
    /// <remarks>
    /// This class is designed to be extended by concrete implementations that provide logic for handling content
    /// of specific formats. Each derived handler should override the <see cref="ProcessContentAsync"/> method to
    /// implement content-specific processing.
    /// </remarks>
    private abstract class ContentTypeHandler
    {
        /// Asynchronously processes the provided content based on the specified parameters and transforms it into a collection of records.
        /// <param name="content">
        /// The raw content to be processed.
        /// </param>
        /// <param name="request">
        /// An instance of <see cref="LoadTextFileRequest"/> containing metadata and configuration details for the processing operation.
        /// </param>
        /// <param name="recordFactory">
        /// A function to create instances of <typeparamref name="TRecord"/> using the provided raw content and processing details.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if any required argument, such as <paramref name="content"/>, <paramref name="request"/>, or <paramref name="recordFactory"/>, is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the content does not meet expected requirements or fails to be processed correctly.
        /// </exception>
        /// <returns>
        /// A task representing the asynchronous processing operation. The task result contains
        /// an array of <typeparamref name="TRecord"/> objects derived from the processed content.
        /// </returns>
        public abstract Task<TRecord[]> ProcessContentAsync(
            string content,
            LoadTextFileRequest request,
            Func<RawContent, LoadTextFileRequest, TRecord> recordFactory);
    }

    /// <summary>
    /// Provides functionality for handling and processing JSON content during the text file loading process
    /// by converting JSON data into domain-specific record representations.
    /// </summary>
    /// <remarks>
    /// This class is a specialized implementation of <see cref="ContentTypeHandler"/>
    /// used to parse and process JSON content, enabling conversion to text data records using a JSON-to-domain data converter.
    /// </remarks>
    private class JsonContentHandler : ContentTypeHandler
    {
        /// Asynchronously processes the provided JSON-formatted content, converting it into a collection of records
        /// using the specified record factory and request details.
        /// <param name="content">
        /// The raw JSON content to be processed.
        /// </param>
        /// <param name="request">
        /// An instance of <see cref="LoadTextFileRequest"/> that includes metadata and configuration
        /// relevant to the content processing operation.
        /// </param>
        /// <param name="recordFactory">
        /// A delegate function used to create instances of <typeparamref name="TRecord"/>
        /// based on the provided raw content and request details.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="content"/>, <paramref name="request"/>, or <paramref name="recordFactory"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the JSON content is invalid, cannot be converted, or has an unsupported structure.
        /// </exception>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains
        /// an array of <typeparamref name="TRecord"/> objects created from the processed JSON content.
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
            return (await jsonConverter.ConvertJsonAsync(CreateJsonRecord, content, request.TextFileFileName!)
                    .ConfigureAwait(false))
                .Cast<TRecord>().ToArray();
        }
    }

    /// <summary>
    /// Handles the processing of XML content by converting it into domain-specific records within the data loading pipeline.
    /// </summary>
    /// <remarks>
    /// This class provides functionality to parse and transform XML content into structured format suitable for storage and further processing.
    /// </remarks>
    private class XmlContentHandler : ContentTypeHandler
    {
        /// Asynchronously processes the provided XML content using a content handler, transforming it into a collection of records.
        /// <param name="content">
        /// The XML content to be processed.
        /// </param>
        /// <param name="request">
        /// An instance of <see cref="LoadTextFileRequest"/> that contains metadata and configuration parameters required for processing the content.
        /// </param>
        /// <param name="recordFactory">
        /// A function for creating instances of <typeparamref name="TRecord"/> based on the provided raw content and request details.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if any of the arguments, such as <paramref name="content"/>, <paramref name="request"/>, or <paramref name="recordFactory"/>, is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the XML content cannot be processed or if it contains an invalid format.
        /// </exception>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains
        /// an array of <typeparamref name="TRecord"/> objects generated from the processed XML content.
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
            return (await xmlConverter.ConvertXmlAsync(CreateXmlRecord, content, request.TextFileFileName!)
                    .ConfigureAwait(false))
                .Cast<TRecord>().ToArray();
        }
    }

    /// <summary>
    /// Handles the processing of CSV content into domain-specific records.
    /// </summary>
    /// <remarks>
    /// This class is a specialized implementation for handling CSV-based content
    /// during the text file loading process. It utilizes a CSV-to-record converter
    /// to generate domain-specific record instances based on the provided content
    /// and configuration.
    /// </remarks>
    /// <typeparam name="TRecord">
    /// The type of the domain-specific record used for processing the content. Must inherit
    /// from <see cref="AesirTextData{TKey}"/>.
    /// </typeparam>
    private class CsvContentHandler : ContentTypeHandler
    {
        /// Asynchronously processes the provided CSV content using a specified content handler and record factory.
        /// <param name="content">
        /// The raw CSV content to be processed.
        /// </param>
        /// <param name="request">
        /// An instance of <see cref="LoadTextFileRequest"/> containing metadata and configuration settings
        /// required for processing the content.
        /// </param>
        /// <param name="recordFactory">
        /// A function used to create records of type <typeparamref name="TRecord"/> using the provided
        /// raw content and request details.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if any required argument, such as <paramref name="content"/>, <paramref name="request"/>,
        /// or <paramref name="recordFactory"/>, is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the CSV content cannot be processed or has an invalid format.
        /// </exception>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an array of
        /// <typeparamref name="TRecord"/> objects generated from the processed CSV content.
        /// </returns>
        public override async Task<TRecord[]> ProcessContentAsync(string content, LoadTextFileRequest request, Func<RawContent, LoadTextFileRequest, TRecord> recordFactory)
        {
            var rawContent = new RawContent
            {
                Text = content,
                PageNumber = 1
            };

            ICsvTextData CreateCsvRecord()
            {
                var record = recordFactory(rawContent, request);
                return record is not ICsvTextData data
                    ? throw new InvalidOperationException("Record is not of type ICsvTextData")
                    : data;
                
            }

            var csvConverter = new CsvToAesirTextDataConverter<ICsvTextData>();
            
            return (await csvConverter.ConvertCsvAsync(CreateCsvRecord, content, request.TextFileFileName!)
                    .ConfigureAwait(false))
                .Cast<TRecord>().ToArray();
        }
    }

    /// <summary>
    /// Provides functionality for processing default content types, such as plain text, markdown, and HTML,
    /// by dividing them into manageable chunks and converting them into records suitable for storage and processing.
    /// </summary>
    /// <remarks>
    /// This class is utilized as part of a content handling strategy for unstructured and semi-structured file types
    /// where no specialized content handler is provided. It processes content by dividing it into logical chunks
    /// and applying a factory method to transform these chunks into domain-specific records.
    /// </remarks>
    private class DefaultContentHandler : ContentTypeHandler
    {
        /// Asynchronously processes the provided text content by chunking it into smaller parts and transforming it
        /// into a collection of records using the specified factory function.
        /// <param name="content">
        /// The text content to be processed.
        /// </param>
        /// <param name="request">
        /// An instance of <see cref="LoadTextFileRequest"/> containing information about the original text file and
        /// related request details.
        /// </param>
        /// <param name="recordFactory">
        /// A function that generates instances of <typeparamref name="TRecord"/> using the provided text content chunks
        /// and file request details.
        /// </param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that represents the asynchronous operation. The task result contains an
        /// array of <typeparamref name="TRecord"/> representing the processed records.
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

    /// Asynchronously loads and processes a text file, converting its contents into a collection of records
    /// and storing these in the associated vector store collection.
    /// <param name="request">
    /// An instance of <see cref="LoadTextFileRequest"/> providing necessary details about the text file loading operation,
    /// such as the file name, local path, batch size, and other configuration parameters.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests, allowing the operation to be cancelled if requested.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="request"/> or any of its required properties is null.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown if the specified file does not exist at the provided path in <paramref name="request"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the content type cannot be determined or if loading fails due to format or operational constraints.
    /// </exception>
    /// <returns>
    /// A task that represents the asynchronous load operation. Upon completion, the text file's
    /// contents will have been processed and stored as records in the vector store.
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

    /// Asynchronously converts the provided file content into a standardized string format based on its content type.
    /// <param name="fileContent">
    /// The raw file content to be converted, provided as a string.
    /// </param>
    /// <param name="contentType">
    /// The content type of the file, which determines the conversion process. Supported types include PlainText, Markdown, HTML, JSON, XML, and CSV.
    /// </param>
    /// <exception cref="NotSupportedException">
    /// Thrown if the specified <paramref name="contentType"/> is not supported for conversion.
    /// </exception>
    /// <returns>
    /// A <see cref="Task{String}"/> that represents the asynchronous conversion operation. The result contains the converted, standardized content as a string.
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
            SupportedFileContentTypes.CsvContentType => fileContent,
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
            SupportedFileContentTypes.CsvContentType => new CsvContentHandler(),
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
            SupportedFileContentTypes.JsonContentType,
            SupportedFileContentTypes.CsvContentType
        };
    }

    /// Asynchronously retrieves the raw text content from the provided file path.
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
    /// A specialized <see cref="MarkdownPipeline"/> instance configured with advanced extensions
    /// to enhance Markdown parsing and rendering capabilities. Facilitates complex Markdown processing
    /// such as extended syntax support and advanced formatting functionalities.
    /// </summary>
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// Asynchronously converts plain text into Markdown-formatted text using a predefined Markdown pipeline configuration.
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
/// Provides functionality to process and convert markdown text while maintaining its original markdown structure.
/// </summary>
/// <remarks>
/// This class is designed to handle the conversion of markdown content in scenarios where the text must adhere
/// to its existing markdown syntax while ensuring proper processing and validation.
/// </remarks>
internal class MarkdownToMarkdownConverter
{
    /// <summary>
    /// A private readonly instance of the <see cref="MarkdownPipeline"/> configured with advanced
    /// extensions to facilitate parsing and rendering of markdown documents while preserving the
    /// markdown structure, specifically used within the <see cref="MarkdownToMarkdownConverter"/> class.
    /// </summary>
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// Asynchronously converts the input markdown content into a processed markdown format.
    /// <param name="markdownText">
    /// The markdown content to be converted.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
    /// The task result contains the processed markdown content as a string.
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
/// This class uses the ReverseMarkdown library to perform the conversion process,
/// transforming HTML input into Markdown-compatible output. It allows customization
/// through configuration for handling unknown tags, enabling GitHub-flavored Markdown,
/// managing table structures, and removing comments during the conversion.
/// </remarks>
internal class HtmlToMarkdownConverter
{
    /// Asynchronously converts the provided HTML content to Markdown format using specified conversion settings.
    /// <param name="html">
    /// The input HTML content to be converted to Markdown format.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="html"/> content is null.
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
/// Facilitates the conversion of JSON content into strongly typed instances of <typeparamref name="TRecord"/>.
/// This converter flattens and maps hierarchical JSON structures into a standardized format that implements <see cref="IJsonTextData"/>.
/// </summary>
/// <typeparam name="TRecord">
/// The type of the record to which JSON data will be transformed. Must adhere to the <see cref="IJsonTextData"/> interface.
/// </typeparam>
/// <remarks>
/// Primarily utilized for scenarios where JSON data needs to be processed and represented in a structured text data format
/// for further manipulation, analysis, or storage.
/// </remarks>
[Experimental("SKEXP0001")]
internal class JsonToAesirTextDataConverter<TRecord> where TRecord : IJsonTextData
{
    /// <summary>
    /// A utility class designed to split large text documents into smaller, manageable chunks
    /// based on configurable token constraints, such as tokens per paragraph and tokens per line.
    /// Aims to maintain contextual coherence for use in text processing and analysis.
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
    /// A task that represents the asynchronous operation. The task result contains a list of records of the specified type parsed from the JSON content.
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
/// Transforms XML content into domain-specific structured representations by converting it into Aesir text data records.
/// </summary>
/// <typeparam name="TRecord">
/// The type of the record to be generated from the XML input. Each record must implement <see cref="IXmlTextData"/>.
/// </typeparam>
/// <remarks>
/// This class streamlines the process of parsing raw XML content and mapping it to a defined structured format consistent with
/// Aesir text data representations. It ensures proper handling of hierarchical XML data through a recursive processing approach.
/// </remarks>
[Experimental("SKEXP0001")]
internal class XmlToAesirTextDataConverter<TRecord> where TRecord : IXmlTextData
{
    /// <summary>
    /// A utility instance of the <see cref="DocumentChunker"/> class, statically initialized
    /// to facilitate the efficient division of large text documents into smaller, manageable
    /// text chunks, optimized by customizable token limits for both paragraphs and lines.
    /// Enhances asynchronous processing tasks that require incremental handling of large content.
    /// </summary>
    private static readonly DocumentChunker DocumentChunker = new();

    /// Asynchronously converts XML content into a collection of records using a specified factory function,
    /// transforming and flattening the XML structure into a metadata-enriched format.
    /// <param name="recordFactory">
    /// A factory function responsible for creating instances of <typeparamref name="TRecord"/> from the XML content.
    /// </param>
    /// <param name="xmlContent">
    /// The XML content represented as a string to be parsed and converted into records.
    /// </param>
    /// <param name="filename">
    /// The name of the XML file being processed; can be used for metadata or logging purposes.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that, when completed, contains a list of <typeparamref name="TRecord"/> objects
    /// representing the processed and flattened XML content.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="recordFactory"/> or <paramref name="xmlContent"/> is null.
    /// </exception>
    /// <exception cref="System.Xml.XmlException">
    /// Thrown if <paramref name="xmlContent"/> is invalid and cannot be parsed into a valid XML structure.
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

/// <summary>
/// Provides functionality to convert CSV content into a domain-specific representation of text data through a customizable factory method.
/// </summary>
/// <typeparam name="TRecord">
/// The type of the record representing the processed CSV rows. Must implement <see cref="ICsvTextData"/>.
/// </typeparam>
/// <remarks>
/// This class processes raw CSV files by reading headers and rows, escaping markdown characters in cell data, and generating a text-based summary of the file structure.
/// It supports metadata generation, including column and row summaries, which are encapsulated as text within the resulting records.
/// </remarks>
[Experimental("SKEXP0001")]
internal class CsvToAesirTextDataConverter<TRecord> where TRecord : ICsvTextData
{
    /// <summary>
    /// Responsible for efficiently chunking and tokenizing large text content
    /// into manageable segments, preserving logical structure where possible.
    /// Provides utilities for counting tokens and partitioning text based on
    /// configurable parameters such as tokens per paragraph or tokens per line.
    /// Facilitates downstream processing of text data in size-restricted workflows.
    /// </summary>
    private static readonly DocumentChunker DocumentChunker = new();

    /// <summary>
    /// Specifies the maximum number of columns that can be included in a single chunk of processed data.
    /// Used to segment large datasets into smaller, manageable parts for efficient handling.
    /// </summary>
    private int MaxColumnsPerChunk { get; } = 10;

    /// <summary>
    /// Determines the maximum allowable number of tokens in a single chunk of text data when processing CSV content.
    /// Used to ensure that chunks adhere to a predefined token limit, facilitating efficient handling and processing
    /// of large or complex CSV rows.
    /// </summary>
    private int MaxTokensPerChunk { get; } = 1024;

    /// <summary>
    /// Represents the minimum number of columns allowed in a chunk when processing wide rows of CSV data.
    /// Used as a constraint to dynamically adjust the size of column groups for chunking, ensuring the division
    /// of data remains feasible while adhering to token or size limitations.
    /// </summary>
    private int MinColumnsPerChunk { get; } = 1;

    /// Asynchronously converts a CSV file's content into a list of records based on the specified record factory.
    /// <param name="recordFactory">
    /// A delegate to create instances of <typeparamref name="TRecord"/> for each processed row in the CSV content.
    /// </param>
    /// <param name="csvContent">
    /// The CSV content to be parsed and processed.
    /// </param>
    /// <param name="filename">
    /// The name of the file being processed, used for metadata or summary generation.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a list of <typeparamref name="TRecord"/>
    /// instances representing the structured data generated from the CSV content.
    /// </returns>
    public async Task<List<TRecord>> ConvertCsvAsync(Func<TRecord> recordFactory, string csvContent, string filename)
    {
        List<TRecord> records = [];
        
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = true
        });
        
        // Read headers
        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord!.ToList();

        TRecord record;
        
        var rowIndex = 0;
        while (await csv.ReadAsync())
        {
            rowIndex++;
            var rowData = headers.Select(header => csv.GetField(header) ?? string.Empty)
                .Select(EscapeMarkdownCell)
                .ToList(); // Escape data cells
            
            if (headers.Count <= MaxColumnsPerChunk)
            {
                // Small row: Chunk as full Markdown table
                var rowBuilder = new StringBuilder();
                rowBuilder.AppendLine($"| {string.Join(" | ", headers)} |");
                rowBuilder.AppendLine($"| {string.Join(" | ", Enumerable.Repeat("---", headers.Count))} |"); // Simple separator
                rowBuilder.AppendLine($"| {string.Join(" | ", rowData)} |");

                // Check token count; if over, fall back to key-value text
                var tableText = rowBuilder.ToString();
                if (DocumentChunker.CountTokens(tableText) > MaxTokensPerChunk)
                {
                    tableText = BuildKeyValueText(headers, rowData, rowIndex);
                }

                record = recordFactory();
                
                record.Text = tableText;
                record.CsvPath = $"{filename}:row:{rowIndex}";
                record.NodeType = "FullRow";
                record.ParentInfo =filename;
                
                records.Add(record);
            }
            else
            {
                // Wide row: Split into sub-chunks by column groups, each as a Markdown table
                var currentMaxGroupSize = MaxColumnsPerChunk;
                for (var colStart = 0; colStart < headers.Count; colStart += currentMaxGroupSize)
                {
                    var groupSize = Math.Min(currentMaxGroupSize, headers.Count - colStart);
                    var groupHeaders = headers.Skip(colStart).Take(groupSize).ToList();
                    var groupData = rowData.Skip(colStart).Take(groupSize).ToList();

                    var subBuilder = new StringBuilder();
                    subBuilder.AppendLine($"| {string.Join(" | ", groupHeaders)} |");
                    subBuilder.AppendLine($"| {string.Join(" | ", Enumerable.Repeat("---", groupHeaders.Count))} |"); // Simple separator
                    subBuilder.AppendLine($"| {string.Join(" | ", groupData)} |");

                    var subChunkText = subBuilder.ToString();

                    // Check token count
                    var tokenCount = DocumentChunker.CountTokens(subChunkText);
                    if (tokenCount > MaxTokensPerChunk && groupSize > MinColumnsPerChunk)
                    {
                        // Reduce group size dynamically and retry this group (backtrack)
                        currentMaxGroupSize = Math.Max(groupSize / 2, MinColumnsPerChunk);
                        colStart -= groupSize; // Backtrack to retry
                        continue;
                    }

                    if (tokenCount > MaxTokensPerChunk)
                    {
                        // Fallback for minimal groups: Use key-value text
                        subChunkText = BuildKeyValueText(groupHeaders, groupData, rowIndex);
                    }

                    record = recordFactory();
                
                    record.Text = subChunkText;
                    record.CsvPath =
                        $"{filename}:row:{rowIndex}:columns:{colStart + 1}-{colStart + groupHeaders.Count}";
                    record.NodeType = "SubRow";
                    record.ParentInfo = $"{filename}:row:{rowIndex}";
                
                    records.Add(record);
                }
            }
        }

        var summaryBuilder = new StringBuilder();
        summaryBuilder.AppendLine($"# CSV File Summary: {filename}");
        summaryBuilder.AppendLine($"Column Count: {headers.Count}");
        summaryBuilder.AppendLine($"Row Count: {rowIndex}");
        summaryBuilder.AppendLine("Headers:");
        summaryBuilder.AppendLine(string.Join(" | ", headers.Select(EscapeMarkdownCell)));
        
        record = recordFactory();
        
        record.Text = summaryBuilder.ToString();
        record.CsvPath = $"{filename}:metadata";
        record.NodeType = "Summary";
        record.ParentInfo = filename;
        
        records.Add(record);
        
        return records;
    }
    
    // Helper to escape Markdown table-breaking characters
    /// Escapes special Markdown characters in the given cell value to prevent formatting issues within Markdown tables.
    /// Specifically, this method escapes pipe (`|`) characters and replaces newline characters (`\n` and `\r`)
    /// with `<br>` for proper rendering.
    /// <param name="cell">
    /// The original string value from a CSV cell that needs special characters escaped for Markdown compatibility.
    /// </param>
    /// <returns>
    /// A string with Markdown-breaking characters properly escaped, ensuring safe usage within Markdown tables.
    /// </returns>
    private static string EscapeMarkdownCell(string cell)
    {
        // Escape pipes and newlines; replace newlines with <br> for rendering, or spaces if preferred
        return cell.Replace("|", "\\|").Replace("\n", "<br>").Replace("\r", "<br>");
    }
    
    // Fallback to key-value text as bullet points for better rendering
    /// Builds a key-value text representation of given keys and values for a specific row.
    /// This text is structured as bullet points, with each key-value pair represented as a
    /// single list item, preceded by the row identifier.
    /// <param name="keys">
    /// A list of keys representing column headers or field names.
    /// </param>
    /// <param name="values">
    /// A list of values corresponding to the provided keys.
    /// </param>
    /// <param name="rowId">
    /// The zero-based index of the row to which this data belongs, used in the text for context.
    /// </param>
    /// <returns>
    /// A string containing a key-value representation of the provided keys and values
    /// formatted as bullet points, prefixed with row information.
    /// </returns>
    private static string BuildKeyValueText(List<string> keys, List<string> values, int rowId)
    {
        var kvBuilder = new StringBuilder();
        kvBuilder.AppendLine($"Row {rowId} Partial Data:");
        for (var i = 0; i < keys.Count; i++)
        {
            kvBuilder.AppendLine($"- {keys[i]}: {values[i]}");
        }
        return kvBuilder.ToString();
    }
}