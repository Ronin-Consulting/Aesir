using Microsoft.SemanticKernel;

namespace Aesir.Api.Server.Services.Implementations.Standard;

public class AutoDocumentCollectionService : IDocumentCollectionService
{
    private readonly ILogger<AutoDocumentCollectionService> _logger;
    private readonly IConversationDocumentCollectionService _conversationDocumentCollectionService;
    private readonly IGlobalDocumentCollectionService _globalDocumentCollectionService;

    public AutoDocumentCollectionService(
        ILogger<AutoDocumentCollectionService> logger,
        IConversationDocumentCollectionService conversationDocumentCollectionService,
        IGlobalDocumentCollectionService globalDocumentCollectionService
    )
    {
        _logger = logger;
        _conversationDocumentCollectionService = conversationDocumentCollectionService;
        _globalDocumentCollectionService = globalDocumentCollectionService;
    }

    public Task LoadDocumentAsync(string documentPath, IDictionary<string, object>? fileMetaData = null,
        CancellationToken cancellationToken = default)
    {
        if(fileMetaData == null || !fileMetaData.TryGetValue("DocumentCollectionType", out var metaValue))
            throw new ArgumentException("File metadata must contain a DocumentCollectionType property");

        var documentCollectionType = (DocumentCollectionType)metaValue;
        return documentCollectionType switch
        {
            DocumentCollectionType.Conversation => _conversationDocumentCollectionService.LoadDocumentAsync(
                documentPath, fileMetaData, cancellationToken),
            DocumentCollectionType.Global => _globalDocumentCollectionService.LoadDocumentAsync(documentPath,
                fileMetaData, cancellationToken),
            _ => throw new ArgumentException("Invalid DocumentCollectionType")
        };
    }

    public Task<KernelPlugin> GetKernelPluginAsync(IDictionary<string, object>? kernelArguments = null)
    {
        if(kernelArguments == null || !kernelArguments.TryGetValue("DocumentCollectionType", out var metaValue))
            throw new ArgumentException("Kernel arguments must contain a DocumentCollectionType property");
        
        var documentCollectionType = (DocumentCollectionType)metaValue;
        
        return documentCollectionType switch
        {
            DocumentCollectionType.Conversation => _conversationDocumentCollectionService.GetKernelPluginAsync(kernelArguments),
            DocumentCollectionType.Global => _globalDocumentCollectionService.GetKernelPluginAsync(kernelArguments),
            _ => throw new ArgumentException("Invalid DocumentCollectionType")
        };
    }
}