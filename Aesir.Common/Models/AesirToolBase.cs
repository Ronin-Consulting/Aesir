using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

public class AesirToolBase
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
}