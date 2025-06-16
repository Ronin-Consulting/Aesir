using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

public abstract partial class MessageViewModel : ObservableRecipient
{
    private readonly ILogger _logger;
    private readonly IMarkdownService _markdownService;
    
    [ObservableProperty] 
    private string _message = string.Empty;

    [ObservableProperty] 
    private bool _isLoaded;
    
    public virtual string Role => "Unknown";

    public string Content { get; set; } = string.Empty;
    
    public Guid Id { get; set; } = Guid.NewGuid();

    public ICommand RegenerateMessageCommand { get; }

    protected MessageViewModel(ILogger logger, IMarkdownService markdownService)
    {
        _logger = logger;
        _markdownService = markdownService;
        
        RegenerateMessageCommand = CreateRegenerateMessageCommand();
    }

    protected virtual ICommand CreateRegenerateMessageCommand()
    {
        return new RelayCommand(() => { }); // Default no-op implementation
    }
    
    public void SetMessage(AesirChatMessage message)
    {
        Content = message.Content;
        
        var htmlMessage = _markdownService.RenderMarkdownAsHtmlAsync(message.Content).Result;
        Message = htmlMessage;
        
        IsLoaded = true;
    }
    
    public Task<string> SetStreamedMessageAsync(IAsyncEnumerable<AesirChatStreamedResult?> message)
    {
        return Task.Run(async () =>
        {
            var title = string.Empty;
            Content = string.Empty;
            await foreach (var result in message)
            {
                if (result is null)
                {
                    continue;
                }
                
                //_logger.LogDebug("Received streamed message: {Result}", JsonSerializer.Serialize(result));

                title = result.Title;
                
                Content += result.Delta.Content;
                
                Content = Content.TrimStart();
                
                var htmlMessage = await _markdownService.RenderMarkdownAsHtmlAsync(Content);
                
                Dispatcher.UIThread.Invoke(() =>
                {
                    Message = htmlMessage;
                    IsLoaded = true;
                });
            }
            
            return title;
        });
    }
    
    protected virtual string NormalizeInput(string input)
    {
        return input.Replace("\n", "<br>");
    }
    
    public AesirChatMessage GetAesirChatMessage()
    {
        return new AesirChatMessage()
        {
            Role = Role,
            Content = Content
        };
    }
}