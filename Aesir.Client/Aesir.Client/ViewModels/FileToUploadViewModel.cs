using System;
using Aesir.Client.Messages;
using Aesir.Client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;

namespace Aesir.Client.ViewModels;

/// <summary>
/// ViewModel responsible for managing file upload operations and coordinating
/// user interactions related to file uploading within a conversation context.
/// </summary>
/// <remarks>
/// This class inherits from ObservableRecipient and implements IRecipient interface
/// to handle received <see cref="FileUploadRequestMessage"/> instances. It maintains
/// the state of the current file to be uploaded, including its name, path, visibility,
/// and processing status, and facilitates interaction with file upload services.
/// Dependencies such as <see cref="IDocumentCollectionService"/> and <see cref="IDialogService"/>
/// are injected through the constructor, enabling service interactions and user dialogs.
/// </remarks>
public partial class FileToUploadViewModel(
    IDocumentCollectionService documentCollectionService,
    IDialogService dialogService)
    : ObservableRecipient, IRecipient<FileUploadRequestMessage>
{
    /// <summary>
    /// Represents the default file name used when no file has been selected or specified.
    /// </summary>
    private const string DefaultFileName = "No File";

    /// <summary>
    /// Indicates whether the file upload view is currently visible or not.
    /// </summary>
    [ObservableProperty]
    private bool _isVisible;

    /// <summary>
    /// Indicates whether the file is currently being processed.
    /// </summary>
    [ObservableProperty]
    private bool _isProcessingFile;

    /// <summary>
    /// Represents the name of the file to be uploaded.
    /// Defaults to "No File" when no file is selected or specified.
    /// </summary>
    [ObservableProperty]
    private string _fileName = DefaultFileName;

    [ObservableProperty] 
    private MaterialIconKind _iconKind = MaterialIconKind.FileDocument;
    
    /// <summary>
    /// Stores the unique identifier for the current conversation associated with the file upload process.
    /// This value helps in tracking file operations or determining the context of messages exchanged between components.
    /// </summary>
    private string? _conversationId;

    /// Sets the conversation ID associated with the current file upload process.
    /// <param name="conversationId">
    /// The unique identifier of the conversation to associate with the file upload.
    /// </param>
    public void SetConversationId(string conversationId)
    {
        _conversationId = conversationId;
    }

    /// Sets the file information based on the specified file.
    /// <param name="file">The file to be set.</param>
    public void SetFileInfo(IStorageFile file)
    {
        FileName = file.Name;
        
        if (Path.GetExtension(FileName).Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            IconKind = MaterialIconKind.FileImage;
        }
    }

    /// <summary>
    /// Toggles the processing state of a file.
    /// Changes the value of the <c>IsProcessingFile</c> property to its opposite.
    /// </summary>
    public void ToggleProcessingFile()
    {
        IsProcessingFile = !IsProcessingFile;
    }

    /// <summary>
    /// Toggles the visibility of the current file.
    /// Changes the <see cref="IsVisible"/> state between true and false.
    /// </summary>
    public void ToggleVisible()
    {
        IsVisible = !IsVisible;
    }

    /// Removes the file associated with the current conversation asynchronously.
    /// Sends a message to notify that the file upload has been canceled and updates the view model state accordingly.
    /// <return>
    /// A task that represents the asynchronous operation of removing the file.
    /// </return>
    [RelayCommand]
    private async Task RemoveFileAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                WeakReferenceMessenger.Default.Send(new FileUploadCanceledMessage()
                {
                    ConversationId = _conversationId,
                    FileName = FileName
                });

                await documentCollectionService.DeleteUploadedConversationFileAsync(FileName, _conversationId!);
                
                IsProcessingFile = false;
                IsVisible = false;
                FileName = DefaultFileName;
            }
        );
    }

    /// Resets the file-related state in the view model to default values.
    /// This includes clearing the file name, file path, visibility, and processing statuses.
    public void ClearFile()
    {
        IsProcessingFile = false;
        IsVisible = false;
        FileName = DefaultFileName;
    }

    /// Handles a file upload request message and processes the file upload operation.
    /// <param name="message">
    /// The file upload request message containing the conversation ID and file path
    /// for the file to be uploaded.
    /// </param>
    public void Receive(FileUploadRequestMessage message)
    {
        if (_conversationId != message.ConversationId) return;
        
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            SetFileInfo(message.File);
            ToggleProcessingFile();
            ToggleVisible();
            
            WeakReferenceMessenger.Default.Send(new FileUploadStatusMessage()
            {
                ConversationId = _conversationId,
                FileName = message.File.Name,
                IsProcessing = true
            });

            try
            {
                await documentCollectionService.UploadConversationFileAsync(message.File, _conversationId!);
                
                ToggleProcessingFile();
                
                WeakReferenceMessenger.Default.Send(new FileUploadStatusMessage()
                {
                    ConversationId = _conversationId,
                    FileName = message.File.Name,
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
                    FileName = message.File.Name,
                    IsProcessing = false,
                    IsSuccess = false
                });

                await dialogService.ShowErrorDialogAsync("Upload Error", $"An error occurred while uploading the file: {ex.Message}");
            }
        });
    }
}