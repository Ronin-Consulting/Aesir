using System.Collections.Generic;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Services;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

public class AssistantMessageViewModel(
    ILogger<AssistantMessageViewModel> logger,
    IMarkdownService markdownService,
    IContentProcessingService contentProcessingService)
    : MessageViewModel(logger, markdownService)
{
    private readonly IContentProcessingService _contentProcessingService = contentProcessingService ?? throw new System.ArgumentNullException(nameof(contentProcessingService));

    public override string Role => "assistant";
    
    protected override ICommand CreateRegenerateMessageCommand()
    {
        return new RelayCommand(RegenerateMessage);
    }

    private void RegenerateMessage()
    {
        WeakReferenceMessenger.Default.Send(new RegenerateMessageMessage(this));
    }
    
    protected override string NormalizeInput(string input)
    {
        return _contentProcessingService.ProcessThinkingModelContent(input);
    }

    public void LinkClicked(string link, Dictionary<string, string> attributes)
    {
        _contentProcessingService.HandleLinkClick(link, attributes);
    }
}