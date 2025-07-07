using CommunityToolkit.Mvvm.ComponentModel;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents the view model for an input dialog, encapsulating the properties
/// and logic required to manage and retrieve user input data.
/// </summary>
public partial class InputDialogViewModel : ObservableRecipient
{
    /// <summary>
    /// Represents the text label displayed in the input dialog.
    /// Used to provide additional context or instructions to the user.
    /// </summary>
    [ObservableProperty]
    private string? _labelText;

    /// <summary>
    /// Represents the text input provided by the user in the input dialog.
    /// </summary>
    private string? _inputText;

    /// Gets or sets the user-provided input text within the dialog.
    /// This property represents the text entered by the user in an input dialog.
    /// It is bound to the input field in the dialog view and is updated whenever
    /// the user modifies the text. The value can be null if no input is provided.
    public string? InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    /// Indicates whether the input text field is required to be filled by the user.
    /// This boolean variable determines if the validation for mandatory input is active.
    /// Used in contexts where user-provided input is necessary for further operations.
    [ObservableProperty]
    private bool _isInputTextRequired;
}