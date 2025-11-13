using System;
using Aesir.Client.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aesir.Client.ViewModels;

public partial class LogDetailDialogViewModel :ObservableRecipient, IDisposable
{

    [ObservableProperty]
    public AesirKernelLog? _log;
    
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