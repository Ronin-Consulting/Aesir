using Aesir.Client.Models;
using Aesir.Client.ViewModels;
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
        ShowPanel();
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (ActionPanel == null) return;
        HidePanel();
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (ActionPanel == null) return;
        HidePanel();
    }

    private void HidePanel()
    {
        ActionPanel.Opacity = 0.0;
    }

    private void ShowPanel()
    {
        ActionPanel.Opacity = 1.0;
    }

    public void StartEditAction_Clicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not UserMessageViewModel viewModel) return;
        viewModel.RawMessage = viewModel.ConvertFromHtml(viewModel.Message);
        viewModel.IsEditing = true;
        MessageEditor.Focus();
    }

    public void EndEditAction_Clicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not UserMessageViewModel viewModel) return;
        viewModel.SetMessage(AesirChatMessage.NewUserMessage(viewModel.ConvertToHtml(viewModel.RawMessage)));
        viewModel.IsEditing = false;
    }
}
