using Aesir.Client.ViewModels;
using Avalonia.Controls;
using TheArtOfDev.HtmlRenderer.Avalonia;
using TheArtOfDev.HtmlRenderer.Core.Entities;

namespace Aesir.Client.Controls;

public partial class AssistantMessage : UserControl
{
    public AssistantMessage()
    {
        InitializeComponent();
        HtmlLabel.LinkClicked += HtmlLabel_OnLinkClicked;
    }

    public void HtmlLabel_OnLinkClicked(object? sender, HtmlRendererRoutedEventArgs<HtmlLinkClickedEventArgs> e)
    {
        if (DataContext is AssistantMessageViewModel viewModel)
        {
            viewModel.LinkClicked(e.Event.Link, e.Event.Attributes);
        }
        
        e.Handled = true;
    }
}