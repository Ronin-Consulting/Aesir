using System;
using System.Text.Json.Serialization;
using Aesir.Common.Prompts;

namespace Aesir.Client.Models;

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

    public string? GetFileName()
    {
        if (Role != "user") return "";

        if (!HasFile()) return null;
        
        var match = System.Text.RegularExpressions.Regex.Match(
            Content, 
            @"<file>(.*?)</file>", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        return match.Success ? match.Groups[1].Value : null;
    }
    
    public string? GetContentWithoutFileName()
    {
        if (Role != "user") return Content;

        if (!HasFile()) return Content;
        
        const string endTag = "</file>";
        var endIndex = Content.IndexOf(endTag, StringComparison.OrdinalIgnoreCase);
        
        if (endIndex >= 0)
        {
            // Return everything after the closing </file> tag, trimmed
            return Content.Substring(endIndex + endTag.Length).TrimStart();
        }

        return Content;
    }

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