using Aesir.Common.Models;

namespace Aesir.Client.Models;

/// <summary>
/// Represents a streamed result of a chat session in the Aesir system.
/// Inherits from <see cref="AesirChatStreamedResultBase"/> and is specifically used
/// in the client context to handle chat stream responses.
/// </summary>
public class AesirChatStreamedResult : AesirChatStreamedResultBase
{
    /// <summary>
    /// Represents a streamed result for a chat session specifically in the client context.
    /// </summary>
    public AesirChatStreamedResult()
    {
        Title = "Chat Session (Client)";
    }
}