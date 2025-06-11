using CommunityToolkit.Mvvm.ComponentModel;

namespace Aesir.Client.ViewModels;

public partial class FileToUploadViewModel : ObservableRecipient
{
    [ObservableProperty]
    private bool _isVisible = true;
    
    [ObservableProperty]
    private string _fileName;
}