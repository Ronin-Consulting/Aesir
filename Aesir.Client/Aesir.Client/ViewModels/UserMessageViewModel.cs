using System;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Services;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

public partial class UserMessageViewModel(ILogger<UserMessageViewModel> logger, IMarkdownService markdownService) : MessageViewModel(logger, markdownService)
{
    [ObservableProperty]
    private bool _isEditing = false;
    
    [ObservableProperty]
    private string _rawMessage = string.Empty;


    public override string Role => "user";

    protected override ICommand CreateRegenerateMessageCommand()
    {
        return new RelayCommand(RegenerateMessage);
    }

    public string ConvertFromHtml(string html)
    {
        return html.Replace("<p>", "").Replace("</p>", "").TrimEnd('\n');
    }

    public string ConvertToHtml(string rawMessage)
    {
        return $"<p>{rawMessage}</p>";
    }

    private void RegenerateMessage()
    {
        WeakReferenceMessenger.Default.Send(new RegenerateMessageMessage(this));
    }
}
