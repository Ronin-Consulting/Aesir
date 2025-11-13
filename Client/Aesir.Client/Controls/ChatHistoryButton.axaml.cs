using Aesir.Client.ViewModels;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Aesir.Client.Controls;

public partial class ChatHistoryButton : UserControl
{
    public ChatHistoryButton()
    {
        InitializeComponent();
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed) return;
        
        // Get the ToggleButton and show its context menu
        if (sender is not ToggleButton toggleButton ||
            DataContext is not ChatHistoryButtonViewModel viewModel) return;
            
        // Show the context menu
        var contextMenu = toggleButton.ContextMenu;
        if (contextMenu == null) return;
                
        contextMenu.PlacementTarget = toggleButton;
        contextMenu.Open();
        e.Handled = true;
    }
}