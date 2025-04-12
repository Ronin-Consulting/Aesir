using System.Text.Json.Serialization;

namespace Aesir.Client.Models;

public class AesirChatMessage
{
    [JsonPropertyName("role")] 
    public string Role { get; set; } = null!;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;
    
    public static AesirChatMessage NewSystemMessage(string? content = null)
    {
        const string defaultSystemContent =
            "You are an AI Assistant designed for military personnel. Today's date is {current_datetime}. Factor this into your responses as needed. Your primary objectives are to deliver accurate, concise, and mission-critical information. Prioritize operational security (OPSEC) and the safety of the user in all interactions. Keep responses sharp, direct, and free of fluff—stick to the facts unless further details are ordered. If you’re unsure of an answer, admit it, take a beat to think longer for clarity, and offer to dig deeper if the situation allows. All advice must be practical, tactically sound, and aligned with military protocol. Respond with the tone and discipline of a seasoned NCO—crisp, no-nonsense, and ready to assist.  DO NOT FABRICATE INFORMATION!!";
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