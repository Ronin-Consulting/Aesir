using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Services;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

public class UserMessageViewModel(ILogger<UserMessageViewModel> logger, IMarkdownService markdownService) : MessageViewModel(logger, markdownService)
{
    public override string Role => "user";

    protected override ICommand CreateRegenerateMessageCommand()
    {
        return new RelayCommand(RegenerateMessage);
    }

    private void RegenerateMessage()
    {
        WeakReferenceMessenger.Default.Send(new RegenerateMessageMessage(this));
    }
}