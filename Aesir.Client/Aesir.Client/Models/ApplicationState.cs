using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Aesir.Client.Messages;
using Aesir.Client.Services;
using Aesir.Common.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace Aesir.Client.Models;

/// <summary>
/// Represents the shared and observable state of the application, managing critical
/// data such as collections of available models and chat sessions.
/// </summary>
/// <remarks>
/// This class extends <see cref="ObservableRecipient"/>, allowing it to handle
/// property change notifications and facilitate message communication between
/// components. It serves as a central point for managing and dynamically loading data
/// like <see cref="AvailableModels"/> and <see cref="ChatSessions"/> through asynchronous
/// operations. Implements <see cref="IDisposable"/> for resource management.
/// </remarks>
public partial class ApplicationState(
    IModelService modelService, 
    IChatHistoryService chatHistoryService,
    IConfigurationService configurationService
   )
    : ObservableRecipient, IDisposable, IRecipient<ChatSessionDeletedMessage>
{
    /// <summary>
    /// Determines if the application is prepared to process and handle a new AI-generated message.
    /// Used for managing and synchronizing the flow of AI message handling within the application state.
    /// </summary>
    [ObservableProperty] [NotifyPropertyChangedRecipients]
    private bool _readyForNewAiMessage = true;

    /// <summary>
    /// Stores information about the currently selected agent within the application.
    /// This variable is used to manage and reflect the user's selection, ensuring proper handling
    /// of associated data and actions for the chosen model throughout the application.
    /// </summary>
    [ObservableProperty] [NotifyPropertyChangedRecipients]
    private AesirAgentBase? _selectedAgent;

    /// <summary>
    /// Represents the unique identifier of the currently selected chat session.
    /// This field is used to keep track of the active chat session within the application state,
    /// facilitating operations such as messaging and session management.
    /// </summary>
    [ObservableProperty] [NotifyPropertyChangedRecipients]
    private Guid? _selectedChatSessionId;

    /// <summary>
    /// Represents the currently active chat session within the application.
    /// Tracks and manages the ongoing context and interactions tied to a specific session.
    /// </summary>
    [ObservableProperty] [NotifyPropertyChangedRecipients]
    private AesirChatSession? _chatSession;

    /// <summary>
    /// Represents the collection of chat sessions available in the application.
    /// This property is observable and allows dynamic updates to the chat session
    /// list as new sessions are retrieved or modified within the application's
    /// state.
    /// </summary>
    public ObservableCollection<AesirChatSessionItem> ChatSessions { get; set; } = [];

    /// <summary>
    /// Represents the collection of agents available for use within the application.
    /// </summary>
    public ObservableCollection<AesirAgentBase> AvailableAgents { get; set; } = [];

    /// <summary>
    /// Asynchronously loads the system configuration readiness status.
    /// </summary>
    /// <returns>
    /// An AesirConfigurationReadinessBase object containing the readiness status
    /// and any specific reasons why the system might not be ready.
    /// </returns>
    public async Task<AesirConfigurationReadinessBase> CheckSystemConfigurationReady()
    {
        return await configurationService.GetIsSystemConfigurationReadyAsync();
    }

    /// <summary>
    /// Asynchronously loads the list of available models and updates the shared application state.
    /// </summary>
    /// <returns>
    /// A collection of available agents represented as <see cref="AesirAgentBase"/>.
    /// </returns>
    public async Task LoadAvailableAgentsAsync()
    {
        AvailableAgents.Clear();

        var agents = await configurationService.GetAgentsAsync();
        foreach (var agent in agents)
            AvailableAgents.Add(agent);
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

    /// <summary>
    /// Releases the resources used by the application state.
    /// </summary>
    /// <param name="disposing">
    /// A boolean value indicating whether the method is being called manually (true)
    /// or by the garbage collector (false).
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsActive = false;
        }
    }

    /// <summary>
    /// Disposes the resources used by the <see cref="ApplicationState"/> instance.
    /// </summary>
    /// <remarks>
    /// This method performs application-defined tasks associated with freeing,
    /// releasing, or resetting unmanaged resources. It calls the protected <see cref="Dispose(bool)"/>
    /// method with the disposing parameter set to true and suppresses finalization of the object to
    /// optimize garbage collection.
    /// </remarks>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Receive(ChatSessionDeletedMessage message)
    {
        SelectedChatSessionId = null;
    }
}
