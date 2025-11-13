using Aesir.Client.Models;
using Aesir.Client.Services;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents a view model for system messages in the application.
/// Extends the <see cref="MessageViewModel"/> class to provide functionality specific to system-level messages.
/// </summary>
public class SystemMessageViewModel(
    ApplicationState  applicationState,
    ILogger<SystemMessageViewModel> logger, 
    IMarkdownService markdownService, 
    IKernelLogService kernelLogService
    ) : MessageViewModel(applicationState,logger, markdownService,kernelLogService)
{
    public override string Role => "system";
}