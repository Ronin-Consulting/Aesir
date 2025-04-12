using System.Text.Json.Serialization;

namespace Aesir.Api.Server.Models;

public class AesirChatStreamedResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;
    
    [JsonPropertyName("chat_session_id")]
    public Guid? ChatSessionId { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = "Chat Session (Server)";
    
    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; } = null!;
    
    [JsonPropertyName("delta")]
    public AesirChatMessage Delta { get; set; } = null!;
}