using Aesir.Common.Models;

namespace Aesir.Common.Prompts.PromptCategories;

public static class TitleGenerationPrompts
{
    public static readonly PromptTemplate SystemPrompt = new(@"
Generate a short, descriptive title for a conversation based on the user's message. 
The title should be 3-8 words that capture the main topic or intent. Use only plain text - no bullet points, dashes, or special formatting. 
The title should be suitable for display in a UI as a conversation label.

Input: A user's chat message
Output: A plain text title (3-8 words)

Example:
Input: ""I'm really excited about the new project launch happening next week, it's going to be amazing!""
Output: ""New Project Launch Discussion""");
}