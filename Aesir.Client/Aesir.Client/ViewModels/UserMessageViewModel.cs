using Aesir.Client.Services;
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
}
