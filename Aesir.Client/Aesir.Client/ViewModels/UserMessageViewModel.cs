using Aesir.Client.Services;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

public class UserMessageViewModel(ILogger<UserMessageViewModel> logger, IMarkdownService markdownService) : MessageViewModel(logger, markdownService)
{
    public override string Role => "user";
}