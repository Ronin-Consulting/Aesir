using System.Linq;
using Aesir.Client.Models;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Controls;

public partial class Conversation : UserControl
{
    private ILogger<Conversation> _logger;
    private readonly ApplicationState _appState;
    
    public Conversation()
    {
        InitializeComponent();
        
        _logger = Ioc.Default.GetService<ILogger<Conversation>>()!;
        _appState = Ioc.Default.GetService<ApplicationState>()!;
    }

    private void ScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        var scrollViewer = (ScrollViewer)sender!;
        var scroll =
            scrollViewer.Offset.Y - scrollViewer.Extent.Height + scrollViewer.Viewport.Height;
        
        if(_appState.ChatSession?.GetMessages().LastOrDefault()?.Role == "assistant" || 
           _appState.ChatSession?.GetMessages().LastOrDefault()?.Role == "system")
            ScrollToEndButton.IsVisible = scroll < -25;
        else
        if (scroll < 0)
        {
            scrollViewer.ScrollToEnd();
        }
    }

    private void ScrollToEndButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _appState.ReadyForNewAiMessage = false;
        
        var scrollViewer = ScrollViewer;
        
        scrollViewer.ScrollToEnd();
        
        _appState.ReadyForNewAiMessage = true;
    }
}