using System.Text.Json.Serialization;

namespace Aesir.Api.Server.Models;

public class AesirChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = null!;

    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;

    public bool HasFile()
    {
        if (Role != "user") return false;
        
        return Content.StartsWith("<file", StringComparison.OrdinalIgnoreCase) && 
               Content.Contains("</file>", StringComparison.OrdinalIgnoreCase);
    }

    public void AddFile(string filename)
    {
        //<file>SalesData.pdf</file>
        var contentFixed = Content;
        if (Content.StartsWith("<file", StringComparison.OrdinalIgnoreCase))
        {
            contentFixed = contentFixed.Split("</file>")[1];
        }

        Content = $"<file>{filename}</file>{contentFixed}";
    }

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