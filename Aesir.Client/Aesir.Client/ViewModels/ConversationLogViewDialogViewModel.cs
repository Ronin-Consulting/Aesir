using System;
using System.Collections.ObjectModel;
using Aesir.Common.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aesir.Client.ViewModels;

public class ConversationLogViewDialogViewModel :ObservableRecipient, IDisposable
{
    public ObservableCollection<AesirKernelLogBase> Logs { get; set; }

    private AesirKernelLogBase? _selectedLog;
    public AesirKernelLogBase? SelectedLog
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
    private void OnLogSelected(AesirKernelLogBase? selectedLog)
    {
        if (selectedLog != null)
        {
            // WeakReferenceMessenger.Default.Send(new ShowDocumentDetailMessage(selectedDocument));
        }
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