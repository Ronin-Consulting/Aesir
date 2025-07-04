using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Models;
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

    [ObservableProperty]
    private string? _fileName;
    
    public override string Role => "user";

    protected override ICommand CreateRegenerateMessageCommand()
    {
        return new RelayCommand(RegenerateMessage);
    }
    
    public override async Task SetMessage(AesirChatMessage message)
    {
        if (message.HasFile())
        {
            FileName = message.GetFileName();
        }
        
        Content = message.GetContentWithoutFileName() ?? throw new InvalidOperationException();
        
        var htmlMessage = await markdownService.RenderMarkdownAsHtmlAsync(NormalizeInput(message.Content));
        Message = htmlMessage;
        
        IsLoaded = true;
    }

    public async Task SetMessageAfterEdit(string rawMessage)
    {
        Content = rawMessage;
        
        var htmlMessage = await markdownService.RenderMarkdownAsHtmlAsync(NormalizeInput(rawMessage));
        Message = htmlMessage;
        
        IsLoaded = true;
    }

    public override AesirChatMessage GetAesirChatMessage()
    {
        return AsUserMessage();
    }

    public AesirChatMessage AsUserMessage()
    {
        var content = Content;

        if (FileName != null)
        {
            content = $"<file>{FileName}</file>{Content}";
        }

        return AesirChatMessage.NewUserMessage(content);
    }

    public string ConvertUnorderedListTagsToBulletLists(string html)
    {
        var result = html;
        
        // Process each <ul> block individually
        while (result.Contains("<ul>"))
        {
            var ulStart = result.IndexOf("<ul>");
            var ulEnd = result.IndexOf("</ul>", ulStart);
            
            if (ulStart >= 0 && ulEnd >= 0)
            {
                var beforeUl = result[..ulStart];
                var ulContent = result[(ulStart + 4)..ulEnd];
                var afterUl = result[(ulEnd + 5)..];
                
                // Replace <li> tags with dash prefix within this ul block
                ulContent = ulContent.Replace("<li>", "- ").Replace("</li>", "");
                
                result = beforeUl + ulContent + afterUl;
            }
            else
            {
                break;
            }
        }
        result = result.Replace("<ul>", "").Replace("</ul>", "");
        
        return result;
    }
    
    public string ConvertOrderedListTagsToNumberedLists(string html)
    {
        var result = html;
        
        // Process each <ol> block individually
        while (result.Contains("<ol>"))
        {
            var olStart = result.IndexOf("<ol>");
            var olEnd = result.IndexOf("</ol>", olStart);
            
            if (olStart >= 0 && olEnd >= 0)
            {
                var beforeOl = result[..olStart];
                var olContent = result[(olStart + 4)..olEnd];
                var afterOl = result[(olEnd + 5)..];
                
                // Replace <li> tags with numbered prefix within this ol block
                var listNumber = 1;
                while (olContent.Contains("<li>"))
                {
                    var liIndex = olContent.IndexOf("<li>");
                    if (liIndex >= 0)
                    {
                        olContent = olContent[..liIndex] + $"{listNumber}. " + olContent[(liIndex + 4)..];
                    }
                    var closeLiIndex = olContent.IndexOf("</li>");
                    if (closeLiIndex >= 0)
                    {
                        olContent = olContent[..closeLiIndex] + olContent[(closeLiIndex + 5)..];
                    }
                    listNumber++;
                }
                
                result = beforeOl + olContent + afterOl;
            }
            else
            {
                break;
            }
        }
        result = result.Replace("<ol>", "").Replace("</ol>", "");
        
        return result;
    }

    public string ConvertFromHtml(string html)
    {
        // Can we go from html back to markdown instead...
        
        var result = html;
        
        // Convert HTML lists to plain text format
        result = ConvertUnorderedListTagsToBulletLists(result);
        result = ConvertOrderedListTagsToNumberedLists(result);
        
        // Remove paragraph tags
        result = result.Replace("<p>", "").Replace("</p>", "");
        
        return result.TrimEnd('\n');
    }
    
    private void RegenerateMessage()
    {
        WeakReferenceMessenger.Default.Send(new RegenerateMessageMessage(this));
    }
}
