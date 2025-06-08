using System;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia.Base;

namespace Aesir.Client.Desktop.ViewModels;

public partial class PdfViewerControlViewModel() : ObservableRecipient, ISetFullApi<string>, IInput
{
    public string InputValue { get; set; } = null!;
    private IFullApi<string>? _fullApi;

    [ObservableProperty] private IImage? _pdfImageSource;

    [ObservableProperty] private string _zoomPercentage = "100%";
    private int _zoomLevel = 100;
    
    private IZoomApi? _zoomApi;
    
    public void SetPdfImageSource(IImage pdfImageSource)
    {
        PdfImageSource = pdfImageSource;
    }

    public void SetFullApi(IFullApi<string>? fullApi)
    {
        _fullApi = fullApi;
    }

    public void SetZoomApi(IZoomApi zoomApi)
    {
        _zoomApi = zoomApi;
    }

    [RelayCommand]
    private async Task ButtonClickAsync(string parameter)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _fullApi!.SetButtonResult(parameter);
            _fullApi.Close();
        });
    }

    [RelayCommand]
    private void DecrementZoomButtonClick()
    {
        _zoomLevel -= 10;
        _zoomLevel = Math.Max(_zoomLevel, 0);
        
        _zoomApi?.DecrementZoom();
        
        ZoomPercentage = $"{_zoomLevel}%";
    }
    
    [RelayCommand]
    private void IncrementZoomButtonClick()
    {
        _zoomLevel += 10;
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