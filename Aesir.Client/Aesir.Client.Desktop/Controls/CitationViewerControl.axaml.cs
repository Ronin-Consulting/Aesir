using Avalonia.Controls;
using Aesir.Client.Desktop.ViewModels;
using Avalonia;
using Avalonia.Controls.PanAndZoom;

namespace Aesir.Client.Desktop.Controls;

public partial class CitationViewerControl : UserControl
{
    public CitationViewerControl()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        var viewModel = DataContext as CitationViewerControlViewModel;
        viewModel!.SetZoomApi(new ZoomApiImpl(CitationZoomBorder));
        
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