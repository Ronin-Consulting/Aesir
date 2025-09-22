using Aesir.Client.Models;
using Aesir.Common.Models;

namespace Aesir.Client.Messages;

/// <summary>
/// Represents a message used to show the details of a document in the system.
/// </summary>
public class ShowDocumentDetailMessage(AesirDocument? document)
{
    /// <summary>
    /// Represents a document entity, encapsulated within the <see cref="ShowDocumentDetailMessage"/> class.
    /// This property holds an instance of <see cref="AesirDocument"/> which provides core document information such as
    /// ID, file name, MIME type, and file size. Should be null if new or if import was requested.
    /// </summary>
    public AesirDocument Document  { get; set; } = document ?? new AesirDocument();
}
