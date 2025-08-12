using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Provides the ViewModel for the Tools View within the application.
/// </summary>
/// <remarks>
/// This class extends <see cref="ObservableRecipient"/>, providing support for observing and responding
/// to changes in property values and other notifications. Implements <see cref="IDisposable"/> to ensure
/// proper resource management when instances of this ViewModel are no longer needed.
/// </remarks>
public class ToolsViewViewModel : ObservableRecipient, IDisposable
{
    /// <summary>
    /// Releases all resources used by the ToolsViewViewModel instance.
    /// </summary>
    /// <param name="disposing">
    /// A boolean value indicating whether the method is being called from the
    /// Dispose method (true) or from the finalizer (false).
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsActive = false;
        }
    }

    /// Releases the resources used by the ToolsViewViewModel class.
    /// This method calls the protected Dispose method and optionally schedules
    /// the object for garbage collection by suppressing finalization.
    /// Use this method to release resources explicitly before the object is reclaimed by garbage collection.
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}