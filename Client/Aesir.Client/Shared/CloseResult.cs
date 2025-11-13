namespace Aesir.Client.Shared;

/// <summary>
/// Used to communicate a general result of closing a view from the view model to the view.
/// </summary>
public enum CloseResult
{
    Cancelled,
    Errored,
    Created,
    Updated,
    Deleted
}