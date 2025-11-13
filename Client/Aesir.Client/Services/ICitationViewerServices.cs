using System.Threading.Tasks;

namespace Aesir.Client.Services;

/// <summary>
/// Defines a service for viewing PDF documents within the application.
/// </summary>
public interface ICitationViewerService
{
    /// <summary>
    /// Asynchronously displays a PDF file in a viewer with specified options.
    /// </summary>
    /// <param name="fileUri">The URI of the PDF file to display.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ShowCitationAsync(string fileUri);
}