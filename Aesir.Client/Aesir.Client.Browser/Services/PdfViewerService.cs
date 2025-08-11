using System;
using System.Threading.Tasks;
using Aesir.Client.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Browser.Services;

public class PdfViewerService(
    ILogger<PdfViewerService> logger,
    BrowserJsService browserJsService,
    INotificationService notificationService,
    IConfiguration configuration
) : IPdfViewerService
{
    public async Task ShowPdfAsync(string fileUri)
    {
        // fileUri should be like file://guid/Aesir.pdf#page=1
        try
        {
            var (id, fileName, pageNumber) = ParseAesirUri(fileUri);
            
            var documentCollectionsBaseUrl = configuration.GetValue<string>("Inference:DocumentCollections");
            var encodedId = Uri.EscapeDataString(id);
            var url = $"{documentCollectionsBaseUrl}/file/{encodedId}/{fileName}#page={pageNumber}"; 
            
            var opened = await browserJsService.OpenNewWindowAsync(url);
            if (!opened)
                notificationService.ShowErrorNotification("Popup Blocked", "Your browser may be blocking pop-ups.");
            
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching PDF page image.");
        }
    }
    
    private (string id, string filePath, int pageNumber) ParseAesirUri(string aesirUri)
    {
        var trimmed = aesirUri.Substring("file://".Length);
        
        // Example trimmed = "guid/filename.pdf#page=2"
        var hashIndex = trimmed.IndexOf("#page=");
        var pageNumber = 1;
        if (hashIndex >= 0)
        {
            int.TryParse(trimmed.Substring(hashIndex + 6), out pageNumber);
            trimmed = trimmed.Substring(0, hashIndex);
        }
        
        // Handle ids with or without leading slash
        var hasLeadingSlash = trimmed.StartsWith("/");
        var slashIndex = hasLeadingSlash ? trimmed.IndexOf('/', 1) : trimmed.IndexOf('/');
        var (id, filename) = slashIndex > 0 
            ? (trimmed.Substring(0, slashIndex), trimmed.Substring(slashIndex + 1))
            : (trimmed, "");

        return (id, filename, pageNumber);
    }
}