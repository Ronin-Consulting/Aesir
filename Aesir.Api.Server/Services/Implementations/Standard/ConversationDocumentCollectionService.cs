using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;

namespace Aesir.Api.Server.Services.Implementations.Standard;

[Experimental("SKEXP0070")]
public class ConversationDocumentCollectionService : IConversationDocumentCollectionService
{
    private readonly VectorStoreTextSearch<AesirConversationDocumentTextData<Guid>> _conversationDocumentTextSearch;

    public ConversationDocumentCollectionService(VectorStoreTextSearch<AesirConversationDocumentTextData<Guid>> conversationDocumentTextSearch)
    {
        _conversationDocumentTextSearch = conversationDocumentTextSearch;
    }

    public async Task<KernelPlugin> GetKernelPlugin(string? conversationId)
    {
        await Task.CompletedTask;
        
        var conversationFilter = new TextSearchFilter();
        conversationFilter.Equality(nameof(AesirConversationDocumentTextData<Guid>.ConversationId), conversationId);
        
        var globalDocumentTextSearchOptions = new TextSearchOptions
        {
            Top = 5,
            Filter = conversationFilter
        };
            
        var conversationDocumentSearchPlugin = _conversationDocumentTextSearch
            .CreateGetTextSearchResults(searchOptions: globalDocumentTextSearchOptions);

        return KernelPluginFactory.CreateFromFunctions(
            "ChatDocSearch",
            "Search and extract relevant information from chat conversation documents uploaded by the user during a conversation. It is designed to query the content of these impromptu text-based documents, such as transcripts or message logs, to retrieve details based on user-specified criteria, including keywords, topics, participants, or timestamps. Use this tool when the task involves analyzing or retrieving information from user-uploaded chat conversation documents.",
            [conversationDocumentSearchPlugin]
        );
    }
}