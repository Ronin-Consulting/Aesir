namespace Aesir.Client.Messages;

/// <summary>
/// Represents a message sent to request the download of a file.
/// </summary>
public class FileDownloadRequestMessage
{
    /// <summary>
    /// Gets or sets the name of the file associated with the download request.
    /// </summary>
    /// <remarks>
    /// This property holds the file name required for processing a file download request message.
    /// The value is provided when initiating a file download operation.
    /// </remarks>
    public required string? FileName { get; set; }
}