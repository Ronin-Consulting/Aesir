using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aesir.Client.Desktop.ViewModels;

public partial class PdfViewerControlViewModel() : ObservableRecipient
{
    [ObservableProperty] private IImage? _pdfImageSource;

    [ObservableProperty] private string _zoomPercentage = "100%";
    private int _zoomLevel = 100;
    
    private IZoomApi? _zoomApi;
    
    public void SetPdfImageSource(IImage pdfImageSource)
    {
        PdfImageSource = pdfImageSource;
    }
    
    public void SetZoomApi(IZoomApi zoomApi)
    {
        _zoomApi = zoomApi;
    }
    
    [RelayCommand]
    private void DecrementZoomButtonClick()
    {
        _zoomLevel -= 25;
        _zoomLevel = Math.Max(_zoomLevel, 0);
        
        _zoomApi?.DecrementZoom();
        
        ZoomPercentage = $"{_zoomLevel}%";
    }
    
    [RelayCommand]
    private void IncrementZoomButtonClick()
    {
        _zoomLevel += 25;
        _zoomLevel = Math.Min(_zoomLevel, 250);
        
        _zoomApi?.IncrementZoom();
        
        ZoomPercentage = $"{_zoomLevel}%";
    }
}

public interface IZoomApi
{
    void DecrementZoom();
    void IncrementZoom();
}