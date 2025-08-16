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
/// Represents the view model for managing MCP Servers in the application.
/// </summary>
/// <remarks>
/// This class extends <see cref="ObservableRecipient"/> to manage state and perform
/// operations related to MCP Server interactions. It provides commands to display chat,
/// MCP Servers, and to add new MCP Servers. Additionally, it manages the collection of MCP Servers
/// and tracks the selected MCP Server.
/// </remarks>
public class McpServersViewViewModel : ObservableRecipient, IDisposable
{
    /// <summary>
    /// Represents a command that triggers the display of the chat interface.
    /// </summary>
    public ICommand ShowChat { get; protected set; }

    /// <summary>
    /// Represents a command that triggers the display of an interface for adding a new MCP Server.
    /// </summary>
    public ICommand ShowAddMcpServer { get; protected set; }

    /// <summary>
    /// Represents a collection of MCP Servers displayed in the MCP Servers view.
    /// </summary>
    public ObservableCollection<AesirMcpServerBase> McpServers { get; protected set; }

    /// <summary>
    /// Represents the currently selected MCP Server from the collection of MCP Servers.
    /// This property is bound to the selection within the user interface and updates whenever
    /// a new MCP Server is chosen. Triggers logic related to MCP Server selection changes.
    /// </summary>
    public AesirMcpServerBase? SelectedMcpServer
    {
        get => _selectedMcpServer;
        set
        {
            if (SetProperty(ref _selectedMcpServer, value))
            {
                OnMcpServerSelected(value);
            }
        }
    }

    /// <summary>
    /// Represents the logger instance used for capturing and recording log messages
    /// within the context of the McpServersViewViewModel class. This includes logging
    /// errors, warnings, and informational messages related to the execution of
    /// various operations and application states in the view model.
    /// </summary>
    private readonly ILogger<McpServersViewViewModel> _logger;

    /// <summary>
    /// Provides navigation functionality to transition between various views or features
    /// within the application.
    /// </summary>
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Provides access to configuration-related operations and data
    /// management for agents and MCP Servers within the system.
    /// </summary>
    private readonly IConfigurationService _configurationService;

    /// <summary>
    /// Backing field for the currently selected MCP Server in the view model.
    /// </summary>
    private AesirMcpServerBase? _selectedMcpServer;

    /// Represents the view model for managing MCP Servers within the application.
    /// Provides commands to display the chat and MCP Server creation interfaces.
    /// Maintains a collection of MCP Servers and tracks the currently selected MCP Server.
    /// Integrates navigation and configuration services to coordinate application workflows.
    public McpServersViewViewModel(
        ILogger<McpServersViewViewModel> logger,
        INavigationService navigationService,
        IConfigurationService configurationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationService = navigationService;
        _configurationService = configurationService;

        ShowChat = new RelayCommand(ExecuteShowChat);
        ShowAddMcpServer = new RelayCommand(ExecuteShowAddMcpServer);

        McpServers = new ObservableCollection<AesirMcpServerBase>();
    }

    /// Called when the view model is activated.
    /// Invokes an asynchronous operation to load MCP Server data into the view model's collection.
    /// This method is designed to execute on the UI thread and ensures the proper initialization
    /// of MCP Server-related data when the view model becomes active.
    protected override void OnActivated()
    {
        base.OnActivated();

        Dispatcher.UIThread.InvokeAsync(LoadMcpServersAsync);
    }

    /// <summary>
    /// Does the initial load of the MCP Servers.
    /// </summary>
    private async Task LoadMcpServersAsync()
    {
        await RefreshMcpServersAsync();
    }

    /// Asynchronously loads MCP Servers into the view model's MCP Servers collection.
    /// Fetches the MCP Servers from the configuration service and populates the collection.
    /// Handles any exceptions that may occur during the loading process.
    public async Task RefreshMcpServersAsync()
    {
        try
        {
            var mcpServers = await _configurationService.GetMcpServersAsync();
            McpServers.Clear();
            foreach (var mcpServer in mcpServers)
                McpServers.Add(mcpServer);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading MCP Servers: {ex.Message}");
        }   
    }

    /// Executes navigation to the Chat view within the application.
    /// Invokes the navigation service to display the Chat interface, facilitating interaction with chat-specific UI components.
    private void ExecuteShowChat()
    {
        _navigationService.NavigateToChat();
    }

    /// Executes the command to show the interface for adding a new MCP Server.
    /// Sends a message indicating that the interface for MCP Server details should be displayed.
    /// This method is bound to the `ShowAddMcpServer` command in the view model and is triggered
    /// when the corresponding user action is performed in the UI.
    private void ExecuteShowAddMcpServer()
    {
        WeakReferenceMessenger.Default.Send(new ShowMcpServerDetailMessage(null));
    }

    /// Handles logic when a MCP Server is selected in the McpServersViewViewModel.
    /// Sends a message to display detailed information about the selected MCP Server.
    /// <param name="selectedMcpServer">The MCP Server that has been selected. If null, no action is taken.</param>
    private void OnMcpServerSelected(AesirMcpServerBase? selectedMcpServer)
    {
        if (selectedMcpServer != null)
        {
            WeakReferenceMessenger.Default.Send(new ShowMcpServerDetailMessage(selectedMcpServer));
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

    /// Disposes of the resources used by the McpServersViewViewModel.
    /// Ensures proper release of managed resources and suppresses finalization
    /// to optimize garbage collection.
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}