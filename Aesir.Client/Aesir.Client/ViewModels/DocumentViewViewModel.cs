using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Aesir.Client.Shared;
using Aesir.Client.Validators;
using Aesir.Common.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Irihi.Avalonia.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents the view model for document-related views, providing properties, commands,
/// and events to manage tool configurations and interactions in the user interface.
/// </summary>
/// <remarks>
/// This class extends <see cref="ObservableRecipient"/> to manage state and perform
/// operations related to tool interactions. It provides commands to display chat,
/// tools, and to add new tools. Additionally, it manages the collection of tools
/// and tracks the selected tool.
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
    /// Service for accessing and managing configuration data, including tools
    /// and tools, within the application.
    /// </summary>
    private readonly IDocumentCollectionService _documentCollectionService;

    /// <summary>
    /// Represents the form data model for the tool view, used to handle data
    /// binding and validation within the ToolViewViewModel.
    /// </summary>
    [ObservableProperty] private DocumentFormDataModel _formModel;
    
    /// <summary>
    /// Command used to cancel the current operation or revert changes in the Tool View.
    /// </summary>
    public ICommand CancelCommand { get; set; }

    /// <summary>
    /// Command used to delete the associated tool or entity.
    /// </summary>
    public ICommand DeleteCommand { get; set; }

    /// <summary>
    /// Event triggered to request the closure of the view or dialog.
    /// </summary>
    public event EventHandler<object?>? RequestClose;
    
    
    /// Represents the view model for the document view. Handles the binding of document data
    /// and communication between the user interface and underlying services.
    public DocumentViewViewModel(AesirDocument document, 
        INotificationService notificationService,
        IDocumentCollectionService documentCollectionService)
    {
        _document = document;
        _documentCollectionService =documentCollectionService;
        _notificationService = notificationService;

        FormModel = new()
        {
            Name = _document.FileName,
            Chats = GetChatsForDocument(_document),
            
        };
        CancelCommand = new RelayCommand(ExecuteCancelCommand);
        DeleteCommand = new AsyncRelayCommand(ExecuteDeleteCommand);
    }

    private IEnumerable<AesirChatSessionBase>? GetChatsForDocument(AesirDocument document)
    {
        return new List<AesirChatSessionBase>([
            new  AesirChatSession()
            {
                Id = Guid.NewGuid(),
                Conversation = new AesirConversation()
                {
                    Id = Guid.NewGuid().ToString(), 
                    Messages = new List<AesirChatMessage>([ new AesirChatMessage(){Content = "Content",Role = "Role",ThoughtsContent = "Thoughts"}])
                }
            }
            // "Chat 1", "Chat 2", "Chat 3", "Chat 4"
        ]);
    }

    /// <summary>
    /// Invoked when the view model is activated. This method executes initialization logic such as
    /// invoking UI-thread-specific tasks and loading the necessary resources or state for operation.
    /// </summary>
    protected override void OnActivated()
    {
        base.OnActivated();

        // Dispatcher.UIThread.InvokeAsync(LoadAvailableAsync);
    }

    /// <summary>
    /// Executes the logic to delete the document.
    /// </summary>
    private async Task ExecuteDeleteCommand()
    {
        var closeResult = CloseResult.Errored;
        
        try
        {
            if (FormModel.Chats != null)
            {
                foreach (var chat in FormModel.Chats)
                {
                    // await _documentCollectionService.DeleteUploadedGlobalFileAsync(_document.FileName,"0");
                    await _documentCollectionService.DeleteUploadedConversationFileAsync(_document.FileName,
                        chat.Conversation.Id);
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
    
    protected override void OnDeactivated()
    {
        base.OnDeactivated();
    
        // FormModel.PropertyChanged -= OnFormModelPropertyChanged;
    }

}

/// <summary>
/// Represents a model for the tool form data used to bind properties and validate inputs in the tool view.
/// </summary>
public partial class DocumentFormDataModel : ObservableValidator
{
    /// <summary>
    /// The name of the document
    /// </summary>
    [ObservableProperty] private string? _name;
    
    /// <summary>
    /// The chats that the document is associated with.
    /// </summary>
    [ObservableProperty] IEnumerable<AesirChatSessionBase>? _chats;
 
    /// <summary>
    /// Whether the document is in chats.
    /// </summary>
    [ObservableProperty] private bool? _hasChats;   
}