using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Aesir.Client.ViewModels;

public class LogsViewDialogViewModel :ObservableRecipient, IDisposable
{
    public ObservableCollection<AesirKernelLog> Logs { get; set; }

    private AesirKernelLog? _selectedLog;
    
    /// <summary>
    /// Represents a command that triggers the display of an interface for log details.
    /// </summary>
    public ICommand ShowLogDetails { get; protected set; }

    /// <summary>
    /// Represents a command that detects a click on the grid as a reselect.
    /// </summary>
    public ICommand ReselectFromGrid { get; protected set; }

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

    public LogsViewDialogViewModel()
    {
        ShowLogDetails = new RelayCommand(ExecuteShowLogDetails);
        ReselectFromGrid = new RelayCommand(ExecuteReselectFromGrid);
    }
    
    /// Executes the command to show the interface for Document details.
    /// Sends a message indicating that the interface for Document details should be displayed.
    private void ExecuteShowLogDetails()
    {
        WeakReferenceMessenger.Default.Send(new ShowLogDetailMessage(_selectedLog));   
    }

    /// Handles logic when a Log is selected in the ToolsViewViewModel.
    /// Sends a message to display detailed information about the selected tool.
    /// <param name="selectedLog">The Log that has been selected. If null, no action is taken.</param>
    private void OnLogSelected(AesirKernelLog? selectedLog)
    {
        // NO-OP ... dialog open is handled in the view code-behind with CellPointerPressed event
    }

    /// Executes the command to show the interface for re-selecting a document when the grid is clicked.
    private void ExecuteReselectFromGrid()
    {
        WeakReferenceMessenger.Default.Send(new ShowLogDetailMessage(_selectedLog));   
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsActive = false;
        }
    }

    /// Disposes of the resources used by the DocumentsViewViewModel.
    /// Ensures proper release of managed resources and suppresses finalization
    /// to optimize garbage collection.
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}