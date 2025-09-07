using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aesir.Client.Desktop.ViewModels;

/// <summary>
/// Represents the ViewModel for the Citation Viewer Control.
/// Manages the logic for handling citation content, including images and text,
/// and provides interaction mechanisms for zoom functionality.
/// </summary>
public partial class CitationViewerControlViewModel() : ObservableRecipient
{
    /// <summary>
    /// Represents the source of the image displayed in the citation viewer.
    /// This property holds an instance of <see cref="IImage"/> which provides the image data
    /// to be rendered in the view.
    /// </summary>
    [ObservableProperty] private IImage? _citationImageSource;

    /// <summary>
    /// Represents the source of the citation text being displayed in the Citation Viewer Control.
    /// This property is used to bind or retrieve the current citation text source, allowing the UI to reflect changes accordingly.
    /// </summary>
    [ObservableProperty] private string? _citationTextSource;

    /// <summary>
    /// Represents the current zoom level for the citation viewer as a percentage.
    /// The zoom level is stored as a string and defaults to "100%".
    /// </summary>
    [ObservableProperty] private string _zoomPercentage = "100%";

    /// <summary>
    /// Determines whether the citation source is an image.
    /// </summary>
    [ObservableProperty] private bool _isImage = true;

    /// <summary>
    /// Represents the instance of an implementation of the <see cref="IZoomApi"/> interface
    /// used to manage zoom functionality within the Citation Viewer control.
    /// </summary>
    /// <remarks>
    /// This variable is assigned via the <see cref="SetZoomApi"/> method and is utilized by
    /// commands such as <see cref="DecrementZoomButtonClick"/> and <see cref="IncrementZoomButtonClick"/>
    /// to adjust the zoom level of the viewer.
    /// </remarks>
    private IZoomApi? _zoomApi;

    /// <summary>
    /// Sets the image source for displaying a citation in the citation viewer.
    /// Updates the viewer state to indicate that it is currently displaying an image and assigns the provided image source.
    /// </summary>
    /// <param name="citationImageSource">The image source to be displayed as the citation.</param>
    public void SetCitationImageSource(IImage citationImageSource)
    {
        IsImage = true;
        CitationImageSource = citationImageSource;
    }

    /// <summary>
    /// Sets the citation text source for the citation viewer.
    /// Marks the viewer as displaying text rather than an image.
    /// Updates the citation text source with the specified text content.
    /// </summary>
    /// <param name="citationTextSource">The string content representing the text citation to be displayed in the viewer.</param>
    public void SetCitationTextSource(string citationTextSource)
    {
        IsImage = false;
        CitationTextSource = citationTextSource;
    }

    /// <summary>
    /// Sets the implementation of the <see cref="IZoomApi"/> interface for managing zoom functionality.
    /// This allows the ViewModel to delegate zoom operations to the provided implementation.
    /// </summary>
    /// <param name="zoomApi">An implementation of the <see cref="IZoomApi"/> interface to manage zooming capabilities.</param>
    public void SetZoomApi(IZoomApi zoomApi)
    {
        _zoomApi = zoomApi;
    }

    /// <summary>
    /// Invokes the zoom-out functionality in the associated implementation of the <see cref="IZoomApi"/> interface, if available.
    /// Designed to decrease the zoom level of the citation viewer by a predefined step.
    /// Relies on the <see cref="DecrementZoom"/> method of the provided <see cref="IZoomApi"/> implementation.
    /// </summary>
    [RelayCommand]
    private void DecrementZoomButtonClick()
    {
        _zoomApi?.DecrementZoom();
    }

    /// <summary>
    /// Triggers an increase in the zoom level of the PDF viewer through the associated <see cref="IZoomApi"/> implementation.
    /// Delegates the zoom-in functionality to the <see cref="IZoomApi.IncrementZoom"/> method, if the API is available.
    /// </summary>
    [RelayCommand]
    private void IncrementZoomButtonClick()
    {
        _zoomApi?.IncrementZoom();
    }
}

/// <summary>
/// Provides functionalities for controlling the zoom level, such as incrementing or decrementing the zoom percentage.
/// This interface is typically used in conjunction with UI elements that allow users to zoom in or out of content,
/// such as PDF viewers or image controls.
/// </summary>
public interface IZoomApi
{
    /// <summary>
    /// Decreases the zoom level by triggering the corresponding method in the assigned implementation of <see cref="IZoomApi"/>.
    /// Ensures that the zoom operation is handled only if a valid zoom API instance is set.
    /// </summary>
    void DecrementZoom();

    /// <summary>
    /// Increases the zoom level of the associated PDF viewer or image viewer by a predefined step.
    /// Ensures the zoom level does not exceed the maximum allowable value.
    /// Updates the zoom display percentage and delegates the operation to the implementation
    /// of the <see cref="IZoomApi"/> interface, if available.
    /// </summary>
    /// <remarks>
    /// Invokes the corresponding zoom-in functionality in the <see cref="IZoomApi"/> implementation.
    /// If no implementation is available, no operation is performed.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the associated Zoom API implementation is not properly configured or accessible.
    /// </exception>
    void IncrementZoom();
}