using Microsoft.SemanticKernel;

namespace Aesir.Api.Server.Services;

public interface IConversationDocumentCollectionService
{
    Task<KernelPlugin> GetKernelPlugin(string? conversationId);
}