using System.Text.Json.Serialization;

namespace Aesir.Api.Server.Models;

public class AesirChatSession
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("user_id")]
    public string UserId { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

    [JsonPropertyName("conversation")] 
    public AesirConversation Conversation { get; set; } = null!;
    
    [JsonIgnore]
    public string Title { get; set; } = "Chat Session (Server)";
}

public class AesirChatSessionItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = null!;

    [JsonPropertyName("title")]
    public string Title { get; set; } = "Chat Session (Server)";
    
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
}