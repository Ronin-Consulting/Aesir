using Aesir.Client.ViewModels;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace Aesir.Client.Controls;

public partial class ChatHistory : UserControl
{
    public ChatHistory()
    {
        InitializeComponent();

        this.WithViewModel(Ioc.Default.GetService<ChatHistoryViewModel>());
    }
}