using CommunityToolkit.Mvvm.ComponentModel;

namespace Aesir.Client.ViewModels;

public partial class ConfirmDialogViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string? _messageText;
}