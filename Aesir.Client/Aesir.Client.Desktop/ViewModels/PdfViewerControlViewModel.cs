using System.ComponentModel;
using System.Runtime.CompilerServices;
using MsBox.Avalonia.Base;

namespace Aesir.Client.Desktop.ViewModels;

public class PdfViewerControlViewModel : ISetFullApi<string>, INotifyPropertyChanged, IInput
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public string InputValue { get; set; }
    private IFullApi<string>? _fullApi;
    
    public void SetFullApi(IFullApi<string>? fullApi)
    {
        _fullApi = fullApi;
    }
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}