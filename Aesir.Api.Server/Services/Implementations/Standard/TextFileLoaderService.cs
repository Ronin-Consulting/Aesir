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
/// Provides functionality for loading and processing text files, transforming raw text content into domain-specific record representations.
/// </summary>
/// <typeparam name="TKey">
/// The type of the unique identifier for the records. Must be a non-nullable type.
/// </typeparam>
/// <typeparam name="TRecord">
/// The type of the domain-specific record used to represent processed text content. Must inherit from <see cref="AesirTextData{TKey}"/>.
/// </typeparam>
/// <remarks>
/// This service facilitates the processing of text file content by leveraging unique identifier generation, vector-based data storage, and embedding computation.
/// It offers methods for content validation, asynchronous file handling, and customization of record creation, allowing integration with domain-specific requirements.
/// </remarks>
[Experimental("SKEXP0001")]
public class TextFileLoaderService<TKey, TRecord>(
    UniqueKeyGenerator<TKey> uniqueKeyGenerator,
    VectorStoreCollection<TKey, TRecord> vectorStoreRecordCollection,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    Func<RawContent, LoadTextFileRequest, TRecord> recordFactory,
    IConfigurationService configurationService,
    IServiceProvider serviceProvider,
    ILogger<TextFileLoaderService<TKey, TRecord>> logger
) : BaseDataLoaderService<TKey, TRecord>(uniqueKeyGenerator, vectorStoreRecordCollection, embeddingGenerator,
    configurationService, serviceProvider, logger), ITextFileLoaderService<TKey, TRecord>
    where TKey : notnull
    where TRecord : AesirTextData<TKey>
{
    /// <summary>
    /// A delegate providing a mechanism to generate instances of <typeparamref name="TRecord"/>
    /// using raw content and file loading request parameters.
    /// Facilitates the creation of typed records from unstructured input data during
    /// text file loading operations.
    /// </summary>
    private readonly Func<RawContent, LoadTextFileRequest, TRecord> _recordFactory = recordFactory;

    /// <summary>
    /// Represents an abstract base class for handling and processing different types of content during text file handling operations.
    /// </summary>
    /// <remarks>
    /// Concrete implementations of this class should provide specific logic to process various content formats such as JSON, XML, or plain text.
    /// These implementations are required to override the <see cref="ProcessContentAsync"/> method to define how the raw content should be converted
    /// into domain-specific records.
    /// </remarks>
    private abstract class ContentTypeHandler
    {
        /// Asynchronously processes the provided string content based on the specified request and creates a collection of records.
        /// <param name="content">
        /// The string content to be processed.
        /// </param>
        /// <param name="request">
        /// An instance of <see cref="LoadTextFileRequest"/> that includes metadata and configuration for processing the content.
        /// </param>
        /// <param name="recordFactory">
        /// A factory method that creates <typeparamref name="TRecord"/> instances using the processed raw content and the provided request.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="content"/>, <paramref name="request"/>, or <paramref name="recordFactory"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the content cannot be processed due to invalid or unexpected input.
        /// </exception>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an array of <typeparamref name="TRecord"/> that were created from the processed content.
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
    /// This class is a specialized implementation of <see cref="ContentTypeHandler"/>.
    /// It is designed to parse and process JSON content, transforming it into structured data
    /// records while adhering to the requirements of the text file loader service.
    /// </remarks>
    private class JsonContentHandler : ContentTypeHandler
    {
        /// Asynchronously processes the provided JSON content and converts it into a collection of records.
        /// <param name="content">
        /// The raw JSON content to be processed.
        /// </param>
        /// <param name="request">
        /// A <see cref="LoadTextFileRequest"/> instance containing metadata and configuration information
        /// related to the processing of the provided JSON content.
        /// </param>
        /// <param name="recordFactory">
        /// A function that facilitates the creation of <typeparamref name="TRecord"/> instances
        /// using the raw content and processing details.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if any mandatory parameter, such as <paramref name="content"/>, <paramref name="request"/>, or <paramref name="recordFactory"/>, is null.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown if the content fails to parse or does not conform to anticipated JSON structure.
        /// </exception>
        /// <returns>
        /// A task representing the asynchronous operation. The task result contains an array of
        /// <typeparamref name="TRecord"/> instances derived from the provided JSON content.
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
    /// Handles processing of XML content by parsing and transforming it into structured domain-specific records.
    /// </summary>
    /// <remarks>
    /// This class operates as part of the data loading pipeline, ensuring that XML content is properly converted into formats suitable for storage and further operations.
    /// It utilizes the provided record factory to generate domain representations from raw XML content.
    /// </remarks>
    private class XmlContentHandler : ContentTypeHandler
    {
        /// Asynchronously processes the content of an XML file and converts it into an array of records.
        /// <param name="content">
        /// The raw XML content to be processed.
        /// </param>
        /// <param name="request">
        /// Contains details about the file being loaded, including metadata and configuration settings.
        /// </param>
        /// <param name="recordFactory">
        /// A factory function to create instances of <typeparamref name="TRecord"/> based on the raw content and request details.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if any of the parameters, such as <paramref name="content"/>, <paramref name="request"/>, or <paramref name="recordFactory"/>, is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the XML content cannot be processed due to format errors or invalid configuration.
        /// </exception>
        /// <returns>
        /// A task representing the asynchronous operation. The task result contains an array of <typeparamref name="TRecord"/> objects created from the converted XML content.
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
    /// Handles the processing of CSV content, converting raw CSV input into structured domain-specific records.
    /// </summary>
    /// <remarks>
    /// This internal class provides mechanisms to parse and process CSV formatted content, leveraging a record factory
    /// to create domain-specific representations of the data. It supports asynchronous processing, ensuring scalable
    /// handling of potentially large datasets during text file loading.
    /// </remarks>
    private class CsvContentHandler : ContentTypeHandler
    {
        /// Asynchronously processes CSV content provided as a string and transforms it into an array of records
        /// using the specified parameters and record factory logic.
        /// <param name="content">
        /// The raw CSV content as a string to be processed.
        /// </param>
        /// <param name="request">
        /// A <see cref="LoadTextFileRequest"/> instance containing metadata, such as the file name, and configuration for the CSV processing.
        /// </param>
        /// <param name="recordFactory">
        /// A function that creates instances of <typeparamref name="TRecord"/> using the provided raw content and processing details.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if any of the arguments, such as <paramref name="content"/>, <paramref name="request"/> or
        /// <paramref name="recordFactory"/>, are null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the CSV content is improperly formatted or fails to meet processing requirements.
        /// </exception>
        /// <returns>
        /// A task representing the asynchronous operation. The task result contains an array of <typeparamref name="TRecord"/> objects
        /// created from the processed CSV content.
        /// </returns>
        public override async Task<TRecord[]> ProcessContentAsync(string content, LoadTextFileRequest request,
            Func<RawContent, LoadTextFileRequest, TRecord> recordFactory)
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
    /// Handles processing for default content types, including plain text, markdown, and HTML,
    /// by transforming the raw content into domain-specific records in a chunked, structured manner.
    /// </summary>
    /// <remarks>
    /// This class serves as a fallback content handler for scenarios where no specific handler is assigned
    /// to a given content type. It is responsible for segmenting the provided content into logical parts
    /// and generating corresponding domain-specific records using a factory method.
    /// </remarks>
    private class DefaultContentHandler : ContentTypeHandler
    {
        /// Asynchronously processes the provided content by dividing it into text chunks and transforming each chunk into records using the specified factory method.
        /// <param name="content">
        /// The raw text content to be divided into chunks and converted into records.
        /// </param>
        /// <param name="request">
        /// An instance of <see cref="LoadTextFileRequest"/> containing details such as the file name and other metadata necessary for processing.
        /// </param>
        /// <param name="recordFactory">
        /// A delegate used to create instances of <typeparamref name="TRecord"/> from the provided raw content and additional processing information.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="content"/>, <paramref name="request"/>, or <paramref name="recordFactory"/> is null.
        /// </exception>
        /// <returns>
        /// A task representing the asynchronous operation. The task result contains an array of <typeparamref name="TRecord"/> derived from the processed content chunks.
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

    /// Asynchronously loads a text file into the system, processes its content, and stores the resulting records into the data collection.
    /// <param name="request">
    /// An instance of <see cref="LoadTextFileRequest"/> containing details about the text file to be processed, including its metadata and configuration information.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="request"/> or any required property within it is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown in case of errors during any part of the file loading and processing workflow such as content validation or record processing.
    /// </exception>
    /// <returns>
    /// A task representing the asynchronous operation to load, process, and store the records derived from the given text file.
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

    /// Validates the request parameters ensuring they are not null or empty and adhere to required specifications.
    /// <param name="request">
    /// An instance of <see cref="LoadTextFileRequest"/> containing metadata and file details for the text file.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if any mandatory property, such as <see cref="LoadTextFileRequest.TextFileLocalPath"/> or
    /// <see cref="LoadTextFileRequest.TextFileFileName"/>, is null or an empty string.
    /// </exception>
    private static void ValidateRequest(LoadTextFileRequest request)
    {
        if (string.IsNullOrEmpty(request.TextFileLocalPath))
            throw new InvalidOperationException("TextFileLocalPath is empty");
        if (string.IsNullOrEmpty(request.TextFileFileName))
            throw new InvalidOperationException("TextFileFileName is empty");
    }

    /// Determines and validates the content type of a given file based on its name.
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

    /// Asynchronously converts the provided file content into a standardized string format based on the specified content type.
    /// <param name="fileContent">
    /// The raw content of the file to be converted, represented as a string.
    /// </param>
    /// <param name="contentType">
    /// The type of content described by the file, which influences the conversion process. Supported types include PlainText, Markdown, HTML, JSON, XML, and CSV.
    /// </param>
    /// <exception cref="NotSupportedException">
    /// Thrown when the specified <paramref name="contentType"/> is not supported for conversion operations.
    /// </exception>
    /// <returns>
    /// A task representing the asynchronous conversion process. The task result contains the converted file content as a standardized string.
    /// </returns>
    private static async Task<string> ConvertContentAsync(string fileContent, string contentType)
    {
        return contentType switch
        {
            FileTypeManager.MimeTypes.PlainText => await new PlainTextToMarkdownConverter().ConvertAsync(
                fileContent),
            FileTypeManager.MimeTypes.Markdown => await new MarkdownToMarkdownConverter().ConvertAsync(
                fileContent),
            FileTypeManager.MimeTypes.Html => await new HtmlToMarkdownConverter().ConvertAsync(fileContent),
            FileTypeManager.MimeTypes.Json => fileContent,
            FileTypeManager.MimeTypes.Xml => fileContent,
            FileTypeManager.MimeTypes.Csv => fileContent,
            _ => throw new NotSupportedException($"Content type {contentType} is not supported")
        };
    }

    /// Creates the appropriate content handler based on the specified content type.
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
            FileTypeManager.MimeTypes.Json => new JsonContentHandler(),
            FileTypeManager.MimeTypes.Xml => new XmlContentHandler(),
            FileTypeManager.MimeTypes.Csv => new CsvContentHandler(),
            _ => new DefaultContentHandler()
        };
    }


    /// Retrieves the list of file content types that are supported by the TextFileLoaderService.
    /// <returns>
    /// An array of strings specifying the MIME types of the supported file content types.
    /// </returns>
    protected static string[] GetSupportedFileContentTypes()
    {
        return new[]
        {
            FileTypeManager.MimeTypes.PlainText,
            FileTypeManager.MimeTypes.Markdown,
            FileTypeManager.MimeTypes.Html,
            FileTypeManager.MimeTypes.Xml,
            FileTypeManager.MimeTypes.Json,
            FileTypeManager.MimeTypes.Csv
        };
    }

    /// Asynchronously retrieves the raw text content of a file given its local file path.
    /// <param name="textFileLocalPath">
    /// The path to the text file on the local file system from which the raw content will be read.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous read operation. The result contains the entire raw text content of the specified file as a string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="textFileLocalPath"/> is null or an empty string.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown if the file at the specified <paramref name="textFileLocalPath"/> cannot be found.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if an error occurs during the file reading process, such as insufficient permissions or the file being locked.
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
    /// A readonly instance of <see cref="MarkdownPipeline"/> configured with advanced extensions
    /// to support enhanced Markdown parsing and rendering. Used for processing and converting
    /// plain text into Markdown with additional syntax and formatting capabilities.
    /// </summary>
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// Asynchronously converts the provided text content into Markdown-compatible content based on the specified content type.
    /// <param name="fileContent">
    /// The text content to be converted.
    /// </param>
    /// <param name="contentType">
    /// The MIME type of the input content, which determines the conversion processing logic.
    /// </param>
    /// <exception cref="NotSupportedException">
    /// Thrown when the specified content type is not supported for conversion.
    /// </exception>
    /// <returns>
    /// A task representing the asynchronous conversion operation. The task result contains the converted content as a string in Markdown format.
    /// </returns>
    public async Task<string> ConvertAsync(string plainText)
    {
        return await Task.FromResult(Markdown.ToPlainText(plainText, _pipeline));
    }
}

/// <summary>
/// Provides functionality to process and transform markdown text using a roundtrip rendering approach, preserving the original markdown structure and syntax.
/// </summary>
/// <remarks>
/// This class is built to parse markdown content into a structured document format, process the content as necessary,
/// and re-render it back into markdown. It is intended for scenarios requiring preservation of markdown fidelity while enabling modifications or processing workflows.
/// </remarks>
internal class MarkdownToMarkdownConverter
{
    /// <summary>
    /// A private readonly instance of the <see cref="MarkdownPipeline"/> that is configured to parse and process
    /// markdown content using advanced extensions. It serves as a core component for handling markdown operations
    /// within the <see cref="MarkdownToMarkdownConverter"/> class.
    /// </summary>
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// Asynchronously converts the input markdown content into a processed markdown format.
    /// <param name="markdownText">
    /// The markdown content to be converted.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains
    /// the processed markdown content as a string.
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
/// This class leverages an external library to transform HTML input into Markdown-compatible output.
/// It supports a range of customization options, including handling of unknown tags,
/// enabling GitHub-flavored Markdown, table formatting, and the removal of HTML comments.
/// Ideal for scenarios where converting HTML documents to a simpler Markdown representation is required.
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
    /// A task representing the asynchronous operation. The task result contains the converted content as a Markdown string.
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
/// </summary>
/// <typeparam name="TRecord">
/// The type of the record to which JSON data will be transformed. Must adhere to the <see cref="IJsonTextData"/> interface.
/// </typeparam>
/// <remarks>
/// This class processes hierarchical JSON structures, mapping and flattening them into a standardized format implementing <see cref="IJsonTextData"/>.
/// Commonly used for transforming incoming JSON data into structured text-based representations suitable for analysis, storage, or other operations.
/// </remarks>
[Experimental("SKEXP0001")]
internal class JsonToAesirTextDataConverter<TRecord> where TRecord : IJsonTextData
{
    /// <summary>
    /// A static instance of the <see cref="DocumentChunker"/> utility class, used for splitting
    /// text content into smaller, logically consistent chunks based on token constraints.
    /// Facilitates enhanced contextual integrity and efficient processing of large text documents.
    /// </summary>
    private static readonly DocumentChunker DocumentChunker = new();

    /// Converts a JSON string into a list of records of a specified type asynchronously.
    /// <param name="recordFactory">
    /// A function that creates an instance of the record type to generate records from the JSON data.
    /// </param>
    /// <param name="jsonContent">
    /// The JSON string containing data to be parsed and converted into records.
    /// </param>
    /// <param name="filename">
    /// The name of the file associated with the JSON data, used for contextual or logging purposes.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a list of records of the specified type created from the parsed JSON content.
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
    /// A utility class designed to segment large text documents into smaller, manageable chunks
    /// based on specified token limits for paragraphs and lines. Facilitates the processing,
    /// indexing, and analysis of extensive text data by dividing content into structured segments
    /// for efficient handling.
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
/// Provides functionality to convert CSV content into structured domain-specific text data records using a customizable factory method.
/// </summary>
/// <typeparam name="TRecord">
/// The type of the record that represents processed CSV rows. Must implement <see cref="ICsvTextData"/>.
/// </typeparam>
/// <remarks>
/// This class handles the transformation of raw CSV file content into structured text data by reading the file's headers and content rows.
/// It supports escaping special characters, such as markdown syntax, present in the CSV data and generating metadata about the file structure.
/// The processed content and metadata are encapsulated within the resulting domain-specific records, enabling downstream operational use.
/// </remarks>
[Experimental("SKEXP0001")]
internal class CsvToAesirTextDataConverter<TRecord> where TRecord : ICsvTextData
{
    /// <summary>
    /// A utility class designed to facilitate the partitioning and token analysis of text content.
    /// Supports configurable properties for chunking based on token limits per paragraph or line, enabling efficient
    /// handling of large text data in workflows with size constraints or processing limitations.
    /// </summary>
    private static readonly DocumentChunker DocumentChunker = new();

    /// <summary>
    /// Specifies the maximum number of columns that can be processed together as a single chunk
    /// when handling CSV data with wide rows. This property ensures that large tables with
    /// excessive columns are split into manageable portions for processing or representation.
    /// </summary>
    private int MaxColumnsPerChunk { get; } = 10;

    /// <summary>
    /// Specifies the maximum number of tokens allowed per chunk when processing
    /// CSV file data. This property is used to control data segmentation, ensuring
    /// that generated text chunks remain within a predefined token limit.
    /// </summary>
    private int MaxTokensPerChunk { get; } = 1024;

    /// <summary>
    /// Specifies the minimum number of columns that can be grouped together in a chunk when
    /// processing CSV data. This property ensures that chunks do not fall below a certain
    /// size during dynamic adjustments for token count limits, helping to maintain a balance
    /// between chunk size and token optimization during CSV data conversion.
    /// </summary>
    private int MinColumnsPerChunk { get; } = 1;

    /// Asynchronously converts CSV content into a list of structured records using the specified record factory and metadata details.
    /// <param name="recordFactory">
    /// A delegate function used to create instances of <typeparamref name="TRecord"/> for each processed row in the CSV content.
    /// </param>
    /// <param name="csvContent">
    /// The CSV content to be parsed and converted into structured data.
    /// </param>
    /// <param name="filename">
    /// The name of the file associated with the CSV content, used for metadata generation and summarization.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The result of the task is a list of <typeparamref name="TRecord"/>
    /// objects created from the CSV content, with metadata and summary information as applicable.
    /// </returns>
    public async Task<List<TRecord>> ConvertCsvAsync(Func<TRecord> recordFactory, string csvContent, string filename)
    {
        List<TRecord> records = [];

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = true,
            ShouldSkipRecord = args => args.Row.Parser.Record?.All(string.IsNullOrWhiteSpace) ?? true
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
                rowBuilder.AppendLine($"Row {rowIndex + 1}");
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
                record.CsvPath = $"{filename}:row:{rowIndex + 1}";
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
                    subBuilder.AppendLine($"Row {rowIndex + 1}, Columns {colStart + 1}-{colStart + groupHeaders.Count}");
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
                        $"{filename}:row:{rowIndex + 1}:columns:{colStart + 1}-{colStart + groupHeaders.Count}";
                    record.NodeType = "SubRow";
                    record.ParentInfo = $"{filename}:row:{rowIndex + 1}";
                
                    records.Add(record);
                }
            }
        }

        var summaryBuilder = new StringBuilder();
        summaryBuilder.AppendLine($"# CSV File Summary: {filename}");
        summaryBuilder.AppendLine($"Column Count: {headers.Count}");
        summaryBuilder.AppendLine($"Row Count: {rowIndex + 1}");
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
    /// Builds a key-value text representation of the provided keys and values for a given row,
    /// formatted as bullet points and prefixed with the row identifier.
    /// <param name="keys">
    /// A list of strings representing the keys, typically corresponding to column headers or field names.
    /// </param>
    /// <param name="values">
    /// A list of strings representing the values associated with the keys.
    /// </param>
    /// <param name="rowId">
    /// The zero-based index of the row to which the data corresponds. This is included in the text for contextual identification.
    /// </param>
    /// <returns>
    /// A string representing the key-value pairs as bullet points, including the row identifier for context.
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