namespace Aesir.Client.Messages;

/// <summary>
/// Represents the status of a file upload process, including its file path,
/// processing state, success status, and any associated errors.
/// </summary>
public class FileUploadStatusMessage
{
    /// Gets or sets the identifier for the ongoing conversation.
    /// This property is used to track and associate messages or actions
    /// with a specific conversation context, enabling proper message
    /// flow and handling within the application.
    public string? ConversationId { get; set; }

    /// Represents the file path associated with the file being uploaded or processed
    /// during a file upload operation.
    /// The value of this property is used to determine the location of the file in the file
    /// system or to reference the file during processing and communication between
    /// components.
    public string FilePath { get; set; } = "No File";

    /// Gets or sets a value indicating whether a file upload process is currently ongoing.
    /// This property is used to monitor or control the state of file upload operations.
    /// A value of `true` indicates that a file is being processed or uploaded,
    /// while a value of `false` indicates that the process has completed or has not started.
    public bool IsProcessing { get; set; }

    /// Indicates whether the file upload operation was successful.
    /// This property is set to true when the file upload completes successfully
    /// and false if the operation fails.
    public bool IsSuccess { get; set; }

    /// Gets or sets the error message associated with the file upload status.
    /// This property contains details about any errors encountered during
    /// the file upload process. If no error occurred, this property may be null or empty.
    public string? ErrorMessage { get; set; }
}