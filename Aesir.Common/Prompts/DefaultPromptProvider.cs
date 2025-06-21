using Aesir.Common.Models;
using Aesir.Common.Prompts.PromptCategories;

namespace Aesir.Common.Prompts;

public class DefaultPromptProvider : IPromptProvider
{
    public PromptTemplate GetSystemPrompt(PromptContext context)
    {
        return context switch
        {
            PromptContext.Business => BusinessPrompts.SystemPrompt,
            PromptContext.Military => MilitaryPrompts.SystemPrompt,
            _ => BusinessPrompts.SystemPrompt
        };
    }

    public PromptTemplate GetTitleGenerationPrompt()
    {
        return TitleGenerationPrompts.SystemPrompt;
    }

    public PromptTemplate GetCondensePrompt()
    {
        return SystemPrompts.CondensePrompt;
    }
}