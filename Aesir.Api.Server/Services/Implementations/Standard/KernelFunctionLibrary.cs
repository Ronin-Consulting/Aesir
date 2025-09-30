using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Aesir.Api.Server.Extensions;
using Aesir.Api.Server.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Plugins.Web;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// A library providing kernel functions for performing a variety of operations such as image analysis, hybrid
/// document search, semantic search, web search, and conversation summarization.
/// This class is designed to operate on textual data and allows integration with different search mechanisms using
/// provided connectors and filters. It is parameterized by a key type and a record type to maintain flexibility
/// when handling data structures.
/// </summary>
/// <typeparam name="TKey">
/// The type of keys uniquely identifying records. This must be non-nullable.
/// </typeparam>
/// <typeparam name="TRecord">
/// The type of the records being processed, which must inherit from AesirTextData<TKey>.
/// </typeparam>
[Experimental("SKEXP0001")]
public class KernelFunctionLibrary<TKey, TRecord>(
    ITextSearch textSearch,
    IKeywordHybridSearchable<TRecord>? hybridSearch,
    VectorStoreCollection<TKey, TRecord> vectorStoreCollection)
    where TKey : notnull
    where TRecord : AesirTextData<TKey>
{
    /// Analyzes image files to classify them as either 'document' or 'non-document'.
    /// If classified as a document, the image is processed with OCR to extract text.
    /// For non-document classifications, a detailed visual description is generated.
    /// This method is intended for image files like PNG, JPG, and BMP and should not be
    /// used for PDFs, text files, or non-image attachments.
    /// <param name="imageFilter">
    /// An optional filter to constrain the analysis to specific images. Pass null for no filtering.
    /// </param>
    /// <param name="top">
    /// The maximum number of results to return, with a default value of 5.
    /// </param>
    /// <returns>
    /// A kernel function that analyzes and processes the specified images, returning
    /// search results that include details such as name, value, and link for each analyzed image.
    /// </returns>
    public KernelFunction GetImageAnalysisFunction(TextSearchFilter? imageFilter = null, int top = 5)
    {
        var imageTextSearchOptions = new TextSearchOptions
        {
            Top = top,
            Filter = imageFilter
        };

        var imageTextSearchResultsFunctionOptions = new KernelFunctionFromMethodOptions()
        {
            FunctionName = "AnalyzeImageContent",
            Description =
                "Analyzes ONLY image files (e.g., PNG, JPG, BMP) to classify as 'document' or 'non-document'. If classified as a document image, transcribes text via OCR. If non-document, provides a detailed visual description. DO NOT use this for PDFs, text files, or non-image attachments—ignore or handle those separately.",
            Parameters =
            [
                new KernelParameterMetadata("query")
                {
                    Description = "The name of the image file to process.", ParameterType = typeof(string),
                    IsRequired = true
                },
                // new KernelParameterMetadata("count") { Description = "Maximum number of results to return (default: 25).", ParameterType = typeof(int), IsRequired = false, DefaultValue = 25 },
                // new KernelParameterMetadata("skip") { Description = "Number of initial results to skip for pagination (default: 0).", ParameterType = typeof(int), IsRequired = false, DefaultValue = 0 },
            ],
            ReturnParameter = new KernelReturnParameterMetadata
            {
                ParameterType = typeof(KernelSearchResults<TextSearchResult>),
                Description =
                    "A collection of search results, where each TextSearchResult contains properties Name, Value, and Link."
            },
        };

        return textSearch.CreateGetTextSearchResults(searchOptions: imageTextSearchOptions,
            options: imageTextSearchResultsFunctionOptions);
    }

    /// Creates a kernel function for performing hybrid document search. The search combines traditional search techniques
    /// with keyword-based and semantic matching mechanisms. It retrieves relevant content as specified by the query,
    /// returning details such as name, value, and link for each result. This function is suitable for searching through
    /// provided file collections for matched content using hybrid methods.
    /// <param name="searchOptions">
    /// Custom options to configure the hybrid search behavior. Pass null to use default options for the search process.
    /// </param>
    /// <param name="top">
    /// The maximum number of search results to return. Default is null, in which case the default limit is applied.
    /// </param>
    /// <returns>
    /// A kernel function that performs hybrid document searches and provides structured results, where each result
    /// contains details such as name, value, and link of the found document.
    /// </returns>
    public KernelFunction GetHybridDocumentSearchFunction(HybridSearchOptions<TRecord>? searchOptions = null,
        int? top = null)
    {
        ArgumentNullException.ThrowIfNull(hybridSearch);

        var overrideTopResults = top;

        var functionOptions = new KernelFunctionFromMethodOptions()
        {
            FunctionName = "PerformHybridDocumentSearch",
            Description =
                "Perform a search for content related to the specified query. The search will return the name, value and link for the related content.",
            Parameters =
            [
                new KernelParameterMetadata("query")
                {
                    Description =
                        "The search query string, supporting keywords, phrases, or natural language input for hybrid matching.",
                    ParameterType = typeof(string), IsRequired = true
                },
                new KernelParameterMetadata("files")
                    { Description = "The files to search.", ParameterType = typeof(string[]), IsRequired = true },
                // new KernelParameterMetadata("count") { Description = "Maximum number of results to return (default: 25).", ParameterType = typeof(int), IsRequired = false, DefaultValue = 25 },
                // new KernelParameterMetadata("skip") { Description = "Number of initial results to skip for pagination (default: 0).", ParameterType = typeof(int), IsRequired = false, DefaultValue = 0 },
            ],
            ReturnParameter = new KernelReturnParameterMetadata
            {
                ParameterType = typeof(KernelSearchResults<TextSearchResult>),
                Description =
                    "A collection of search results, where each TextSearchResult contains properties Name, Value, and Link."
            },
        };

        return KernelFunctionFactory.CreateFromMethod(GetHybridSearchResultAsync, functionOptions);


        async Task<IEnumerable<TextSearchResult>> GetHybridSearchResultAsync(Kernel kernel, KernelFunction function,
            KernelArguments arguments, CancellationToken cancellationToken, int count = 5, int skip = 0)
        {
            arguments.TryGetValue("query", out var query);

            var files = new List<string>();
            arguments.TryGetValue("files", out var filesValue);

            if (filesValue is JsonElement jsonElement)
            {
                files = jsonElement.EnumerateArray().Select(x => x.GetString()).ToList()!;
            }

            if (string.IsNullOrEmpty(query?.ToString()) && files.Count == 0)
            {
                return [];
            }

            var searchValue = string.IsNullOrEmpty(query?.ToString()) ? string.Join(" ", files) : query.ToString();

            if (searchOptions != null)
                searchOptions.Skip = skip;

            var keywords = searchValue!.KeywordsOnly();
            var results = await hybridSearch.HybridSearchAsync(
                searchValue!,
                keywords,
                overrideTopResults ?? count,
                searchOptions,
                cancellationToken
            ).ToListAsync(cancellationToken).ConfigureAwait(false);

            // just return everything if less than count or override vs filtering to score of 0.5
            if (results.Count < (overrideTopResults ?? count))
                return results.Select(r =>
                    new TextSearchResult(r.Record.Text!)
                    {
                        Link = r.Record.ReferenceLink,
                        Name = r.Record.ReferenceDescription
                    }
                );

            return results.Where(r => r.Score >= 0.6f).Select(r =>
                new TextSearchResult(r.Record.Text!)
                {
                    Link = r.Record.ReferenceLink,
                    Name = r.Record.ReferenceDescription
                }
            );
        }
    }

    /// Creates a kernel function for performing semantic document searches based on a query.
    /// It matches the query semantically to documents and returns details such as the name, value,
    /// and link for each relevant document. This function supports natural language input, phrases,
    /// and keywords for semantic matching, and is primarily designed for textual document content.
    /// <param name="semanticSearchFilter">
    /// An optional filter to constrain the scope of the semantic document search based on predefined criteria.
    /// Pass null to perform the search without any specific filter.
    /// </param>
    /// <param name="top">
    /// The maximum number of results to return. Default is 5.
    /// </param>
    /// <returns>
    /// A kernel function that executes the semantic document search and returns a collection of results,
    /// each represented as a TextSearchResult containing properties like Name, Value, and Link.
    /// </returns>
    public KernelFunction GetSemanticDocumentSearchFunction(TextSearchFilter? semanticSearchFilter = null, int top = 5)
    {
        var semanticTextSearchOptions = new TextSearchOptions
        {
            Top = top,
            Filter = semanticSearchFilter
        };

        var semanticSearchResultsFunctionOptions = new KernelFunctionFromMethodOptions()
        {
            FunctionName = "PerformSemanticDocumentSearch",
            Description =
                "Perform a search for content related to the specified query. The search will return the name, value and link for the related content.",
            Parameters =
            [
                new KernelParameterMetadata("query")
                {
                    Description =
                        "The search query string, supporting keywords, phrases, or natural language input for semantic matching.",
                    ParameterType = typeof(string), IsRequired = true
                },
                new KernelParameterMetadata("files")
                    { Description = "The files to search.", ParameterType = typeof(string[]), IsRequired = true },
                // new KernelParameterMetadata("count") { Description = "Maximum number of results to return (default: 25).", ParameterType = typeof(int), IsRequired = false, DefaultValue = 25 },
                // new KernelParameterMetadata("skip") { Description = "Number of initial results to skip for pagination (default: 0).", ParameterType = typeof(int), IsRequired = false, DefaultValue = 0 },
            ],
            ReturnParameter = new KernelReturnParameterMetadata
            {
                ParameterType = typeof(KernelSearchResults<TextSearchResult>),
                Description =
                    "A collection of search results, where each TextSearchResult contains properties like Name, Value, and Link."
            },
        };

        return textSearch
            .CreateGetTextSearchResults(searchOptions: semanticTextSearchOptions,
                options: semanticSearchResultsFunctionOptions);
    }

    /// Creates and returns a KernelFunction for performing web searches using the specified web search engine connector.
    /// The function utilizes a plugin to extract search results for a given query based on the implemented methods.
    /// <param name="webSearchEngineConnector">
    /// The connector instance for the web search engine, responsible for executing searches and retrieving results
    /// from the specified engine.
    /// </param>
    /// <returns>
    /// A KernelFunction object that facilitates web searches and retrieves results corresponding to the provided queries.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown when no suitable search function can be found or initialized using the provided web search plugin.
    /// </exception>
    public KernelFunction GetWebSearchFunction(IWebSearchEngineConnector webSearchEngineConnector)
    {
        // var googleConnector = new GoogleConnector(
        //     searchEngineId: "64cf6ca85e9454a44", //Environment.GetEnvironmentVariable("CSE_ID"),
        //     apiKey: "AIzaSyByEQBfXtNjdxIGlpeLRz0C1isORMnsHNU"); //Environment.GetEnvironmentVariable("GOOGLE_KEY"))

        var webSearchPlugin = new WebSearchEnginePlugin(webSearchEngineConnector);

        var methods = webSearchPlugin.GetType().GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        // only use the Search Results one because it has metadata...
        var functions =
            (from method in methods
                where method.Name.StartsWith("GetSearchResults")
                select KernelFunctionFactory.CreateFromMethod(
                    method, webSearchPlugin, "GetWebSearchResults"
                )).ToList();

        var webSearchFunction = functions.FirstOrDefault();

        if (webSearchFunction == null)
            throw new Exception("Unable to find web search function");

        return webSearchFunction;
    }

    /// Creates a kernel function designed to summarize conversation documents associated with a given conversation ID.
    /// This allows processing large document data related to specific conversations, breaking the content into manageable
    /// chunks for summarization.
    /// <param name="conversationId">
    /// The unique identifier of the conversation whose documents are to be summarized. Cannot be null.
    /// </param>
    /// <returns>
    /// A kernel function that processes and summarizes conversation documents, specifically extracting key details
    /// and insights from the associated data records.
    /// </returns>
    public KernelFunction GetSummarizeConversationDocumentFunction(string? conversationId)
    {
        ArgumentNullException.ThrowIfNull(conversationId);

        // NOTE: We should convert the other functions to plugins like this... 
        var plugin =
            new SummarizeConversationDocumentPlugin<Guid, AesirConversationDocumentTextData<Guid>>(
                vectorStoreCollection as VectorStoreCollection<Guid, AesirConversationDocumentTextData<Guid>> ??
                throw new InvalidOperationException(),
                conversationId
            );

        return KernelFunctionFactory.CreateFromMethod(plugin.GetChunksAsync);
    }
}

