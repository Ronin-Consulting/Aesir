using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Aesir.Client.Shared;
using Aesir.Common.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Irihi.Avalonia.Shared.Contracts;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents the view model for document-related views, providing properties, commands,
/// and events to manage Document configurations and interactions in the user interface.
/// </summary>
/// <remarks>
/// This class extends <see cref="ObservableRecipient"/> to manage state and perform
/// operations related to Document interactions. It provides commands to display chat,
/// Documents, and to add new Documents. Additionally, it manages the collection of Documents
/// and tracks the selected Document.
/// </remarks>
public partial class DocumentViewViewModel : ObservableRecipient, IDialogContext
{
    /// <summary>
    /// Represents the underlying document configuration and details used by the view model, including properties.
    /// </summary>
    private AesirDocument _document;

    /// <summary>
    /// Notification service responsible for displaying various types of user notifications.
    /// </summary>
    private INotificationService _notificationService;

    /// <summary>
    /// Service for accessing and managing configuration data, including Documents
    /// and Documents, within the application.
    /// </summary>
    private readonly IDocumentCollectionService _documentCollectionService;
    
    private readonly IChatHistoryService _chatHistoryService;

    /// <summary>
    /// Represents the form data model for the Document view, used to handle data
    /// binding and validation within the DocumentViewViewModel.
    /// </summary>
    [ObservableProperty] private DocumentFormDataModel _formModel;
    
    /// <summary>
    /// Command used to cancel the current operation or revert changes in the Document View.
    /// </summary>
    public ICommand CancelCommand { get; set; }

    /// <summary>
    /// Command used to delete the associated Document or entity.
    /// </summary>
    public ICommand DeleteCommand { get; set; }

    /// <summary>
    /// Represents a command that triggers the display of the chat interface.
    /// </summary>
    public ICommand DownloadCommand { get; protected set; }

    /// <summary>
    /// Event triggered to request the closure of the view or dialog.
    /// </summary>
    public event EventHandler<object?>? RequestClose;
    
    
    /// Represents the view model for the document view. Handles the binding of document data
    /// and communication between the user interface and underlying services.
    public DocumentViewViewModel(AesirDocument document, 
        INotificationService notificationService,
        IDocumentCollectionService documentCollectionService,
        IChatHistoryService chatHistoryService)
    {
        _document = document;
        _documentCollectionService =documentCollectionService;
        _notificationService = notificationService;
        _chatHistoryService = chatHistoryService;

        var chats = GetChatsForDocument(_document);
        
        FormModel = new()
        {
            Name = Path.GetFileName(_document.FileName),
            Path = _document.FileName,
            Chats = chats,
            HasChats = chats.Any(),
            Created = _document.CreatedAtDisplay,
            Modified = _document.UpdatedAtDisplay,
            MimeType = _document.MimeType,
            Size = _document.FileSizeDisplay,
        };
        CancelCommand = new RelayCommand(ExecuteCancelCommand);
        DeleteCommand = new AsyncRelayCommand(ExecuteDeleteCommand);
        DownloadCommand = new RelayCommand(ExecuteDownloadCommand);
    }

    private IEnumerable<AesirChatSessionItem>? GetChatsForDocument(AesirDocument document)
    {
        return _chatHistoryService.GetChatSessionsByFileAsync(document.FileNameOnly).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes the logic to delete the document.
    /// </summary>
    private async Task ExecuteDeleteCommand()
    {
        var dialogService = Ioc.Default.GetService<IDialogService>();
        if (dialogService == null) return;

        var result = await dialogService.ShowConfirmationDialogAsync(
            "Delete Document and Associated Chats",
            $"Are you sure you want to delete this document and ALL associated chats?");
        if (result)
        {
            var closeResult = CloseResult.Errored;

            try
            {
                if (FormModel.Chats != null)
                {
                    foreach (var chat in FormModel.Chats)
                    {
                        await _chatHistoryService.DeleteChatSessionAsync(chat.Id);
                        await _documentCollectionService.DeleteUploadedConversationFileAsync(_document.FileName, chat.Id.ToString());
                    }
                }

                closeResult = CloseResult.Deleted;
            }
            catch (Exception e)
            {
                _notificationService.ShowErrorNotification("Error",
                    $"'{FormModel.Name}' failed to delete document : {e.Message}");

                Console.WriteLine(e);
            }
            finally
            {
                Close(closeResult);
            }
        }
    }

    private void ExecuteDownloadCommand()
    {
        WeakReferenceMessenger.Default.Send(new FileDownloadMessage()
        {
            FileName = FormModel.Path
        });
    }

    /// <summary>
    /// Executes the cancel operation for the current view model.
    /// </summary>
    /// <remarks>
    /// This method is typically invoked when the user opts to discard any pending changes
    /// to the form or data. It triggers the closure of the associated view or dialog without
    /// saving any modifications.
    /// </remarks>
    private void ExecuteCancelCommand()
    {
        Close(CloseResult.Cancelled);
    }

    /// <summary>
    /// Triggers the request to close the associated view or dialog by invoking the <see cref="RequestClose"/> event.
    /// </summary>
    /// <remarks>
    /// This method is typically invoked internally to signal that the current operation
    /// or interaction should conclude, and the related view or dialog should be closed.
    /// </remarks>
    public void Close()
    {
        Close(CloseResult.Cancelled);
    }

    /// <summary>
    /// Triggers the request to close the associated view or dialog by invoking the <see cref="RequestClose"/> event.
    /// </summary>
    /// <param name="closeResult">The results to send to the close event</param>
    private void Close(CloseResult closeResult)
    {
        RequestClose?.Invoke(this, closeResult);
    }
    
}

/// <summary>
/// Represents a model for the Document form data used to bind properties and validate inputs in the Document view.
/// </summary>
public partial class DocumentFormDataModel : ObservableValidator
{
    /// <summary>
    /// The name of the document
    /// </summary>
    [ObservableProperty] private string? _name;

    /// <summary>
    /// The name of the document
    /// </summary>
    [ObservableProperty] private string? _path;

    /// <summary>
    /// The chats that the document is associated with.
    /// </summary>
    [ObservableProperty] IEnumerable<AesirChatSessionItem>? _chats;
 
    /// <summary>
    /// Whether the document is in chats.
    /// </summary>
    [ObservableProperty] private bool? _hasChats;   
 
    /// <summary>
    /// Whether the document is in chats.
    /// </summary>
    [ObservableProperty] private string? _size;   
 
    /// <summary>
    /// Whether the document is in chats.
    /// </summary>
    [ObservableProperty] private string? _mimeType;   
 
    /// <summary>
    /// Whether the document is in chats.
    /// </summary>
    [ObservableProperty] private string? _created;   
 
    /// <summary>
    /// Whether the document is in chats.
    /// </summary>
    [ObservableProperty] private string? _modified;   
}