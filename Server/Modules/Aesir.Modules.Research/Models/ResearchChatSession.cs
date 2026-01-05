using System.Text.Json.Serialization;
using Aesir.Common.Models;

namespace Aesir.Modules.Research.Models;

/// <summary>
/// A minimal chat session implementation for persisting research results.
/// Used when research completes and needs to store the report in a ChatSession.
/// </summary>
public class ResearchChatSession : AesirChatSessionBase
{
    /// <summary>
    /// When the session was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
