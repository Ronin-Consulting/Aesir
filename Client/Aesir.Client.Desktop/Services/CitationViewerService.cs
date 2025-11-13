using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aesir.Client.Desktop.Controls;
using Aesir.Client.Desktop.ViewModels;
using Aesir.Client.Services;
using Aesir.Common.FileTypes;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SkiaSharp;
using Ursa.Common;
using Ursa.Controls;
using Ursa.Controls.Options;

namespace Aesir.Client.Desktop.Services;

/// <summary>
/// A service dedicated to handling the viewing of citation documents in PDF format within a desktop application's interface.
/// Manages asynchronous operations for rendering and displaying documents, ensuring the viewer operates within a controlled context.
/// </summary>
public class CitationViewerService(
    ILogger<CitationViewerService> logger,
    IDocumentCollectionService documentCollectionService,
    IDialogService dialogService
) : ICitationViewerService
{
    /// <summary>
    /// A semaphore used to control access to the modal drawer interface, ensuring that only one modal
    /// is displayed at a time. It uses a concurrency limit of 1, enforcing sequential access.
    /// </summary>
    private readonly SemaphoreSlim _modalSemaphore = new(1, 1);

    /// <summary>
    /// Displays a citation in the form of a PDF file in a viewer using the specified file URI.
    /// </summary>
    /// <param name="fileUri">The URI of the citation PDF file to be displayed.</param>
    /// <returns>A task that represents the asynchronous operation of showing the citation.</returns>
    public async Task ShowCitationAsync(string fileUri)
    {
        if (!await _modalSemaphore.WaitAsync(0)) // Try to acquire immediately, don't wait
        {
            return; // Modal is already open, prevent opening another one
        }

        try
        {
            if (Application.Current != null &&
                Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var options = new DrawerOptions()
                {
                    Position = Position.Right,
                    Buttons = DialogButton.None,
                    CanLightDismiss = true,
                    IsCloseButtonVisible = true,
                    Title = "Citation Viewer",
                    CanResize = false
                };
            
                var viewModel = new CitationViewerControlViewModel();
                
                var uri = new Uri(fileUri);
                if (uri.LocalPath.IsTextFile())
                {
                    var text = await GetCitationTextAsync(fileUri);
                    // if the text is null, then show dialog indicating bad file uri
                    if (text == null)
                    {
                        await ShowBadCitationDialog();
                        return;
                    }
                    
                    viewModel.SetCitationTextSource(text);
                }
                else
                {
                    var image = await GetCitationImageAsync(fileUri);
                    // if the image is null, then show dialog indicating bad file uri
                    if (image == null)
                    {
                        await ShowBadCitationDialog();
                        return;
                    }
                
                    viewModel.SetCitationImageSource(image);    
                }
                
                var result = await Drawer.ShowModal<CitationViewerControl, CitationViewerControlViewModel>(
                    viewModel, options: options);

                if (result == DialogResult.None)
                    throw new ApplicationException("PdfViewerService.ShowPdfAsync failed.");
                
                Task ShowBadCitationDialog()
                {
                    return dialogService.ShowErrorDialogAsync("Bad Citation", "The citation requires regeneration. Retry AI query.");
                }
            }
        }
        finally
        {
            _modalSemaphore.Release();
        }
    }

    /// <summary>
    /// Retrieves an image representation of a specific page from a PDF document provided by its URI.
    /// </summary>
    /// <param name="fileUri">
    /// The URI of the PDF file, which may include a page number specified in the fragment
    /// (e.g., file://path/document.pdf#page=1).
    /// </param>
    /// <returns>
    /// An <see cref="IImage"/> object representing the specified page from the PDF document,
    /// or null if an error occurs, the file is invalid, or the file type is unsupported.
    /// </returns>
    private async Task<IImage?> GetCitationImageAsync(string fileUri)
    {
        // fileUri should be like file://guid/Aesir.pdf#page=1
        try
        {
            var uri = new Uri(fileUri);

            if (!int.TryParse(uri.Fragment.TrimStart('#', 'p', 'a', 'g', 'e', '='), out var pageNumber))
            {
                pageNumber = 1;
            }

            await using var fileContentStream = await documentCollectionService.GetFileContentStreamAsync(uri.LocalPath);
            
            if (FileTypeManager.IsImage(uri.LocalPath))
            {
                var mimeType = uri.LocalPath.GetMimeType();
                
                if (mimeType == FileTypeManager.MimeTypes.Tiff)
                {
                    // Handle multi-page TIFF files
                    using var image = await Image.LoadAsync(fileContentStream);
                    
                    // Get the specific page (frame) from the TIFF
                    var frameIndex = Math.Max(0, Math.Min(pageNumber - 1, image.Frames.Count - 1));
                    
                    using var singleFrameImage = image.Frames.CloneFrame(frameIndex);
                    using var memoryStream = new MemoryStream();
                    await singleFrameImage.SaveAsPngAsync(memoryStream);
                    memoryStream.Position = 0;
                    
                    return new Bitmap(memoryStream);
                }
                else
                {
                    // Handle other image types
                    using var image = await Image.LoadAsync(fileContentStream);
                    using var memoryStream = new MemoryStream();
                    await image.SaveAsPngAsync(memoryStream); // Convert to PNG for Avalonia
                    memoryStream.Position = 0;
                    
                    return new Bitmap(memoryStream);
                }
            }
            
            if (FileTypeManager.MimeTypes.Pdf == uri.LocalPath.GetMimeType())
            {
                // Render the first page (pageIndex: 0) to a SKBitmap        
#pragma warning disable CA1416
                var skBitmap = PDFtoImage.Conversion.ToImage(fileContentStream, pageNumber - 1);
#pragma warning restore CA1416

                // Convert SKBitmap to an Avalonia Bitmap
                using var memoryStream = new MemoryStream();
            
                skBitmap.Encode(SKEncodedImageFormat.Png, 100).SaveTo(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
            
                return new Bitmap(memoryStream);
            }
            
            throw new NotSupportedException("File type not supported.");
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching PDF page or image page.");
            return null;
        }
    }

    /// <summary>
    /// Asynchronously retrieves the text content of a citation file from the given file URI.
    /// </summary>
    /// <param name="fileUri">The URI of the citation file whose text content needs to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the text content of the file, or null if an error occurs.</returns>
    private async Task<string?> GetCitationTextAsync(string fileUri)
    {
        try
        {
            var uri = new Uri(fileUri);
            await using var fileContentStream =
                await documentCollectionService.GetFileContentStreamAsync(uri.LocalPath);
            using var reader = new StreamReader(fileContentStream);
            return await reader.ReadToEndAsync();
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching text file.");
            return null;
        }
    }
}