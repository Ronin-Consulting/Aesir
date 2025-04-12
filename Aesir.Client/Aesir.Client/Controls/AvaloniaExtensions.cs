using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aesir.Client.Controls;

public static class AvaloniaExtensions
{
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