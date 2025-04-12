using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Aesir.Client.Models;

public class AesirChatSession
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("user_id")]
    public string UserId { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
    
    [JsonPropertyName("conversation")]
    public AesirConversation Conversation { get; set; }

    [JsonPropertyName("title")]
    public string Title  { get; set; } = "Chat Session (Client)";
    
    public void AddMessage(AesirChatMessage message)
    {
        Conversation.Messages.Add(message);
    }
    
    public IList<AesirChatMessage> GetMessages()
    {
        return Conversation.Messages;
    }
    
    public AesirChatSession()
    {
        Id = Guid.NewGuid();
        Conversation = new AesirConversation()
        {
            Id = Guid.NewGuid().ToString(),
            Messages = new List<AesirChatMessage>()
            {
                AesirChatMessage.NewSystemMessage()
            }
        };
    }
}

public class AesirChatSessionItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("user_id")]
    public string UserId { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = "Chat Session (Client)";
    
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
}