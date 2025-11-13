using Aesir.Common.Models;

namespace Aesir.Modules.Chat.Models;

/// <summary>
/// Represents a single chunk in a streamed chat completion response.
/// </summary>
public class AesirChatStreamedResult : AesirChatStreamedResultBase
{
    /// <summary>
    /// Initializes a new instance of the AesirChatStreamedResult class.
    /// </summary>
    public AesirChatStreamedResult()
    {
        Title = "Chat Session (Server)";
    }
}