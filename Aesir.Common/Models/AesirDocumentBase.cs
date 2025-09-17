using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

public class AesirDocumentBase
{
    /// <summary>
    /// Gets or sets the id of the tool
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the tool
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
}
