using System.Text.Json.Serialization;

namespace Aesir.Api.Server.Models;

public class AesirChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = null!;

    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;

    public static AesirChatMessage NewSystemMessage(string content)
    {
        return new AesirChatMessage()
        {
            Role = "system",
            Content = content
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