/// <summary>
/// A plugin for summarizing conversation documents, providing functionality to process and extract
/// summarized chunks of text from documents related to a specific conversation. It interacts with a
/// vector store collection holding document data and facilitates retrieval based on the specified
/// conversation context.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys for identifying records in the vector store. This type must be non-nullable.
/// </typeparam>
/// <typeparam name="TRecord">
/// The type of the data records processed by the plugin. This type must derive from AesirConversationDocumentTextData<TKey>.
/// </typeparam>
[Experimental("SKEXP0001")]
internal class SummarizeConversationDocumentPlugin<TKey, TRecord>(
    VectorStoreCollection<TKey, TRecord> vectorStoreCollection,
    string conversationId)
    where TKey : notnull
    where TRecord : AesirConversationDocumentTextData<TKey>
{
    /// Asynchronously retrieves and returns chunks of the document that represent a summary.
    /// The results are filtered based on the conversation ID and filename, and are returned as text search results.
    /// This function processes the document to provide selective summaries when the document size exceeds a specific limit.
    /// <param name="filename">
    /// The name of the file to be summarized. This parameter is required and cannot be null or empty.
    /// </param>
    /// <returns>
    /// A collection of text search results where each result contains a portion of the summarized document,
    /// along with details such as the reference link and description.
    /// </returns>
    [KernelFunction(name: "SummarizeDocument"), Description("Returns chunks of the document representing a summary.")]
    public async Task<IEnumerable<TextSearchResult>> GetChunksAsync(
        [Description("The filename of the document to be summarized.")]
        string filename)
    {
        ArgumentNullException.ThrowIfNull(filename);
        if (string.IsNullOrEmpty(filename))
        {
            throw new ArgumentException("Filename cannot be null or empty", nameof(filename));
        }

        var options = new FilteredRecordRetrievalOptions<TRecord>
        {
            Skip = 0,
            IncludeVectors = false,
            OrderBy = ob => ob.Ascending(r => r.CreatedAt)
        };

        Expression<Func<TRecord, bool>> filter = record =>
            record.ConversationId == conversationId;

        var results = (await vectorStoreCollection
                .GetAsync(filter, top: int.MaxValue, options)
                .ToListAsync())
            .Where(r => r.ReferenceDescription!.EndsWith(filename)).ToList();

        // just take a summary of the document
        var count = results.Count;

        if (count <= 25)
            return results.OrderBy(r => r.CreatedAt)
                .Select(r => new TextSearchResult(r.Text!)
                {
                    Link = r.ReferenceLink,
                    Name = r.ReferenceDescription
                });

        var quarterSize = count / 4;

        return results.Take(quarterSize)
            .Concat(results.Skip(quarterSize * 2)
                .Take(quarterSize))
            .Concat(results.Skip(quarterSize * 3))
            .OrderBy(r => r.CreatedAt)
            .Select(r => new TextSearchResult(r.Text!)
            {
                Link = r.ReferenceLink,
                Name = r.ReferenceDescription
            });
    }
}