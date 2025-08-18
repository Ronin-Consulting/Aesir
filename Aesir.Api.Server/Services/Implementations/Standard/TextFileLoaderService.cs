using System.ClientModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Aesir.Api.Server.Extensions;
using Aesir.Common.FileTypes;
using Aesir.Api.Server.Models;
using Markdig;
using Markdig.Renderers.Roundtrip;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Tiktoken;

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
) : ITextFileLoaderService<TKey, TRecord>
    where TKey : notnull
    where TRecord : AesirTextData<TKey>
{
    /// <summary>
    /// A static instance of the encoder used for token counting operations within the service.
    /// Utilizes the default encoding from the <see cref="DocumentChunker"/> class.
    /// </summary>
    private static readonly Encoder TokenCounter = new(DocumentChunker.DefaultEncoding);

    /// Represents a utility class for chunking text into smaller sections based on token limits.
    /// This class is designed to break down large blocks of text, such as documents or raw content,
    /// into manageable chunks for further processing or analysis. The chunking process can optionally
    /// include a header for each chunk, allowing metadata to be prepended to the content.
    /// This class is marked as experimental and its API or behavior may be subject to changes in future versions.
    /// Thread Safety:
    /// This class is not guaranteed to be thread-safe.
    /// Experimental:
    /// The usage of this class is currently experimental (Code: SKEXP0050).
    /// Remarks:
    /// The token limits, such as `tokensPerParagraph` and `tokensPerLine`, govern the chunking logic
    /// to ensure the text sections are suitable for downstream consumption.
    private static readonly DocumentChunker DocumentChunker = new();

    /// Asynchronously loads a text file, processes its content based on the file type, and updates the underlying vector store collection with the extracted data.
    /// <param name="request">
    /// An instance of <see cref="LoadTextFileRequest"/> containing the details of the text file to load, such as its local path and file name.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests, allowing the operation to be canceled.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the local file path or file name in <paramref name="request"/> is null or empty.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown if the file type of the requested file is not supported.
    /// </exception>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    public async Task LoadTextFileAsync(LoadTextFileRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.TextFileLocalPath))
            throw new InvalidOperationException("TextFileLocalPath is empty");

        if (string.IsNullOrEmpty(request.TextFileFileName))
            throw new InvalidOperationException("TextFileFileName is empty");

        var supportedFileContentTypes = GetSupportedFileContentTypes();
        if (!request.TextFileFileName.ValidFileContentType(
                out var actualContentType,
                supportedFileContentTypes
            ))
            throw new NotSupportedException(
                $"Only [{string.Join(", ", supportedFileContentTypes)}] files are currently supported and not: {actualContentType}");

        // Create the collection if it doesn't exist
        await vectorStoreRecordCollection.EnsureCollectionExistsAsync(cancellationToken).ConfigureAwait(false);

        var toDelete = await vectorStoreRecordCollection.GetAsync(
                filter: data => data.Text != null,
                int.MaxValue,
                cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);

        toDelete = toDelete.Where(data =>
            data.ReferenceDescription!.Contains(request.TextFileFileName.TrimStart("file://"))).ToList();

        if (toDelete.Count > 0)
            await vectorStoreRecordCollection.DeleteAsync(
                toDelete.Select(td => td.Key), cancellationToken);

        var fileContent = await GetTextRawContentAsync(request.TextFileLocalPath);

        var plainTextConverter = new PlainTextToMarkdownConverter();
        var markdownConverter = new MarkdownToMarkdownConverter();
        var htmlConverter = new HtmlToMarkdownConverter();

        var content = actualContentType switch
        {
            SupportedFileContentTypes.PlainTextContentType => await plainTextConverter.ConvertAsync(fileContent),
            SupportedFileContentTypes.MarkdownContentType => await markdownConverter.ConvertAsync(fileContent),
            SupportedFileContentTypes.HtmlContentType => await htmlConverter.ConvertAsync(fileContent),
            SupportedFileContentTypes.JsonContentType => fileContent,
            SupportedFileContentTypes.XmlContentType => fileContent,
            _ => throw new NotSupportedException($"Content type {actualContentType} is not supported")
        };

        TRecord[] records;
        var batchSize = request.BatchSize;

        if (actualContentType == SupportedFileContentTypes.JsonContentType)
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
            records = (await jsonConverter.ConvertJsonAsync(CreateJsonRecord, content).ConfigureAwait(false))
                .Cast<TRecord>().ToArray();
            
            var processedRecords = new List<TRecord>(records.Length);
            for (var i = 0; i < records.Length; i += batchSize)
            {
                var batch = records.Skip(i).Take(batchSize);
                var recordTasks = batch.Select(async record =>
                {
                    var textChunk = record.Text ?? throw new InvalidOperationException("Text is null");

                    record.Key = uniqueKeyGenerator.GenerateKey();
                    record.ReferenceDescription ??= request.TextFileFileName.TrimStart("file://");
                    record.ReferenceLink ??=
                        new Uri($"file://{request.TextFileFileName.TrimStart("file://")}").AbsoluteUri;
                    record.TextEmbedding ??= await GenerateEmbeddings(textChunk, cancellationToken);
                    record.TokenCount ??= TokenCounter.CountTokens(textChunk);

                    return record;
                }).ToArray();

                processedRecords.AddRange(await Task.WhenAll(recordTasks).ConfigureAwait(false));
            }
            
            records = processedRecords.ToArray();
        }
        else
        if (actualContentType == SupportedFileContentTypes.XmlContentType)
        {
            throw new NotImplementedException();
        }
        else
        if (actualContentType == SupportedFileContentTypes.CsvContentType)
        {
            throw new NotImplementedException();
        }
        else
        {
            var rawContent = new RawContent
            {
                Text = content,
                PageNumber = 1
            };

            var textChunks = DocumentChunker.ChunkText(rawContent.Text!,
                $"File: {request.TextFileFileName}\nPage: {rawContent.PageNumber}");
            
            var chunks = textChunks.ToArray();
            var processedRecords = new List<TRecord>(chunks.Length);

            for (var i = 0; i < chunks.Length; i += batchSize)
            {
                var batch = chunks.Skip(i).Take(batchSize);

                // Process each chunk
                var recordTasks = batch.Select(async chunk =>
                {
                    var chunkContent = new RawContent
                    {
                        Text = chunk,
                        PageNumber = 1
                    };

                    var record = recordFactory(chunkContent, request);
                    record.Key = uniqueKeyGenerator.GenerateKey();
                    record.Text ??= chunk;
                    record.ReferenceDescription ??= request.TextFileFileName.TrimStart("file://");
                    record.ReferenceLink ??=
                        new Uri($"file://{request.TextFileFileName.TrimStart("file://")}").AbsoluteUri;
                    record.TextEmbedding ??= await GenerateEmbeddings(chunk, cancellationToken);
                    record.TokenCount ??= TokenCounter.CountTokens(record.Text!);

                    return record;
                }).ToArray();

                processedRecords.AddRange(await Task.WhenAll(recordTasks).ConfigureAwait(false));
            }

            records = processedRecords.ToArray();
        }

        // Upsert the records into the vector store
        await vectorStoreRecordCollection
            .UpsertAsync(records, cancellationToken: cancellationToken).ConfigureAwait(false);

        // Unload models to free resources
        await modelsService.UnloadVisionModelAsync();
        await modelsService.UnloadEmbeddingModelAsync();
    }

    /// Generates embeddings for the given text with retry logic to handle specific transient errors.
    /// <param name="text">The text input for which embeddings are to be generated.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the embedding generation task to complete.</param>
    /// <returns>An Embedding instance containing the generated vector representation of the input text.</returns>
    /// <exception cref="HttpOperationException">Thrown when a transient error such as too many requests occurs, and retries are exhausted.</exception>
    /// <exception cref="ClientResultException">Thrown when there is an error during the embedding generation process that is not transient.</exception>
    private async Task<Embedding<float>> GenerateEmbeddings(string text,
        CancellationToken cancellationToken)
    {
        try
        {
            var options = new EmbeddingGenerationOptions()
            {
                Dimensions = 1024
            };
            var vector = (await embeddingGenerator
                .GenerateAsync(text, options, cancellationToken: cancellationToken).ConfigureAwait(false)).Vector;

            return new Embedding<float>(vector);
        }
        catch (ClientResultException ex)
        {
            logger.LogError("Failed to generate embedding. Error: {HttpOperationException}",
                ex.GetRawResponse()?.Content.ToString() ?? ex.ToString());

            throw;
        }
    }

    /// Retrieves the list of file content types that are supported by the service.
    /// <returns>
    /// An array of strings representing the supported file content types.
    /// </returns>
    protected virtual string[] GetSupportedFileContentTypes()
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
    /// A task that represents the asynchronous operation. The task result contains the raw text content read from the file.
    /// </returns>
    protected virtual async Task<string> GetTextRawContentAsync(string textFileLocalPath)
    {
        return await File.ReadAllTextAsync(textFileLocalPath);
    }
}

