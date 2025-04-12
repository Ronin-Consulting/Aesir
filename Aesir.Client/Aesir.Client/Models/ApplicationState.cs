using System;
using System.Collections.ObjectModel;
using Aesir.Client.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aesir.Client.Models;

public partial class ApplicationState : ObservableRecipient
{
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private bool _readyForNewAiMessage = true;
    
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private AesirModelInfo? _selectedModel;
    
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private Guid? _selectedChatSessionId;
    
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private AesirChatSession? _chatSession;

    public ObservableCollection<AesirChatSessionItem> ChatSessions { get; set; } = [];
}