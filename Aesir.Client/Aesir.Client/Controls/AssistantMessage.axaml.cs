using System;
using Aesir.Client.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using TheArtOfDev.HtmlRenderer.Avalonia;
using TheArtOfDev.HtmlRenderer.Core.Entities;

namespace Aesir.Client.Controls;

public partial class AssistantMessage : UserControl
{
    public AssistantMessage()
    {
        InitializeComponent();
        HtmlLabel.LinkClicked += HtmlLabel_OnLinkClicked;

        MessagePanel.PointerEntered += OnPointerEntered;
        MessagePanel.PointerCaptureLost += OnPointerCaptureLost;
        MessagePanel.PointerExited += OnPointerExited;
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        ShowPanel();
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        HidePanel();
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        HidePanel();
    }

    private void HidePanel()
    {
        HoverPanel.Opacity = 0.0;
    }

    private void ShowPanel()
    {
        HoverPanel.Opacity = 1.0;
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