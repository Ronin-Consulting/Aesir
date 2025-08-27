using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aesir.Client.Desktop.ViewModels;

public partial class CitationViewerControlViewModel() : ObservableRecipient
{
    [ObservableProperty] private IImage? _citationImageSource;
    
    [ObservableProperty] private string _zoomPercentage = "100%";
    
    private int _zoomLevel = 100;
    
    private IZoomApi? _zoomApi;
    
    public void SetCitationImageSource(IImage citationImageSource)
    {
        CitationImageSource = citationImageSource;
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
    /// <summary>
    /// Decreases the zoom level of the PDF viewer by a predefined step value.
    /// Ensures the zoom level does not go below the minimum allowable value.
    /// Updates the zoom display percentage and notifies the associated ViewModel.
    /// Invokes the corresponding zoom-out functionality in the implementation of the <see cref="IZoomApi"/> interface, if available.
    /// </summary>
    void DecrementZoom();

    /// <summary>
    /// Increments the zoom level of the PDF viewer by a fixed amount, up to a maximum allowable limit.
    /// </summary>
    /// <remarks>
    /// This method increases the zoom level by 25% each time it is called, ensuring it does not exceed
    /// the maximum zoom level of 250%. It updates the internal zoom percentage property and delegates
    /// the actual zoom functionality to the implementation of <see cref="IZoomApi"/> if available.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the underlying zoom API is not set or accessible when the method is called.
    /// </exception>
    void IncrementZoom();
}