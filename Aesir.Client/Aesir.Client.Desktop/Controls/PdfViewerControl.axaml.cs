using Avalonia.Controls;
using Aesir.Client.Desktop.ViewModels;
using Avalonia;
using Avalonia.Controls.PanAndZoom;

namespace Aesir.Client.Desktop.Controls;

public partial class PdfViewerControl : UserControl
{
    public PdfViewerControl()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        var viewModel = DataContext as PdfViewerControlViewModel;
        viewModel!.SetZoomApi(new ZoomApiImpl(PdfZoomBorder));
        
        base.OnAttachedToVisualTree(e);
    }
}

public class ZoomApiImpl(ZoomBorder zoomBorder) : IZoomApi
{
    public void DecrementZoom()
    {
        zoomBorder.ZoomOut();
    }
    
    public void IncrementZoom()
    {
        zoomBorder.ZoomIn();
    }
}