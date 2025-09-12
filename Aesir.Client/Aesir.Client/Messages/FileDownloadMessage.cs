
namespace Aesir.Client.Messages;

/// <summary>
/// Represents a request message for downloading a file within a specific conversation context.
/// </summary>
/// <remarks>
/// This class is used for transferring the file name required for file download operations. It can be utilized in message-passing
/// scenarios, such as communication between view models or services handling file downloads.
/// <para>
/// associated with the file download, while the <see cref="FileName"/> specifies the file name
/// of the file to be downloaded.
/// </para>
/// </remarks>
public class FileDownloadMessage
{
    /// Represents the file name associated with the file to be downloaded.
    public required string? FileName { get; set; }
}