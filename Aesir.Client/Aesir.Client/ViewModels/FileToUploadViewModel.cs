using System;
using Aesir.Client.Messages;
using Aesir.Client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;

namespace Aesir.Client.ViewModels;

public partial class FileToUploadViewModel : ObservableRecipient, IRecipient<FileUploadRequestMessage>
{
    private readonly IDocumentCollectionService _documentCollectionService;
    private readonly IDialogService _dialogService;
    private const string DefaultFileName = "No File";
    private const string DefaultFilePath = "No Path";

    public FileToUploadViewModel(IDocumentCollectionService documentCollectionService, IDialogService dialogService)
    {
        _documentCollectionService = documentCollectionService;
        _dialogService = dialogService;
    }
    
    [ObservableProperty]
    private bool _isVisible;
    
    [ObservableProperty]
    private bool _isProcessingFile;
    
    [ObservableProperty]
    private string _fileName = DefaultFileName;
    
    [ObservableProperty]
    private string _filePath = DefaultFilePath;

    private string? _conversationId;

    public void SetConversationId(string conversationId)
    {
        _conversationId = conversationId;
    }

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
        await Dispatcher.UIThread.InvokeAsync(() =>
            {
                WeakReferenceMessenger.Default.Send(new FileUploadCanceledMessage()
                {
                    ConversationId = _conversationId,
                    FilePath = FilePath
                });

                IsProcessingFile = false;
                IsVisible = false;
                FilePath = DefaultFilePath;
                FileName = DefaultFileName;
            }
        );
    }

    public void Receive(FileUploadRequestMessage message)
    {
        if (_conversationId != message.ConversationId) return;
        
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            SetFileInfo(message.FilePath);
            ToggleProcessingFile();
            ToggleVisible();
            
            WeakReferenceMessenger.Default.Send(new FileUploadStatusMessage()
            {
                ConversationId = _conversationId,
                FilePath = message.FilePath,
                IsProcessing = true
            });

            try
            {
                await _documentCollectionService.UploadConversationFileAsync(message.FilePath, _conversationId!);
                
                ToggleProcessingFile();
                
                WeakReferenceMessenger.Default.Send(new FileUploadStatusMessage()
                {
                    ConversationId = _conversationId,
                    FilePath = message.FilePath,
                    IsProcessing = false,
                    IsSuccess = true
                });
            }
            catch (Exception ex)
            {
                ToggleProcessingFile();
                
                WeakReferenceMessenger.Default.Send(new FileUploadStatusMessage()
                {
                    ConversationId = _conversationId,
                    FilePath = message.FilePath,
                    IsProcessing = false,
                    IsSuccess = false
                });

                await _dialogService.ShowErrorDialogAsync("Upload Error", $"An error occurred while uploading the file: {ex.Message}");
            }
        });
    }
}