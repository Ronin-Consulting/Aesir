using Microsoft.SemanticKernel;

namespace Aesir.Api.Server.Services;

public interface IDocumentCollectionService
{
    Task LoadDocumentAsync(string documentPath, IDictionary<string, object>? fileMetaData = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteDocumentAsync(IDictionary<string, object>? fileMetaData,
        CancellationToken cancellationToken = default);
    KernelPlugin GetKernelPlugin(IDictionary<string, object>? kernelPluginArguments = null);
}

public enum DocumentCollectionType
{
    Conversation,
    Global
}

public class GlobalDocumentCollectionArgs : Dictionary<string, object>
{
    public static GlobalDocumentCollectionArgs Default => new();
    
    public DocumentCollectionType DocumentCollectionType => (DocumentCollectionType) this["DocumentCollectionType"];

    private GlobalDocumentCollectionArgs()
    {
        this["DocumentCollectionType"] = DocumentCollectionType.Global;
    }

    public void SetCategoryId(string categoryId)
    {
        this["CategoryId"] = categoryId;   
    }
}

public class ConversationDocumentCollectionArgs : Dictionary<string, object>
{
    public static ConversationDocumentCollectionArgs Default => new();
    
    public DocumentCollectionType DocumentCollectionType => (DocumentCollectionType) this["DocumentCollectionType"];
    
    private ConversationDocumentCollectionArgs()
    {
        this["DocumentCollectionType"] = DocumentCollectionType.Conversation;
    }
    
    public void SetConversationId(string conversationId)
    {
        this["ConversationId"] = conversationId;   
    }
}

public static class SupportedFileContentTypes
{
    public static readonly string PdfContentType = "application/pdf";
}