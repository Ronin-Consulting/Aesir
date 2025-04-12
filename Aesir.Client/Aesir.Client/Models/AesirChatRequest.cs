using System;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Aesir.Client.Models;

public class AesirChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "model-not-set";

    [JsonPropertyName("chat_session_id")]
    public Guid? ChatSessionId { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = "title-not-set";
    
    [JsonPropertyName("chat_session_updated_at")]
    public DateTimeOffset ChatSessionUpdatedAt { get; set; } = DateTimeOffset.Now;
    
    [JsonPropertyName("conversation")] 
    public AesirConversation Conversation { get; set; } = null!;

    [JsonPropertyName("temperature")] 
    public double? Temperature { get; set; } = 0.2;

    [JsonPropertyName("top_p")] 
    public double? TopP { get; set; }

    [JsonPropertyName("max_tokens")] 
    public int? MaxTokens { get; set; } = 4096;
    
    [JsonPropertyName("user")] 
    public string User { get; set; } = null!;
    
    [JsonPropertyName("client_datetime")]
    public string ClientDateTime { get; set; } = DateTime.Now.ToString("F", new CultureInfo("en-US"));
    
    public static AesirChatRequest NewWithDefaults()
    {
        return new AesirChatRequest()
        {
            Model = "not-set",
            Conversation = new AesirConversation()
            {
                Id = Guid.NewGuid().ToString()
            },
            Temperature = 0.1,
            MaxTokens = 4096,
            User = "Unknown"
        };
    }
}