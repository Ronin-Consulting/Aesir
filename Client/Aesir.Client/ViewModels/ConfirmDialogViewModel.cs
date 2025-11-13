using CommunityToolkit.Mvvm.ComponentModel;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents the ViewModel for a confirmation dialog in the application.
/// </summary>
/// <remarks>
/// This class is responsible for providing the necessary data binding and logic
/// for the ConfirmDialog user interface. It interacts with view components
/// such as messages and dialog options. Extends the ObservableRecipient
/// to support MVVM data-binding.
/// </remarks>
public partial class ConfirmDialogViewModel : ObservableRecipient
{
    /// <summary>
    /// Represents the text message displayed in a confirmation dialog.
    /// This variable holds the content of the message shown to the user.
    /// </summary>
    [ObservableProperty]
    private string? _messageText;
}