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

    /// Gets or sets the file name related to the file upload process.
    public string FileName { get; set; } = null!;
}