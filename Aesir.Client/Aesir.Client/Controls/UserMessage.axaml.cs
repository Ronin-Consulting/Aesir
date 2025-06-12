using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Aesir.Client.Controls;

public partial class UserMessage : UserControl
{
    public UserMessage()
    {
        InitializeComponent();
        
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        PointerCaptureLost += OnPointerCaptureLost;
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (ActionPanel == null) return;
        ActionPanel.Opacity = 1.0;
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (ActionPanel == null) return;
        ActionPanel.Opacity = 0.0;
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (ActionPanel == null) return;
        ActionPanel.Opacity = 0.0;
    }

    public void EditAction_Clicked(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("EDIT ACTION CLICKED!");
    }
}