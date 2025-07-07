namespace Aesir.Client.Messages;

/// <summary>
/// Represents a request message for uploading a file within a specific conversation context.
/// </summary>
/// <remarks>
/// This class is used for transferring the file path and associated conversation
/// identifier required for file upload operations. It can be utilized in message-passing
/// scenarios, such as communication between view models or services handling file uploads.
/// <para>
/// The <see cref="ConversationId"/> represents the unique identifier of the conversation
/// associated with the file upload, while the <see cref="FilePath"/> specifies the location
/// of the file to be uploaded.
/// </para>
/// </remarks>
public class FileUploadRequestMessage
{
    /// Represents the unique identifier for a conversation associated with a file upload request.
    /// The ConversationId property is used to associate specific file upload requests
    /// with a particular conversation or chat session. It helps to maintain context
    /// and ensure that uploaded files are properly linked to the intended conversation.
    /// Type:
    /// string? (nullable)
    /// Remarks:
    /// - This property should be assigned a valid conversation identifier when sending a file upload request.
    /// - If not properly set, the request may not be associated with the intended conversation.
    public string? ConversationId { get; set; }

    /// <summary>
    /// Represents the file path to be uploaded in the context of file upload operations.
    /// </summary>
    /// <remarks>
    /// This property specifies the location or name of the file that needs to be uploaded.
    /// It is essential for identifying and processing the file during upload tasks.
    /// </remarks>
    public string FilePath { get; set; } = null!;
}