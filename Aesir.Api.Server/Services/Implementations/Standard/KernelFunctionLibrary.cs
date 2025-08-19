using System.Diagnostics.CodeAnalysis;
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
/// A library providing a set of kernel functions for performing various data analysis and search operations on
/// textual data. The class is parameterized by a key type and a record type, enabling it to handle diverse data
/// structures in a flexible manner.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys for the records being processed. This type must be non-nullable.
/// </typeparam>
/// <typeparam name="TRecord">
/// The type of the data records being processed. The type must derive from AesirTextData<TKey>.
/// </typeparam>
[Experimental("SKEXP0001")]
public class KernelFunctionLibrary<TKey, TRecord>(
    ITextSearch textSearch,
    IKeywordHybridSearchable<TRecord>? hybridSearch)
    where TKey : notnull
    where TRecord : AesirTextData<TKey>
{
    /// Creates a kernel function for analyzing images to classify them as either 'document' or 'non-document'.
    /// If classified as a document, processes the image with OCR to extract text. If classified as non-document,
    /// provides a detailed visual description. This function is intended for image files like PNG, JPG, and BMP
    /// and is not suitable for other file types such as PDFs or text files.
    /// <param name="imageFilter">
    /// A filter to constrain the scope of the image analysis, based on custom search criteria.
    /// Pass null for no filtering.
    /// </param>
    /// <param name="top">
    /// The maximum number of results to return. Default is 5.
    /// </param>
    /// <returns>
    /// A kernel function that processes and analyzes the specified images, returning results that contain
    /// details such as name, value, and link for each analyzed image.
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

    /// <summary>
    /// Creates a kernel function that performs a hybrid document search based on the given query criteria.
    /// </summary>
    /// <param name="searchOptions">
    /// Optional search options of type <see cref="HybridSearchOptions{TRecord}"/> that define custom filtering or behavior for the hybrid search.
    /// </param>
    /// <param name="top">
    /// Optional parameter specifying the maximum number of results to return. Overrides the default result count if provided.
    /// </param>
    /// <returns>
    /// A <see cref="KernelFunction"/> that can be used to execute a hybrid search for documents. The search returns a collection of results
    /// with properties Name, Value, and Link that match the query.
    /// </returns>
    public KernelFunction GetHybridDocumentSearchFunction(HybridSearchOptions<TRecord>? searchOptions = null, int? top = null)
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
            
            var searchValue = string.IsNullOrEmpty(query?.ToString()) ?
                string.Join(" ", files) : query.ToString();
            
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

    /// <summary>
    /// Retrieves a semantic document search function for performing searches on a collection of documents
    /// using specified filtering and result count criteria.
    /// </summary>
    /// <param name="semanticSearchFilter">
    /// An optional filter used to apply specific constraints to the semantic search. If null, no filtering is applied.
    /// </param>
    /// <param name="top">
    /// Specifies the maximum number of top results to return. Default value is 5.
    /// </param>
    /// <returns>
    /// A <see cref="KernelFunction"/> that represents the semantic document search functionality,
    /// capable of searching content based on a query and returning relevant results.
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
                Description = "Perform a search for content related to the specified query. The search will return the name, value and link for the related content.",
                Parameters = [
                    new KernelParameterMetadata("query") { Description = "The search query string, supporting keywords, phrases, or natural language input for semantic matching.", ParameterType = typeof(string), IsRequired = true },
                    new KernelParameterMetadata("files") { Description = "The files to search.", ParameterType = typeof(string[]), IsRequired = true },
                    // new KernelParameterMetadata("count") { Description = "Maximum number of results to return (default: 25).", ParameterType = typeof(int), IsRequired = false, DefaultValue = 25 },
                    // new KernelParameterMetadata("skip") { Description = "Number of initial results to skip for pagination (default: 0).", ParameterType = typeof(int), IsRequired = false, DefaultValue = 0 },
                ],
                ReturnParameter = new KernelReturnParameterMetadata { ParameterType = typeof(KernelSearchResults<TextSearchResult>), Description = "A collection of search results, where each TextSearchResult contains properties like Name, Value, and Link."  },
            };
            
            return textSearch
                .CreateGetTextSearchResults(searchOptions: semanticTextSearchOptions, options: semanticSearchResultsFunctionOptions);        
    }

    /// <summary>
    /// Creates and returns a KernelFunction for performing web searches using the provided web search engine connector.
    /// </summary>
    /// <param name="webSearchEngineConnector">
    /// The connector instance for the desired web search engine, which allows executing web searches and retrieving results.
    /// </param>
    /// <returns>
    /// A KernelFunction object configured to perform web searches for content related to a specified query.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown when no valid web search function can be retrieved from the specified plugin.
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
            (from method in methods where method.Name.StartsWith("GetSearchResults") 
                select KernelFunctionFactory.CreateFromMethod(
                    method, webSearchPlugin, 
                    functionName: "PerformWebSearch", 
                    description: "Perform a web search for content related to the specified query. The search will return the name, value and link for the related content."
            )).ToList();

        var webSearchFunction = functions.FirstOrDefault();
        
        if(webSearchFunction == null)
            throw new Exception("Unable to find web search function");
        
        return webSearchFunction;
    }
}