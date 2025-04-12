using System;
using System.Text.Json.Serialization;

namespace Aesir.Client.Models;

public class AesirChatResult
{
    [JsonPropertyName("chat_session_id")]
    public Guid? ChatSessionId { get; set; }
    
    [JsonPropertyName("conversation")]
    public AesirConversation AesirConversation { get; set; } = null!;
    
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
    
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
}