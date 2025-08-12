using System;
using System.Threading.Tasks;
using Aesir.Client.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Browser.Services;

/// <summary>
/// Provides a service for displaying PDF documents in a browser environment.
/// </summary>
public class PdfViewerService(
    ILogger<PdfViewerService> logger,
    BrowserJsService browserJsService,
    INotificationService notificationService,
    IConfiguration configuration
) : IPdfViewerService
{
    /// <summary>
    /// Displays a PDF file in a new browser window, allowing the user to view its contents starting from a specified page number.
    /// </summary>
    /// <param name="fileUri">
    /// The URI of the PDF file to be displayed. The format should follow the pattern:
    /// file://guid/Aesir.pdf#page=1, where 'guid' represents the unique identifier,
    /// 'Aesir.pdf' is the file name, and 'page=1' specifies the page number to open.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation of displaying the PDF file.
    /// If the operation fails due to a browser blocking pop-ups, an error notification is shown.
    /// </returns>
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

    /// <summary>
    /// Parses a URI string in the Aesir format to extract the document ID, file name, and page number.
    /// </summary>
    /// <param name="aesirUri">The Aesir-formatted URI string to parse. The expected format is "file://guid/filename.pdf#page=number".</param>
    /// <returns>A tuple containing the extracted document ID, file name, and page number.
    /// The document ID is a string, the file name is a string, and the page number is an integer.</returns>
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