using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

public class AesirConfigurationReadinessBase
{
    [JsonPropertyName("is_ready")]   
    public bool IsReady { get; set; }
    
    /// <summary>
    /// Gets or sets the collection of specific reasons why the system configuration
    /// is not ready. This collection is empty when IsReady is true.
    /// </summary>
    /// <remarks>
    /// Each reason provides a human-readable explanation of a missing or invalid
    /// configuration requirement that prevents the system from being fully operational.
    /// </remarks>
    [JsonPropertyName("reasons")]
    public IEnumerable<string> Reasons { get; set; } = [];
}
