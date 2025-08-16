using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Aesir.Api.Server.Extensions;
using Aesir.Api.Server.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Plugins.Web;

namespace Aesir.Api.Server.Services.Implementations.Standard;

[Experimental("SKEXP0001")]
public class KernelFunctionLibrary<TKey, TRecord>(
    ITextSearch textSearch,
    IKeywordHybridSearchable<TRecord>? hybridSearch)
    where TKey : notnull
    where TRecord : AesirTextData<TKey>
{
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
            if (string.IsNullOrEmpty(query?.ToString()))
            {
                return [];
            }

            // NOTE: how to process the files parameter if we wanted to
            // var files = new List<string>();
            // arguments.TryGetValue("files", out var filesValue);
            // if (filesValue is JsonElement jsonElement)
            // {
            //     files = jsonElement.EnumerateArray().Select(x => x.GetString()).ToList()!;
            // }
            //
            // // if only a single file then determine if we can just load all of its text
            // if (files.Count == 1)
            // {
            //     var found = await _vectorStoreRecordCollection.GetAsync(filter: data => data.ConversationId == conversationId,
            //         int.MaxValue, cancellationToken: cancellationToken).ToListAsync(cancellationToken).ConfigureAwait(false);
            //     
            //     // for now total up the tokens.. if less than 16K then return results
            //     if (found.Sum(r => r.TokenCount) <= 16384)
            //     {
            //         return found.OrderBy(r => r.CreatedAt).Select(r =>
            //             new TextSearchResult(r.Text!)
            //             {
            //                 Link = r.ReferenceLink,
            //                 Name = r.ReferenceDescription
            //             }
            //         );
            //     }
            // }

            if (searchOptions != null)
                searchOptions.Skip = skip;

            var searchValue = query.ToString()!;
            var keywords = searchValue.KeywordsOnly();
            var results = await hybridSearch.HybridSearchAsync(
                searchValue,
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

            return results.Where(r => r.Score >= 0.5f).Select(r =>
                new TextSearchResult(r.Record.Text!)
                {
                    Link = r.Record.ReferenceLink,
                    Name = r.Record.ReferenceDescription
                }
            );
        }
    }

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