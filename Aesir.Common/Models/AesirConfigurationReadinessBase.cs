using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

public class AesirConfigurationReadinessBase
{
    [JsonPropertyName("is_ready")]   
    public bool IsReady { get; set; }
    
    [JsonPropertyName("reasons")]
    public IEnumerable<string> Reasons { get; set; }
}