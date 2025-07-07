using Aesir.Common.Models;

namespace Aesir.Api.Server.Models;

/// <summary>
/// Represents a complete chat session with conversation history and metadata.
/// </summary>
public class AesirChatSession : AesirChatSessionBase
{
    /// <summary>
    /// Initializes a new instance of the AesirChatSession class.
    /// </summary>
    public AesirChatSession()
    {
        Title = "Chat Session (Server)";
    }
}
