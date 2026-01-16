using System.Text.Json.Serialization;
using Aesir.Common.Models;

namespace Aesir.Modules.Chat.Models;

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

    /// <summary>
    /// Gets or sets whether the chat session has research in progress.
    /// This is computed from the research_session table.
    /// </summary>
    [JsonPropertyName("has_research_in_progress")]
    public bool HasResearchInProgress { get; set; }
}
