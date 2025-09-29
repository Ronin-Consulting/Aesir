using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Controls;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Aesir.Common.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Ursa.Controls;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents the view model for managing logs in the application.
/// </summary>
/// <remarks>
/// This class extends <see cref="ObservableRecipient"/> to manage state and perform
/// operations related to tool interactions. It provides commands to display chat,
/// tools, and to add new tools. Additionally, it manages the collection of tools
/// and tracks the selected tool.
/// </remarks>
public class LogsViewViewModel : ObservableRecipient, IDisposable
{
    /// <summary>
    /// Represents a command that triggers the display of the chat interface.
    /// </summary>
    public ICommand ShowChat { get; protected set; }

    /// <summary>
    /// Represents a command that triggers the display of an interface for log details.
    /// </summary>
    public ICommand ShowLogDetails { get; protected set; }

    /// <summary>
    /// Represents a command that detects a click on the grid as a reselect.
    /// </summary>
    public ICommand ReselectFromGrid { get; protected set; }

    /// <summary>
    /// Represents a collection of tools displayed in the tools view.
    /// </summary>
    public ObservableCollection<AesirKernelLog> Logs { get; set; }

    /// <summary>
    /// Represents the currently selected tool from the collection of tools.
    /// This property is bound to the selection within the user interface and updates whenever
    /// a new tool is chosen. Triggers logic related to tool selection changes.
    /// </summary>
    public AesirKernelLog? SelectedLog
    {
        get => _selectedLog;
        set
        {
            if (SetProperty(ref _selectedLog, value))
            {
                OnLogSelected(value);
            }
        }
    }

    /// <summary>
    /// Represents the logger instance used for capturing and recording log messages
    /// within the context of the ToolsViewViewModel class. This includes logging
    /// errors, warnings, and informational messages related to the execution of
    /// various operations and application states in the view model.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Provides navigation functionality to transition between various views or features
    /// within the application.
    /// </summary>
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Provides access to configuration-related operations and data
    /// management for logs within the system.
    /// </summary>
    private readonly IKernelLogService _kernelLogService;

    /// <summary>
    /// Backing field for the currently selected log in the view model.
    /// </summary>
    private AesirKernelLog? _selectedLog;

    /// Represents the view model for managing logs within the application.
    /// Provides commands to display the chat and tool creation interfaces.
    /// Integrates navigation and configuration services to coordinate application workflows.
    public LogsViewViewModel(
        ILogger<LogsViewViewModel> logger,
        INavigationService navigationService,
        IKernelLogService kernelLogService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationService = navigationService;
        _kernelLogService = kernelLogService;

        ShowChat = new RelayCommand(ExecuteShowChat);
        ShowLogDetails = new RelayCommand(ExecuteShowLogDetails);
        ReselectFromGrid = new RelayCommand(ExecuteReselectFromGrid);
        
        Logs = new ObservableCollection<AesirKernelLog>();
    }

    /// Called when the view model is activated.
    /// Invokes an asynchronous operation to load tool data into the view model's collection.
    /// This method is designed to execute on the UI thread and ensures the proper initialization
    /// of tool-related data when the view model becomes active.
    protected override void OnActivated()
    {
        base.OnActivated();

        Dispatcher.UIThread.InvokeAsync(LoadLogsAsync);
    }

    /// <summary>
    /// Does the initial load of the tools.
    /// </summary>
    private async Task LoadLogsAsync()
    {
        await RefreshLogsAsync();
    }

    /// Asynchronously loads agents into the view model's Agents collection.
    /// Fetches the agents from the configuration service and populates the collection.
    /// Handles any exceptions that may occur during the loading process.
    public async Task RefreshLogsAsync()
    {
        try
        {
            var logs = await _kernelLogService.GetKernelLogsAsync(DateTimeOffset.Now.AddDays(-7),DateTimeOffset.Now);
            Logs.Clear();
            foreach (var log in logs)
                Logs.Add(log);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading logs: {ex.Message}");
        }
    }

    /// Executes navigation to the Chat view within the application.
    /// Invokes the navigation service to display the Chat interface, facilitating interaction with chat-specific UI components.
    private void ExecuteShowChat()
    {
        _navigationService.NavigateToChat();
    }

    /// Executes the command to show the interface for Document details.
    /// Sends a message indicating that the interface for Document details should be displayed.
    private void ExecuteShowLogDetails()
    {
        WeakReferenceMessenger.Default.Send(new ShowLogDetailMessage(_selectedLog));   
    }

    /// Executes the command to show the interface for re-selecting a document when the grid is clicked.
    private void ExecuteReselectFromGrid()
    {
        WeakReferenceMessenger.Default.Send(new ShowLogDetailMessage(_selectedLog));   
    }

    /// Handles logic when a Log is selected in the ToolsViewViewModel.
    /// Sends a message to display detailed information about the selected tool.
    /// <param name="selectedLog">The Log that has been selected. If null, no action is taken.</param>
    private void OnLogSelected(AesirKernelLog? selectedLog)
    {
        if (selectedLog != null)
        {
            WeakReferenceMessenger.Default.Send(new ShowLogDetailMessage(selectedLog));
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

    /// Disposes of the resources used by the ToolsViewViewModel.
    /// Ensures proper release of managed resources and suppresses finalization
    /// to optimize garbage collection.
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    
    
}