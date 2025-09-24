using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

/// <summary>
/// Base class for agent chat completion requests containing common properties.
/// </summary>
public class AesirAgentChatRequestBase : ChatRequestBase
{
    /// <summary>
    /// Gets or sets the unique identifer of the agent to use for the chat completion.
    /// </summary>
    [JsonPropertyName("agent_id")]
    public Guid? AgentId { get; set; }
}