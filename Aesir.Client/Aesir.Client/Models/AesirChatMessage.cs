using System.Text.Json.Serialization;
using Aesir.Common.Prompts;

namespace Aesir.Client.Models;

public class AesirChatMessage
{
    [JsonPropertyName("role")] 
    public string Role { get; set; } = null!;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;
    
    public static AesirChatMessage NewSystemMessage(string? content = null, PromptContext context = PromptContext.Military)
    {
        var promptProvider = new DefaultPromptProvider();
        var defaultSystemContent = promptProvider.GetSystemPrompt(context).Content;
        
        return new AesirChatMessage()
        {
            Role = "system",
            Content = content ?? defaultSystemContent
        };
    }
    
    public static AesirChatMessage NewAssistantMessage(string content)
    {
        return new AesirChatMessage()
        {
            Role = "assistant",
            Content = content
        };
    }
    
    public static AesirChatMessage NewUserMessage(string content)
    {
        return new AesirChatMessage()
        {
            Role = "user",
            Content = content
        };
    }
}