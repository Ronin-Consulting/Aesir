using System;
using System.IO;
using System.Threading.Tasks;
using Aesir.Client.Desktop.Controls;
using Aesir.Client.Desktop.ViewModels;
using Aesir.Client.Services;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using Ursa.Common;
using Ursa.Controls;
using Ursa.Controls.Options;

namespace Aesir.Client.Desktop.Services;

public class PdfViewerService(
    ILogger<PdfViewerService> logger,
    IDocumentCollectionService documentCollectionService,
    IDialogService dialogService
) : IPdfViewerService
{
    public async Task ShowPdfAsync(string fileUri)
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
            
            var image = await GetPdfImageAsync(fileUri);
            // if the image is null, then show dialog indicating bad file uri
            if (image == null)
            {
                await dialogService.ShowErrorDialogAsync("Invalid", "The file path is invalid.");
                return;
            }
            var viewModel = new PdfViewerControlViewModel();
            viewModel.SetPdfImageSource(image);
            
            var result = await Drawer.ShowModal<PdfViewerControl, PdfViewerControlViewModel>(
                viewModel, options: options);

            if (result == DialogResult.None)
                throw new ApplicationException("PdfViewerService.ShowPdfAsync failed.");
        }
    }
    
    private async Task<IImage?> GetPdfImageAsync(string fileUri)
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
            
            // If the file is PNG, load it directly
            if (Path.GetExtension(uri.LocalPath).Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                return new Bitmap(fileContentStream);
            }
            
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
        catch(Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching PDF page image.");
            return null;
        }
    }
}