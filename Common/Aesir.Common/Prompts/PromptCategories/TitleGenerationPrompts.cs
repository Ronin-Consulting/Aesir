using Aesir.Common.Models;

namespace Aesir.Common.Prompts.PromptCategories;

public static class TitleGenerationPrompts
{
    public static readonly PromptTemplate SystemPrompt = new(@"
Generate a short, descriptive title for a conversation based on the user's message.

## Requirements
- Length: 3-8 words
- Format: Plain text only (no bullet points, dashes, or special formatting)
- Style: Title Case, suitable for display as a UI conversation label
- Content: Capture the main topic or intent of the message

## Examples

User message: ""I'm really excited about the new project launch happening next week!""
Title: New Project Launch Discussion

User message: ""How do I reset my password?""
Title: Password Reset Help

User message: ""Hi""
Title: Quick Greeting

User message: ""Can you explain the difference between TCP and UDP protocols and when to use each one?""
Title: TCP vs UDP Protocol Comparison

Respond with ONLY the title text—no explanations or formatting.
");
}
