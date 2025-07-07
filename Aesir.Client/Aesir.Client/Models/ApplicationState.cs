using System;
using System.Collections.ObjectModel;
using Aesir.Client.Services;
using Aesir.Common.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aesir.Client.Models;

/// <summary>
/// Represents the application state that maintains shared, observable properties
/// and collections used throughout the application.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="ObservableRecipient"/>, which enables it
/// to act as a recipient for messages and provides property change notifications.
/// </remarks>
public partial class ApplicationState : ObservableRecipient
{
    /// <summary>
    /// Indicates whether the application is ready to process a new AI-generated message.
    /// When set to true, the system can accept and handle a new AI message; otherwise, it is not ready.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private bool _readyForNewAiMessage = true;

    /// <summary>
    /// Represents the currently selected model in the application state.
    /// This model is an instance of <see cref="Aesir.Client.Services.AesirModelInfo"/>
    /// and contains metadata about an Aesir model, including its ID,
    /// ownership, creation date, and capabilities (such as chat or embedding functionalities).
    /// Changes to this property notify bound components or services within the application
    /// to update their state or behavior accordingly.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private AesirModelInfo? _selectedModel;

    /// <summary>
    /// Represents the unique identifier of the currently selected chat session.
    /// This property is used to track and manage the active chat session within the application's state.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private Guid? _selectedChatSessionId;

    /// <summary>
    /// Represents the currently active chat session in the application.
    /// This property is observable and notifies recipients when its value changes.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private AesirChatSession? _chatSession;

    /// <summary>
    /// Represents a collection of active or previously accessed chat sessions within the application.
    /// </summary>
    /// <remarks>
    /// This property is utilized to store and manage chat session data for the user, enabling features
    /// like displaying chat history and maintaining session state. It is an observable collection
    /// to reactively update the UI or dependent components upon changes, such as adding or removing
    /// sessions.
    /// </remarks>
    public ObservableCollection<AesirChatSessionItem> ChatSessions { get; set; } = [];
}