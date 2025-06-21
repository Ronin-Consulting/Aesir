using Aesir.Common.Models;

namespace Aesir.Common.Prompts;

public static class SystemPrompts
{
    public static readonly PromptTemplate CondensePrompt = new(@"
Given the following conversation between a user and an AI assistant and a follow up question from user,
rephrase the follow up question to be a standalone question.

Chat History:
{chat_history}
Follow Up Input: {question}
Standalone question:");
}