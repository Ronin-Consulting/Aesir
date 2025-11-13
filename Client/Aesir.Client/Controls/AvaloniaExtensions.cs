using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using Ursa.Controls;

namespace Aesir.Client.Controls;

/// <summary>
/// Provides extension methods for Avalonia controls and frameworks to streamline workflow
/// with view models and clipboard functionality.
/// </summary>
public static class AvaloniaExtensions
{
    /// Configures a UserControl with the specified ViewModel and manages its lifecycle events.
    /// <param name="element">
    /// The UserControl to configure with the ViewModel.
    /// </param>
    /// <param name="viewModel">
    /// The ViewModel (of type ObservableRecipient) to bind to the UserControl. This ViewModel's lifecycle
    /// will be tied to the logical tree events of the UserControl (e.g., AttachedToLogicalTree, DetachedFromLogicalTree).
    /// </param>
    /// <returns>
    /// The UserControl instance with the ViewModel set as its DataContext.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the provided ViewModel is null.
    /// </exception>
    public static UserControl WithViewModel(this UserControl element, ObservableRecipient? viewModel)
    {
        if (viewModel == null) throw new InvalidOperationException();
        
        element.DataContext = viewModel;
        
        element.AttachedToLogicalTree += (sender, args) =>
        {
            viewModel.IsActive = true;
        };
        
        element.DetachedFromLogicalTree += (sender, args) =>
        {
            viewModel.IsActive = false;
        };

        return element;
    }

    /// Associates a view model with the specified window, sets the window's DataContext,
    /// and manages the activation state of the view model based on the window's lifecycle events.
    /// <param name="window">The window to associate with the view model.</param>
    /// <param name="viewModel">The view model to associate with the window. Cannot be null.</param>
    /// <returns>The window instance, with the associated view model set as its DataContext.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the provided view model is null.</exception>
    public static Window WithViewModel(this Window window, ObservableRecipient? viewModel)
    {
        if (viewModel == null) throw new InvalidOperationException();
        
        window.DataContext = viewModel;
        
        window.Opened += (sender, args) =>
        {
            viewModel.IsActive = true;
        };
        
        window.Closed += (sender, args) =>
        {
            viewModel.IsActive = false;
        };

        return window;
    }
    
    public static UrsaView WithViewModel(this UrsaView view, ObservableRecipient? viewModel)
    {
        if (viewModel == null) throw new InvalidOperationException();
        
        view.DataContext = viewModel;
        
        viewModel.IsActive = true;

        return view;
    }

    /// <summary>
    /// Retrieves the clipboard instance associated with the application,
    /// depending on the application's lifetime (classic desktop or single view).
    /// </summary>
    /// <param name="viewModel">The view model instance that invokes the method.</param>
    /// <returns>An instance of <see cref="IClipboard"/> representing the clipboard functionality of the application.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the clipboard functionality is not supported or the application lifetime cannot be determined.
    /// </exception>
    public static IClipboard GetClipboard(this ObservableRecipient viewModel)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow!.Clipboard ?? throw new InvalidOperationException();
        }

        if (Application.Current?.ApplicationLifetime is not ISingleViewApplicationLifetime browser)
            throw new InvalidOperationException("Clipboard not supported");
        
        var visualRoot = browser.MainView!.GetVisualRoot();
        if (visualRoot is TopLevel topLevel) {
            return topLevel.Clipboard!;
        }

        throw new InvalidOperationException("Clipboard not supported");
    }
}