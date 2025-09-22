using System;
using System.Threading.Tasks;
using Aesir.Client.Messages;
using Aesir.Client.Services;
using Aesir.Common.FileTypes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Material.Icons;

namespace Aesir.Client.ViewModels;

/// <summary>
/// ViewModel responsible for handling file upload operations and managing
/// state and interactions related to file uploads within a conversation context.
/// </summary>
/// <remarks>
/// This class extends ObservableRecipient and implements IRecipient interface to
/// facilitate the handling of <see cref="FileUploadRequestMessage"/> instances. It provides
/// methods for configuring the upload process, toggling visibility, managing file
/// information, and updating the processing state.
/// The constructor requires implementations of <see cref="IDocumentCollectionService"/>
/// and <see cref="IDialogService"/> to manage document-related operations and display
/// user dialogs necessary for the file upload workflow.
/// This ViewModel ensures the smooth coordination of file upload features with
/// the application's user interface and backend services.
/// </remarks>
public partial class FileToUploadViewModel(
    IDocumentCollectionService documentCollectionService,
    IDialogService dialogService)
    : ObservableRecipient, IRecipient<FileUploadRequestMessage>, IDisposable
{
    /// <summary>
    /// Defines the constant used as the default file name when no file is chosen or specified.
    /// </summary>
    private const string DefaultFileName = "No File";

    /// <summary>
    /// Determines whether the file upload view is visible to the user.
    /// </summary>
    [ObservableProperty] private bool _isVisible;

    /// <summary>
    /// Indicates whether a file is currently being processed within the view model.
    /// </summary>
    [ObservableProperty] private bool _isProcessingFile;

    /// <summary>
    /// Stores the name of the file to be uploaded by the user.
    /// Defaults to "No File" if no file is selected or provided.
    /// </summary>
    [ObservableProperty] private string _fileName = DefaultFileName;

    /// <summary>
    /// Represents the type of icon displayed in the UI, typically reflecting the current state
    /// or type of the file being handled within the file upload process.
    /// </summary>
    [ObservableProperty] 
    private MaterialIconKind _iconKind = MaterialIconKind.FileDocument;

    /// <summary>
    /// Represents the unique identifier for the current conversation associated with the file upload process.
    /// Used to track the context of operations and the flow of communication between components.
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
    /// <param name="file">
    /// The file to be set, represented as an instance of IStorageFile.
    /// </param>
    public void SetFileInfo(IStorageFile file)
    {
        FileName = file.Name;

        if (!FileTypeManager.IsImage(FileName)) return;
        IconKind = MaterialIconKind.FileImage;
    }

    /// Toggles the processing state of the file currently being handled.
    /// Swaps the value of the <c>IsProcessingFile</c> property between <c>true</c> and <c>false</c>.
    public void ToggleProcessingFile()
    {
        IsProcessingFile = !IsProcessingFile;
    }

    /// Toggles the visibility of the current file.
    /// Updates the state of the file's visibility by inverting the current value of the <see cref="IsVisible"/> property.
    public void ToggleVisible()
    {
        IsVisible = !IsVisible;
    }

    /// Removes the file associated with the current conversation asynchronously.
    /// Sends a cancellation message, updates the file state, and adjusts the visibility of the view model.
    /// <return>
    /// A task representing the asynchronous operation of removing the file.
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

    /// Clears the currently selected file information within the view model.
    /// Resets the file name to the default value, updates visibility,
    /// and processing status to reflect a cleared state.
    public void ClearFile()
    {
        IsProcessingFile = false;
        IsVisible = false;
        FileName = DefaultFileName;
    }

    /// Handles the reception of a file upload request message and initiates the associated file upload process.
    /// <param name="message">
    /// The file upload request message that contains details such as the conversation ID and the file
    /// to be uploaded for processing.
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

                await RemoveFileAsync();
            }
        });
    }

    /// Releases unmanaged resources and any other resources used by the instance.
    /// <param name="disposing">
    /// A boolean value indicating whether the method is being called explicitly to release
    /// both managed and unmanaged resources (true) or if it is being called by the finalizer
    /// to release only unmanaged resources (false).
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsActive = false;
        }
    }

    /// Releases the resources used by the FileToUploadViewModel instance.
    /// This method performs cleanup operations for managed and unmanaged resources.
    /// It ensures proper disposal of resources and prevents finalization by calling
    /// <see cref="GC.SuppressFinalize"/>. Use this method when the instance is no longer needed
    /// to release resources promptly.
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}