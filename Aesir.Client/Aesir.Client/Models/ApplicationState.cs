using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Aesir.Client.Services;
using Aesir.Common.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aesir.Client.Models;

/// <summary>
/// Maintains the shared and observable state of the application, including
/// collections and properties related to available models and chat sessions.
/// </summary>
/// <remarks>
/// This class, derived from <see cref="ObservableRecipient"/>, provides
/// functionality for property change notifications and message handling. It
/// manages collections like <see cref="AvailableModels"/> and <see cref="ChatSessions"/>,
/// which are dynamically loaded through asynchronous methods.
/// </remarks>
public partial class ApplicationState(IModelService modelService, IChatHistoryService chatHistoryService)
    : ObservableRecipient, IDisposable
{
    /// <summary>
    /// Represents a flag indicating the readiness of the application to handle a new AI-generated message.
    /// This property helps in controlling and synchronizing message processing flows within the application.
    /// </summary>
    [ObservableProperty] [NotifyPropertyChangedRecipients]
    private bool _readyForNewAiMessage = true;

    /// <summary>
    /// Represents the model currently selected by the user in the application.
    /// This property holds detailed information about the selected model,
    /// including its metadata retrieved from the Aesir client service.
    /// Updates to this property trigger notifications to relevant recipients
    /// to react accordingly in the UI or application logic.
    /// </summary>
    [ObservableProperty] [NotifyPropertyChangedRecipients]
    private AesirModelInfo? _selectedModel;

    /// <summary>
    /// Stores the unique identifier of the currently active chat session.
    /// This variable helps track which chat session is selected or being interacted with
    /// in the application state.
    /// </summary>
    [ObservableProperty] [NotifyPropertyChangedRecipients]
    private Guid? _selectedChatSessionId;

    /// <summary>
    /// Represents the currently active chat session in the application.
    /// Maintains the state of the chat session, including its context and interactions.
    /// </summary>
    [ObservableProperty] [NotifyPropertyChangedRecipients]
    private AesirChatSession? _chatSession;

    /// <summary>
    /// Represents a collection of chat sessions in the application.
    /// This property is an observable collection used to track and manage
    /// the list of chat sessions, enabling updates and data binding throughout the application.
    /// </summary>
    public ObservableCollection<AesirChatSessionItem> ChatSessions { get; set; } = [];

    /// <summary>
    /// Represents the collection of models available for use within the application.
    /// This collection is populated asynchronously and can include models that
    /// support various features, such as chat functionalities.
    /// </summary>
    public ObservableCollection<AesirModelInfo> AvailableModels { get; set; } = [];

    /// <summary>
    /// Asynchronously loads the list of available models and updates the shared application state.
    /// </summary>
    /// <returns>
    /// A collection of available models represented as <see cref="AesirModelInfo"/>.
    /// </returns>
    public async Task<IEnumerable<AesirModelInfo>> LoadAvailableModelsAsync()
    {
        AvailableModels.Clear();
        
        var models = await modelService.GetModelsAsync();
        foreach (var model in models)
        {
            AvailableModels.Add(model);
        }
        
        return AvailableModels;
    }

    /// <summary>
    /// Asynchronously loads the available chat sessions for the application
    /// by retrieving them via the chat history service and populates the
    /// local collection.
    /// </summary>
    /// <returns>
    /// A collection of <see cref="AesirChatSessionItem"/> objects representing
    /// the loaded chat sessions.
    /// </returns>
    public async Task<IEnumerable<AesirChatSessionItem>> LoadAvailableChatSessionsAsync()
    {
        ChatSessions.Clear();

        var chatSessions = await chatHistoryService.GetChatSessionsAsync();
        foreach (var chatSession in chatSessions)
        {
            ChatSessions.Add(chatSession);
        }
        
        return ChatSessions;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsActive = false;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}