/// <summary>
/// Converts plain text into Markdown-formatted text asynchronously.
/// </summary>
/// <remarks>
/// This class is designed for converting raw plain text into its Markdown representation.
/// It utilizes Markdown formatting rules to process the input text in order to produce
/// its equivalent well-structured Markdown output.
/// </remarks>
internal class PlainTextToMarkdownConverter
{
    /// <summary>
    /// Represents an instance of a <see cref="MarkdownPipeline"/> used to process and transform
    /// plain text or markdown content into alternative formats. This instance is pre-configured
    /// with advanced extensions to enhance markdown parsing capabilities.
    /// </summary>
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// Converts plain text into Markdown format asynchronously.
    /// <param name="plainText">The plain text content to be converted.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the converted Markdown content as a string.
    public async Task<string> ConvertAsync(string plainText)
    {
        return await Task.FromResult(Markdown.ToPlainText(plainText, _pipeline));
    }
}

/// <summary>
/// The <c>MarkdownToMarkdownConverter</c> class provides functionality for processing and converting
/// markdown text while maintaining its markdown format. It parses the provided markdown content, processes
/// it, and returns the result as a markdown string with minimal modifications.
/// </summary>
internal class MarkdownToMarkdownConverter
{
    /// <summary>
    /// Represents the instance of a <see cref="MarkdownPipeline"/> used for parsing and rendering markdown content.
    /// Configured to use advanced extensions within the pipeline.
    /// </summary>
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>
    /// Converts the input markdown text into a processed markdown format.
    /// </summary>
    /// <param name="markdownText">The original markdown text to be processed.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the processed markdown text as a string.</returns>
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
/// This class leverages the ReverseMarkdown library to perform the conversion while
/// allowing for configurable options for handling unknown tags, GitHub-flavored Markdown,
/// comment removal, and other behaviors.
/// </summary>
internal class HtmlToMarkdownConverter
{
    /// <summary>
    /// Converts the provided HTML content to its Markdown equivalent using the specified configuration.
    /// </summary>
    /// <param name="html">The input HTML string to be converted to Markdown.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the converted Markdown string.</returns>
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
/// A utility class that converts JSON content into Aesir-compatible text data records
/// by flattening JSON structures and mapping them to instances of type <typeparamref name="TRecord"/>.
/// </summary>
/// <typeparam name="TRecord">
/// The type of text data record that implements the <see cref="IJsonTextData"/> interface.
/// </typeparam>
[Experimental("SKEXP0001")]
internal class JsonToAesirTextDataConverter<TRecord> where TRecord : IJsonTextData
{
    /// <summary>
    /// Represents a utility class for chunking large text documents into smaller, manageable divisions
    /// based on configurable token limits per paragraph and per line.
    /// </summary>
    private static readonly DocumentChunker DocumentChunker = new();

    /// Converts a JSON string into a list of records of type TRecord.
    /// <param name="recordFactory">
    /// A function that provides a default instance of the TRecord type to be used for creating records.
    /// </param>
    /// <param name="jsonContent">
    /// The JSON content in string format to be parsed and converted into records.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a list of records parsed from the JSON content.
    /// </returns>
    public async Task<List<TRecord>> ConvertJsonAsync(
        Func<TRecord> recordFactory, string jsonContent)
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
                        Flatten(arr[i], jsonKey, nodeType);
                    }
                }
            }
            else if (node is JsonValue rootVal && string.IsNullOrEmpty(prefix))
            {
                var jsonValString = rootVal.ToString();
                var chunks = DocumentChunker.ChunkText(jsonValString, $"Key: root=");
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