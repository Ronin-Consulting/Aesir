using Aesir.Client.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace Aesir.Client.Views;

public partial class MainView : UserControl
{
    private readonly ApplicationState _appState;
    
    public MainView()
    {
        InitializeComponent();
        
        _appState = Ioc.Default.GetService<ApplicationState>()!;
        
        TitleTextBlock.Margin = new Thickness(35, 0, 0, 0);
        
        if (MessageAiTextBox != null)
        {
            MessageAiTextBox.AttachedToVisualTree += (s,e) => MessageAiTextBox.Focus();
            
            _appState.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "ReadyForNewAiMessage" && _appState.ReadyForNewAiMessage)
                {
                    MessageAiTextBox.Focus();
                }
            };
        }
    }

    private void SplitView_OnPaneOpening(object? sender, CancelRoutedEventArgs e)
    {
        TitleTextBlock.Margin = new Thickness(35, 0, 0, 0);
    }

    private void SplitView_OnPaneClosing(object? sender, CancelRoutedEventArgs e)
    {
        TitleTextBlock.Margin = new Thickness(85, 0, 0, 0);
    }

    private void InputElement_OnKey(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            if (sender is TextBox textBox)
            {
                if (textBox.Text.Length > 0)
                {
                    SendMessageButton.Command.Execute(null);
                    
                    MessageAiTextBox.Focus();
                }
            }
        }
    }

    private void MessageAiTextBox_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if(e.Property.Name == "IsEnabled" && e.NewValue is bool enabled && enabled)
        {
            MessageAiTextBox.Focus();
        }
    }
}