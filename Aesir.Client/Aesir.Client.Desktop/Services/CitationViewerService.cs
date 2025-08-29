using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aesir.Client.Desktop.Controls;
using Aesir.Client.Desktop.ViewModels;
using Aesir.Client.Services;
using Aesir.Client.Services.Implementations.Standard;
using Aesir.Common.FileTypes;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using Ursa.Common;
using Ursa.Controls;
using Ursa.Controls.Options;

namespace Aesir.Client.Desktop.Services;

/// <summary>
/// Provides functionality to display PDF documents in a desktop application.
/// This service allows asynchronous rendering and viewing of PDF files within a modal drawer interface.
/// </summary>
public class CitationViewerService(
    ILogger<CitationViewerService> logger,
    IDocumentCollectionService documentCollectionService,
    IDialogService dialogService
) : ICitationViewerService
{
    private readonly SemaphoreSlim _modalSemaphore = new(1, 1);
    
    /// <summary>
    /// Displays a PDF file in a viewer using the provided file URI.
    /// </summary>
    /// <param name="fileUri">The URI of the PDF file to be displayed.</param>
    /// <returns>A task that represents the asynchronous operation of showing the PDF.</returns>
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
            
                var image = await GetCitationImageAsync(fileUri);
                // if the image is null, then show dialog indicating bad file uri
                if (image == null)
                {
                    await dialogService.ShowErrorDialogAsync("Bad Citation", "The citation requires regeneration. Retry AI query.");
                    return;
                }
                var viewModel = new CitationViewerControlViewModel();
                viewModel.SetCitationImageSource(image);
            
                var result = await Drawer.ShowModal<CitationViewerControl, CitationViewerControlViewModel>(
                    viewModel, options: options);

                if (result == DialogResult.None)
                    throw new ApplicationException("PdfViewerService.ShowPdfAsync failed.");
            }
        }
        finally
        {
            _modalSemaphore.Release();
        }
    }

    /// <summary>
    /// Retrieves an image representation of the specified page from a PDF document.
    /// </summary>
    /// <param name="fileUri">The URI of the PDF file, which may include a page number fragment (e.g., file://path/document.pdf#page=1).</param>
    /// <returns>An <see cref="IImage"/> object representing the specified PDF page, or null if the operation fails or the file is invalid.</returns>
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
                    using var image = Image.Load<Rgba32>(fileContentStream);
                    
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
                    using var image = Image.Load<Rgba32>(fileContentStream);
                    using var memoryStream = new MemoryStream();
                    await image.SaveAsPngAsync(memoryStream); // Convert to PNG for Avalonia
                    memoryStream.Position = 0;
                    
                    return new Bitmap(memoryStream);
                }
            }

            if (FileTypeManager.IsTextFile(uri.LocalPath))
            {
                // get the fileContentStream as a string
                using var reader = new StreamReader(fileContentStream);
                var content = await reader.ReadToEndAsync();

                var textFileToPndConverter = new TextFileToPngConverter();
                return await textFileToPndConverter.ConvertToPngAsync(content, uri.LocalPath.GetMimeType());
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
            logger.LogError(ex, "Error occurred while fetching PDF page image.");
            return null;
        }
    }
}