using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aesir.Client.Desktop.ViewModels;

/// <summary>
/// Represents the view model for the PDF Viewer control in the application.
/// This class is responsible for managing the display of PDF images and coordinating
/// zooming functionality through the provided APIs.
/// </summary>
public partial class PdfViewerControlViewModel() : ObservableRecipient
{
    /// <summary>
    /// Represents the image source to be displayed in the PDF viewer control.
    /// </summary>
    /// <remarks>
    /// The property is used to render the current page of the PDF document in the viewer.
    /// It may be null if there is no image to display.
    /// </remarks>
    [ObservableProperty] private IImage? _pdfImageSource;

    /// <summary>
    /// Represents the zoom percentage of the PDF viewer as a string, such as "100%".
    /// </summary>
    [ObservableProperty] private string _zoomPercentage = "100%";

    /// <summary>
    /// Represents the current zoom level of the PDF viewer as an integer value.
    /// The zoom level is expressed as a percentage where 100 corresponds to the default scale.
    /// This variable is updated whenever zoom operations (increment or decrement)
    /// are performed and is used to reflect the current zoom percentage.
    /// </summary>
    private int _zoomLevel = 100;

    /// <summary>
    /// A private field that holds a reference to the implementation of the IZoomApi interface.
    /// The IZoomApi provides functionality for incrementing and decrementing the zoom level
    /// in a PDF viewer context.
    /// </summary>
    private IZoomApi? _zoomApi;

    /// <summary>
    /// Sets the image source for the PDF viewer.
    /// </summary>
    /// <param name="pdfImageSource">The image source representing the PDF content.</param>
    public void SetPdfImageSource(IImage pdfImageSource)
    {
        PdfImageSource = pdfImageSource;
    }

    /// <summary>
    /// Assigns an implementation of the IZoomApi interface to handle zoom operations for the PDF viewer.
    /// </summary>
    /// <param name="zoomApi">The implementation of IZoomApi to be used for zooming functionalities.</param>
    public void SetZoomApi(IZoomApi zoomApi)
    {
        _zoomApi = zoomApi;
    }

    /// <summary>
    /// Handles the click event for the Decrement Zoom button.
    /// Reduces the current zoom level by a fixed amount (25) and ensures the zoom level does not go below 0.
    /// Updates the zoom display percentage and notifies the associated zoom API to apply the zoom decrement.
    /// </summary>
    [RelayCommand]
    private void DecrementZoomButtonClick()
    {
        _zoomLevel -= 25;
        _zoomLevel = Math.Max(_zoomLevel, 0);
        
        _zoomApi?.DecrementZoom();
        
        ZoomPercentage = $"{_zoomLevel}%";
    }

    /// <summary>
    /// Handles the click event for the "Increment Zoom" button, increasing the zoom level
    /// and updating the displayed zoom percentage.
    /// </summary>
    /// <remarks>
    /// This method increments the zoom level by 25%, up to a maximum limit of 250%.
    /// It also updates the bound ZoomPercentage property and invokes the increment action
    /// on the associated IZoomApi instance, if available.
    /// </remarks>
    [RelayCommand]
    private void IncrementZoomButtonClick()
    {
        _zoomLevel += 25;
        _zoomLevel = Math.Min(_zoomLevel, 250);
        
        _zoomApi?.IncrementZoom();
        
        ZoomPercentage = $"{_zoomLevel}%";
    }
}

/// <summary>
/// Defines methods for managing zoom functionality in a UI component.
/// </summary>
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