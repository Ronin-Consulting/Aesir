using CommunityToolkit.Mvvm.ComponentModel;

namespace Aesir.Client.ViewModels;

public partial class InputDialogViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string? _labelText;
    
    private string? _inputText;
    public string? InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }
    
    [ObservableProperty]
    private bool _isInputTextRequired;
}