using Aesir.Common.Models;

namespace Aesir.Common.Prompts;

public interface IPromptProvider
{
    PromptTemplate GetSystemPrompt(PromptContext context);
    PromptTemplate GetTitleGenerationPrompt();
    PromptTemplate GetCondensePrompt();
}

public enum PromptContext
{
    Business,
    Military
}