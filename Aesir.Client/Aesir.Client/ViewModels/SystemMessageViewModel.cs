using Aesir.Client.Services;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

public class SystemMessageViewModel(ILogger<SystemMessageViewModel> logger, IMarkdownService markdownService) : MessageViewModel(logger, markdownService)
{
    public override string Role => "system";
}