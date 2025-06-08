using System;
using System.IO;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

public class DocumentCollectionService : IDocumentCollectionService
{
    private readonly ILogger<DocumentCollectionService> _logger;
    private readonly IFlurlClient _flurlClient;

    public DocumentCollectionService(ILogger<DocumentCollectionService> logger,
        IConfiguration configuration, IFlurlClientCache flurlClientCache)
    {
        _logger = logger;
        _flurlClient = flurlClientCache
            .GetOrAdd("DocumentCollectionClient",
                configuration.GetValue<string>("Inference:DocumentCollections"));
    }
    
    public async Task<Stream> GetStreamAsync(string filename)
    {
        try
        {
            //file/{filename}/content
            var response = (await _flurlClient.Request()
                .AppendPathSegment("file")
                .AppendPathSegment(filename)
                .AppendPathSegment("content")
                .GetAsync());
            
            return await response.GetStreamAsync();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 404)
        {
            throw new FileNotFoundException($"File '{filename}' not found on server.");
        }
        catch (FlurlHttpException ex)
        {
            throw new Exception($"Failed to get file stream for '{filename}': {ex.Message}", ex);
        }
    }

}