using System.Text.Json.Serialization;

namespace Aesir.Tools.LegalValidator.Models;

/// <summary>
/// Basic information about an AESIR agent.
/// </summary>
public class AgentInfo
{
    /// <summary>
    /// Unique identifier for the agent.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Display name of the agent.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the agent.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Model used by the agent for chat.
    /// </summary>
    [JsonPropertyName("chat_model")]
    public string ChatModel { get; set; } = string.Empty;

    /// <summary>
    /// Temperature setting for the agent.
    /// </summary>
    [JsonPropertyName("chat_temperature")]
    public double ChatTemperature { get; set; }
}
