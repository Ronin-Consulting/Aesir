using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Aesir.Client.Models;

public class AesirChatSession
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("user_id")]
    public string UserId { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
    
    [JsonPropertyName("conversation")]
    public AesirConversation Conversation { get; set; } = new()
    {
        Id = Guid.NewGuid().ToString(),
        Messages = new List<AesirChatMessage>()
        {
            AesirChatMessage.NewSystemMessage()
        }
    };

    [JsonPropertyName("title")]
    public string Title  { get; set; } = "Chat Session (Client)";
    
    public void AddMessage(AesirChatMessage message)
    {
        if (Conversation.Messages.Contains(message)) return;
        
        Conversation.Messages.Add(message);
    }
    
    public void RemoveMessage(AesirChatMessage message)
    {
        Conversation.Messages.Remove(message);
    }
    
    public IList<AesirChatMessage> GetMessages()
    {
        return Conversation.Messages;
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