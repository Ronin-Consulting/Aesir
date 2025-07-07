namespace Aesir.Client.Messages;

/// Represents a message that is sent when a file upload process has been canceled.
/// This message is typically used to notify other components that a file upload
/// has been terminated, providing necessary details such as the file path and
/// conversation identifier for the canceled upload.
public class FileUploadCanceledMessage
{
    /// Gets or sets the unique identifier for the conversation associated with the file upload cancellation.
    /// This property helps in identifying which conversation's file upload process was terminated.
    public string? ConversationId { get; set; }

    /// Gets or sets the file path related to the file upload process.
    /// This property typically specifies the location path to the file, which can be
    /// referenced during operations such as file upload or removal. It is utilized
    /// in scenarios requiring file management, such as sending cancellation messages
    /// or deleting previously uploaded files.
    public string FilePath { get; set; } = null!;
}