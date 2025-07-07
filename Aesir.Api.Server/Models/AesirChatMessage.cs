using System.Text.Json.Serialization;

namespace Aesir.Api.Server.Models;

/// <summary>
/// Represents a single chat message with role-based content and file attachment support.
/// </summary>
public class AesirChatMessage : IEquatable<AesirChatMessage>
{
    /// <summary>
    /// Gets or sets the role of the message sender (e.g., "user", "assistant", "system").
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = null!;

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;

    /// <summary>
    /// Determines whether the message contains a file attachment reference.
    /// </summary>
    /// <returns>True if the message contains a file reference; otherwise, false.</returns>
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
    /// Adds or replaces a file reference in the message content.
    /// </summary>
    /// <param name="filename">The filename to add to the message.</param>
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
    /// Extracts the filename from the message content if present.
    /// </summary>
    /// <returns>The filename if found; otherwise, null.</returns>
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
    /// Gets the message content with file references removed.
    /// </summary>
    /// <returns>The message content without file references.</returns>
    public string? GetContentWithoutFileName()
    {
        if (Role != "user") return Content;

        if (!HasFile()) return Content;
        
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
    /// Gets the message content with file references formatted for display.
    /// </summary>
    /// <returns>The message content with file information formatted for display.</returns>
    public string? GetContentWithFileName()
    {
        if (Role != "user") return Content;

        if (!HasFile()) return Content;
        
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
    /// Normalizes message content by removing outer HTML container tags.
    /// </summary>
    /// <param name="content">The content to normalize.</param>
    /// <returns>The normalized content.</returns>
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
    /// Creates a new system message with the specified content.
    /// </summary>
    /// <param name="content">The message content.</param>
    /// <returns>A new system message.</returns>
    public static AesirChatMessage NewSystemMessage(string content)
    {
        return new AesirChatMessage()
        {
            Role = "system",
            Content = content
        };
    }

    /// <summary>
    /// Creates a new assistant message with the specified content.
    /// </summary>
    /// <param name="content">The message content.</param>
    /// <returns>A new assistant message.</returns>
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
    /// <param name="content">The message content.</param>
    /// <returns>A new user message with normalized content.</returns>
    public static AesirChatMessage NewUserMessage(string content)
    {
        return new AesirChatMessage()
        {
            Role = "user",
            Content = NormalizeContent(content)
        };
    }

    public bool Equals(AesirChatMessage? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Role == other.Role && Content == other.Content;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((AesirChatMessage)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Role, Content);
    }
}