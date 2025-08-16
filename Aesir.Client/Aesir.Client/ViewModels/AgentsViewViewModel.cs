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

/// <summary>
/// Represents the view model for managing agents in the application.
/// </summary>
/// <remarks>
/// This class extends <see cref="ObservableRecipient"/> to manage state and perform
/// operations related to agent interactions. It provides commands to display chat,
/// tools, and to add new agents. Additionally, it manages the collection of agents
/// and tracks the selected agent.
/// </remarks>
public class AgentsViewViewModel : ObservableRecipient, IDisposable
{
    /// <summary>
    /// Represents a command that triggers the display of the chat interface.
    /// </summary>
    public ICommand ShowChat { get; protected set; }

    /// <summary>
    /// Represents a command that triggers the display of an interface for adding a new agent.
    /// </summary>
    public ICommand ShowAddAgent { get; protected set; }

    /// <summary>
    /// Represents a collection of agents displayed in the agents view.
    /// </summary>
    public ObservableCollection<AesirAgentBase> Agents { get; protected set; }

    /// <summary>
    /// Represents the currently selected agent from the collection of agents.
    /// This property is bound to the selection within the user interface and updates whenever
    /// a new agent is chosen. Triggers logic related to agent selection changes.
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
    /// within the context of the AgentsViewViewModel class. This includes logging
    /// errors, warnings, and informational messages related to the execution of
    /// various operations and application states in the view model.
    /// </summary>
    private readonly ILogger<AgentsViewViewModel> _logger;

    /// <summary>
    /// Provides navigation functionality to transition between various views or features
    /// within the application.
    /// </summary>
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Provides access to configuration-related operations and data
    /// management for agents and tools within the system.
    /// </summary>
    private readonly IConfigurationService _configurationService;

    /// <summary>
    /// Backing field for the currently selected agent in the view model.
    /// </summary>
    private AesirAgentBase? _selectedAgent;

    /// Represents the view model for managing agents within the application.
    /// Provides commands to display the chat and agent creation interfaces.
    /// Maintains a collection of agents and tracks the currently selected agent.
    /// Integrates navigation and configuration services to coordinate application workflows.
    public AgentsViewViewModel(
        ILogger<AgentsViewViewModel> logger,
        INavigationService navigationService,
        IConfigurationService configurationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationService = navigationService;
        _configurationService = configurationService;

        ShowChat = new RelayCommand(ExecuteShowChat);
        ShowAddAgent = new RelayCommand(ExecuteShowAddAgent);

        Agents = new ObservableCollection<AesirAgentBase>();
    }

    /// Called when the view model is activated.
    /// Invokes an asynchronous operation to load agent data into the view model's collection.
    /// This method is designed to execute on the UI thread and ensures the proper initialization
    /// of agent-related data when the view model becomes active.
    protected override void OnActivated()
    {
        base.OnActivated();

        Dispatcher.UIThread.InvokeAsync(LoadAgentsAsync);
    }

    /// <summary>
    /// Does the initial load of the agents.
    /// </summary>
    private async Task LoadAgentsAsync()
    {
        await RefreshAgentsAsync();
    }

    /// Asynchronously loads agents into the view model's Agents collection.
    /// Fetches the agents from the configuration service and populates the collection.
    /// Handles any exceptions that may occur during the loading process.
    public async Task RefreshAgentsAsync()
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

    /// Executes navigation to the Chat view within the application.
    /// Invokes the navigation service to display the Chat interface, facilitating interaction with chat-specific UI components.
    private void ExecuteShowChat()
    {
        _navigationService.NavigateToChat();
    }

    /// Executes the command to show the interface for adding a new agent.
    /// Sends a message indicating that the interface for agent details should be displayed.
    /// This method is bound to the `ShowAddAgent` command in the view model and is triggered
    /// when the corresponding user action is performed in the UI.
    private void ExecuteShowAddAgent()
    {
        WeakReferenceMessenger.Default.Send(new ShowAgentDetailMessage(null));
    }

    /// Handles logic when an agent is selected in the AgentsViewViewModel.
    /// Sends a message to display detailed information about the selected agent.
    /// <param name="selectedAgent">The agent that has been selected. If null, no action is taken.</param>
    private void OnAgentSelected(AesirAgentBase? selectedAgent)
    {
        if (selectedAgent != null)
        {
            WeakReferenceMessenger.Default.Send(new ShowAgentDetailMessage(selectedAgent));
        }
    }

    /// Releases the resources used by the view model.
    /// Cleans up unmanaged resources and other disposable objects when the object is no longer needed.
    /// <param name="disposing">Indicates whether to release managed resources along with unmanaged resources.
    /// If set to true, both managed and unmanaged resources are disposed; if false, only unmanaged resources are released.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsActive = false;
        }
    }

    /// Disposes of the resources used by the AgentsViewViewModel.
    /// Ensures proper release of managed resources and suppresses finalization
    /// to optimize garbage collection.
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}