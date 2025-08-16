using System.ComponentModel;
using System.Text.Json.Serialization;
using Aesir.Common.Prompts;

namespace Aesir.Common.Models;

public class AesirMcpServerBase
{
    /// <summary>
    /// Gets or sets the id of the agent
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the agent
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the agent
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the command
    /// </summary>
    [JsonPropertyName("command")]
    public string? Command { get; set; }

    /// <summary>
    /// Gets or sets the arguments
    /// </summary>
    [JsonPropertyName("arguments")]
    public IList<string?> Arguments { get; set; }

    /// <summary>
    /// Gets or sets the environment variables
    /// </summary>
    [JsonPropertyName("environment_variables")]
    public IDictionary<string, string?> EnvironmentVariables { get; set; }
}