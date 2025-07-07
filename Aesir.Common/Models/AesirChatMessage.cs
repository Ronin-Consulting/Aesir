using System.Text.Json.Serialization;
using Aesir.Common.Prompts;

namespace Aesir.Common.Models;

/// <summary>
/// Represents a chat message within the Aesir system.
/// </summary>
public class AesirChatMessage : IEquatable<AesirChatMessage>
{
    /// <summary>
    /// Gets or sets the role of the participant in the chat message (e.g., "user", "assistant", "system").
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = null!;

    /// <summary>
    /// Gets or sets the content of the message. This may include plain text, formatted content, or embedded file references.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;

    /// <summary>
    /// Determines if the message content contains a file reference.
    /// </summary>
    /// <returns>True if the content includes a file reference; otherwise, false.</returns>
    public bool HasFile()
    {
        if (Role != "user") return false;
        
        // Use regex to detect <file>...</file> pattern anywhere in the content
        return System.Text.RegularExpressions.Regex.IsMatch(
            Content, 
            @"<file>.*?</file>", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    }

    /// <summary>
    /// Adds or updates a file reference in the content of the message.
    /// </summary>
    /// <param name="filename">The name of the file to be added or updated in the content.</param>
    public void AddFile(string filename)
    {
        if (Role != "user") return;
        
        // Try to replace existing <file>...</file> tag with new filename
        var originalContent = Content;
        Content = System.Text.RegularExpressions.Regex.Replace(
            Content, 
            @"<file>.*?</file>", 
            $"<file>{filename}</file>", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    
        // If no replacement was made (no existing file tag), add the file tag
        if (Content == originalContent)
        {
            Content = $"<file>{filename}</file>{Content}";
        }
    }

    /// <summary>
    /// Extracts the file name from the message content if a file tag is present.
    /// </summary>
    /// <returns>The extracted file name if a file tag exists; otherwise, null.</returns>
    public string? GetFileName()
    {
        if (Role != "user") return null;

        if (!HasFile()) return null;
        
        var match = System.Text.RegularExpressions.Regex.Match(
            Content, 
            @"<file>(.*?)</file>", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Gets the message content with file references removed and formats it for display.
    /// </summary>
    /// <returns>The message content with all file references stripped, formatted for display.</returns>
    public string? GetContentWithoutFileName()
    {
        if (Role != "user" || !HasFile()) return Content;

        // Use regex to remove all <file>...</file> tags
        var result = System.Text.RegularExpressions.Regex.Replace(
            Content, 
            @"<file>.*?</file>", 
            string.Empty, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    
        // Clean up any extra whitespace that might be left
        return result.Trim();
    }

    /// <summary>
    /// Gets the message content with file references included and formatted for display.
    /// </summary>
    /// <returns>The message content with file references included and formatted for display.</returns>
    public string? GetContentWithFileName()
    {
        if (Role != "user" || !HasFile()) return Content;

        // Use regex to remove all <file>...</file> tags
        var result = System.Text.RegularExpressions.Regex.Replace(
            Content, 
            @"<file>.*?</file>", 
            string.Empty, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    
        result = result.Trim();

        result = $"The file is: {GetFileName()}\n{result}";
        
        // Clean up any extra whitespace that might be left
        return result.Trim();
    }

    /// <summary>
    /// Normalizes the content string by trimming whitespace, removing outer HTML tags,
    /// and processing any additional formatting to simplify the content structure.
    /// </summary>
    /// <param name="content">The original content string to be normalized.</param>
    /// <returns>The normalized content string, with unnecessary outer HTML tags removed and trimmed whitespace.</returns>
    private static string NormalizeContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        var trimmedContent = content.Trim();
    
        // Check if the content is wrapped in HTML tags
        if (trimmedContent.StartsWith("<") && trimmedContent.EndsWith(">"))
        {
            // Find the first opening tag
            var firstTagEnd = trimmedContent.IndexOf('>');
            if (firstTagEnd > 0)
            {
                var openingTag = trimmedContent.Substring(0, firstTagEnd + 1);
            
                // Extract tag name (e.g., "p" from "<p>" or "<p class='test'>")
                var tagNameMatch = System.Text.RegularExpressions.Regex.Match(openingTag, @"<(\w+)");
                if (tagNameMatch.Success)
                {
                    var tagName = tagNameMatch.Groups[1].Value;
                    var closingTag = $"</{tagName}>";
                
                    // Check if content ends with the corresponding closing tag
                    if (trimmedContent.EndsWith(closingTag, StringComparison.OrdinalIgnoreCase))
                    {
                        // Remove the outermost container tags
                        var innerContent = trimmedContent.Substring(firstTagEnd + 1, 
                            trimmedContent.Length - firstTagEnd - 1 - closingTag.Length);
                    
                        // Recursively check for more outer containers
                        return NormalizeContent(innerContent);
                    }
                }
            }
        }
    
        // If no outer container found, just replace newlines
        return trimmedContent;
    }

    /// <summary>
    /// Creates a new system message with the specified content and context.
    /// </summary>
    /// <param name="content">The optional content of the system message. If null, a default system message will be used based on the specified context.</param>
    /// <param name="context">The context of the system message used to determine the default content when no content is provided.</param>
    /// <returns>A new instance of <see cref="AesirChatMessage"/> configured as a system message.</returns>
    public static AesirChatMessage NewSystemMessage(string? content = null, PromptContext context = PromptContext.Business)
    {
        var promptProvider = new DefaultPromptProvider();
        var defaultSystemContent = promptProvider.GetSystemPrompt(context).Content;
        
        return new AesirChatMessage()
        {
            Role = "system",
            Content = content ?? defaultSystemContent
        };
    }

    /// <summary>
    /// Creates a new message with the role of 'assistant' and assigns the specified content to it.
    /// </summary>
    /// <param name="content">The content of the assistant's message.</param>
    /// <returns>A new instance of <see cref="AesirChatMessage"/> representing an assistant message with the specified content.</returns>
    public static AesirChatMessage NewAssistantMessage(string content)
    {
        return new AesirChatMessage()
        {
            Role = "assistant",
            Content = content
        };
    }

    /// <summary>
    /// Creates a new user message with the specified content.
    /// </summary>
    /// <param name="content">The content of the user message.</param>
    /// <returns>A new instance of <see cref="AesirChatMessage"/> representing a user message.</returns>
    public static AesirChatMessage NewUserMessage(string content)
    {
        return new AesirChatMessage()
        {
            Role = "user",
            Content = NormalizeContent(content)
        };
    }

    /// <summary>
    /// Determines whether the specified AesirChatMessage is equal to the current instance.
    /// </summary>
    /// <param name="other">The AesirChatMessage to compare with the current instance.</param>
    /// <returns><c>true</c> if the specified message is equal to the current instance; otherwise, <c>false</c>.</returns>
    public bool Equals(AesirChatMessage? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Role == other.Role && Content == other.Content;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>True if the specified object is equal to the current instance; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((AesirChatMessage)obj);
    }

    /// <summary>
    /// Serves as the default hash function for the AesirChatMessage class by combining its Role and Content properties.
    /// </summary>
    /// <returns>A hash code for the current AesirChatMessage instance.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Role, Content);
    }
}