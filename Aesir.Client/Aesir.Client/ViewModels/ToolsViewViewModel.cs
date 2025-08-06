using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aesir.Client.ViewModels;

public class ToolsViewViewModel : ObservableRecipient, IDisposable
{
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsActive = false;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}