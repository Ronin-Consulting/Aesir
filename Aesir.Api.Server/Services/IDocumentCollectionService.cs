using Microsoft.SemanticKernel;

namespace Aesir.Api.Server.Services;

public interface IDocumentCollectionService
{
    Task LoadDocumentAsync(string documentPath, IDictionary<string, object>? fileMetaData = null, CancellationToken cancellationToken = default);
    Task<KernelPlugin> GetKernelPluginAsync(IDictionary<string, object>? kernelArguments = null);
}

public enum DocumentCollectionType
{
    Conversation,
    Global
}

public class GlobalDocumentCollectionArgs : Dictionary<string, object>
{
    public static readonly GlobalDocumentCollectionArgs Default = new();
    
    public DocumentCollectionType DocumentCollectionType => (DocumentCollectionType) this["DocumentCollectionType"];

    private GlobalDocumentCollectionArgs()
    {
        this["DocumentCollectionType"] = DocumentCollectionType.Global;
    }

    public void AddCategoryId(string categoryId)
    {
        this["CategoryId"] = categoryId;   
    }
}

public class ConversationDocumentCollectionArgs : Dictionary<string, object>
{
    public static readonly ConversationDocumentCollectionArgs Default = new();
    
    public DocumentCollectionType DocumentCollectionType => (DocumentCollectionType) this["DocumentCollectionType"];
    
    private ConversationDocumentCollectionArgs()
    {
        this["DocumentCollectionType"] = DocumentCollectionType.Conversation;
    }
    
    public void AddConversationId(string conversationId)
    {
        this["ConversationId"] = conversationId;   
    }
}