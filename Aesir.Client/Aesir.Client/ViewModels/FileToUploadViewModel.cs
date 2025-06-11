using System;
using Aesir.Client.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;

namespace Aesir.Client.ViewModels;

public partial class FileToUploadViewModel : ObservableRecipient, IRecipient<FileUploadRequestMessage>
{
    [ObservableProperty]
    private bool _isVisible;
    
    [ObservableProperty]
    private bool _isProcessingFile;
    
    [ObservableProperty]
    private string _fileName = "No File";
    
    [ObservableProperty]
    private string _filePath = "No Path";

    public void SetFileInfo(string filePath)
    {
        FileName = Path.GetFileName(filePath);
        FilePath = filePath;
    }

    public void ToggleProcessingFile()
    {
        IsProcessingFile = !IsProcessingFile;
    }
    
    public void ToggleVisible()
    {
        IsVisible = !IsVisible;
    }

    [RelayCommand]
    private async Task RemoveFileAsync()
    {
        await Task.CompletedTask;
        
        WeakReferenceMessenger.Default.Send(new FileUploadCanceledMessage()
        {
            FilePath = FilePath
        });

        IsProcessingFile = false;
        IsVisible = false;
        FilePath = "No Path";
        FileName = "No File";
    }

    public void Receive(FileUploadRequestMessage message)
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            SetFileInfo(message.FilePath);
            ToggleProcessingFile();
            ToggleVisible();

            WeakReferenceMessenger.Default.Send(new FileUploadStatusMessage()
            {
                FilePath = message.FilePath,
                IsProcessing = true
            });
            
            await Task.Delay(TimeSpan.FromSeconds(8));
            
            ToggleProcessingFile();
            
            WeakReferenceMessenger.Default.Send(new FileUploadStatusMessage()
            {
                FilePath = message.FilePath,
                IsProcessing = false,
                IsSuccess = true
            });
        });
        // here is where I would use the file upload service to send up the file
    }
}