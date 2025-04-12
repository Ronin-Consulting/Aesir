using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Aesir.Client.Models;

public class AesirConversation
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;
    
    [JsonPropertyName("messages")]
    public IList<AesirChatMessage> Messages { get; set; } = new List<AesirChatMessage>();
}