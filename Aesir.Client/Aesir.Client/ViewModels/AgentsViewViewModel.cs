using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Services;
using Aesir.Common.Models;
using Aesir.Common.Prompts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

public class AgentsViewViewModel : ObservableRecipient
{
    /// <summary>
    /// Represents a command that shows a agents view
    /// </summary>
    public ICommand ShowChat { get; protected set; }

    /// <summary>
    /// Represents a command that shows a tools view
    /// </summary>
    public ICommand ShowTools { get; protected set; }
    
    /// <summary>
    /// epresents a command that shows an add agent view
    /// </summary>
    public ICommand ShowAddAgent { get; protected set; }
    
    /// <summary>
    /// Represents the agents loaded from the system
    /// </summary>
    public ObservableCollection<AesirAgent> Agents { get; protected set; }

    /// <summary>
    /// Represents the currently selected agent
    /// </summary>
    public AesirAgent? SelectedAgent
    {
        get => _selectedAgent;
        set
        {
            if (SetProperty(ref _selectedAgent, value))
            {
                OnAgentSelected(value);
            }
        }
    }

    /// <summary>
    /// Represents the logger instance used for capturing and recording log messages
    /// within the context of the MainViewViewModel class. This includes logging
    /// errors, warnings, and informational messages during the execution of
    /// various operations such as handling chat functionality, toggling features,
    /// or reporting application states.
    /// </summary>
    private readonly ILogger<AgentsViewViewModel> _logger;

    /// <summary>
    /// Navigation service used for navigating the view to another part of the app
    /// </summary>
    private readonly INavigationService _navigationService;
    
    /// <summary>
    /// 
    /// </summary>
    private AesirAgent? _selectedAgent;
    
    /// Represents the view model for the main view in the application.
    /// Handles core application state and provides commands for toggling chat history, starting a new chat, and controlling the microphone.
    /// Integrates services for speech recognition, chat session management, and file upload interactions.
    public AgentsViewViewModel(
        ILogger<AgentsViewViewModel> logger,
        INavigationService navigationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationService = navigationService;

        ShowChat = new RelayCommand(ExecuteShowChat);
        ShowTools = new RelayCommand(ExecuteShowTools);
        ShowAddAgent = new RelayCommand(ExecuteShowAddAgent);
        
        // TODO load this on activate
        Agents = new ObservableCollection<AesirAgent>([
            new AesirAgent
            {
                Name = "Agent 1",
                ChatModel = "gpt-4.1-2025-04-14",
                EmbeddingModel = "text-embedding-3-large",
                VisionModel = "gpt-4.1-2025-04-14",
                Source = ModelSource.OpenAI,
                Tools = new List<string>() { "RAG" },
                Prompt = PromptContext.Military
            },
            new AesirAgent
            {
                Name = "Agent 2",
                ChatModel = "qwen3:32b-q4_K_M",
                EmbeddingModel = "mxbai-embed-large:latest",
                VisionModel = "gemma3:12b",
                Source = ModelSource.Ollama,
                Tools = new List<string>() { "RAG" },
                Prompt = PromptContext.Military
            },
            new AesirAgent
            {
                Name = "Computer Use",
                ChatModel = "cogito:32b-v1-preview-qwen-q4_K_M",
                EmbeddingModel = "mxbai-embed-large:latest",
                VisionModel = "gemma3:12b",
                Source =ModelSource.Ollama,
                Tools = new List<string>() { "RAG" },
                Prompt = PromptContext.Business
            }
        ]);
    }

    private void ExecuteShowChat()
    {
        _navigationService.NavigateToChat();
    }

    private void ExecuteShowTools()
    {
        _navigationService.NavigateToTools();
    }

    private void ExecuteShowAddAgent()
    {
        WeakReferenceMessenger.Default.Send(new ShowAgentDetailMessage(null));
    }

    private void OnAgentSelected(AesirAgent? selectedAgent)
    {
        if (selectedAgent != null)
        {
            WeakReferenceMessenger.Default.Send(new ShowAgentDetailMessage(selectedAgent));
        }
    }
}