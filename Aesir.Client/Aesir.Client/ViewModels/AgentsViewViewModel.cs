using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Services;
using Aesir.Common.Models;
using Avalonia.Threading;
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
    public ObservableCollection<AesirAgentBase> Agents { get; protected set; }

    /// <summary>
    /// Represents the currently selected agent
    /// </summary>
    public AesirAgentBase? SelectedAgent
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
    /// within the context of the ChatViewViewModel class. This includes logging
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
    /// Configuration service used for managing configuration
    /// </summary>
    private readonly IConfigurationService _configurationService;
    
    /// <summary>
    /// The currently selected agent
    /// </summary>
    private AesirAgentBase? _selectedAgent;
    
    /// Represents the view model for the main view in the application.
    /// Handles core application state and provides commands for toggling chat history, starting a new chat, and controlling the microphone.
    /// Integrates services for speech recognition, chat session management, and file upload interactions.
    public AgentsViewViewModel(
        ILogger<AgentsViewViewModel> logger,
        INavigationService navigationService,
        IConfigurationService configurationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationService = navigationService;
        _configurationService = configurationService;

        ShowChat = new RelayCommand(ExecuteShowChat);
        ShowTools = new RelayCommand(ExecuteShowTools);
        ShowAddAgent = new RelayCommand(ExecuteShowAddAgent);

        Agents = new ObservableCollection<AesirAgentBase>();
    }
    
    protected override void OnActivated()
    {
        base.OnActivated();

        Dispatcher.UIThread.InvokeAsync(LoadAgentsAsync);
    }
    
    private async Task LoadAgentsAsync()
    {
        try
        {
            var agents = await _configurationService.GetAgentsAsync();
            Agents.Clear();
            foreach (var agent in agents)
                Agents.Add(agent);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading agents: {ex.Message}");
        }
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

    private void OnAgentSelected(AesirAgentBase? selectedAgent)
    {
        if (selectedAgent != null)
        {
            WeakReferenceMessenger.Default.Send(new ShowAgentDetailMessage(selectedAgent));
        }
    }
}