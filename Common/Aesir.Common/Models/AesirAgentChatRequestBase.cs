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

    /// <summary>
    /// Gets or sets the identifier of the project this chat request is associated with.
    /// When set, project-specific instructions and documents will be included in the chat context.
    /// </summary>
    [JsonPropertyName("project_id")]
    public Guid? ProjectId { get; set; }
}