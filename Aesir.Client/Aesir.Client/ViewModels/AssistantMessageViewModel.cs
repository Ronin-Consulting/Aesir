using Aesir.Client.Services;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

public class AssistantMessageViewModel(ILogger<AssistantMessageViewModel> logger, IMarkdownService markdownService) : MessageViewModel(logger, markdownService)
{
    public override string Role => "assistant";
    
    protected override string NormalizeInput(string input)
    {
        return input;
    }
}