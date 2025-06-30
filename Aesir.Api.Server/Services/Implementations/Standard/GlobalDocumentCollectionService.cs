using Microsoft.SemanticKernel;

namespace Aesir.Api.Server.Services.Implementations.Standard;

public class GlobalDocumentCollectionService : IGlobalDocumentCollectionService
{
    public Task LoadDocumentAsync(string documentPath, IDictionary<string, object>? fileMetaData = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<KernelPlugin> GetKernelPluginAsync(IDictionary<string, object>? kernelArguments = null)
    {
        throw new NotImplementedException();
    }
}