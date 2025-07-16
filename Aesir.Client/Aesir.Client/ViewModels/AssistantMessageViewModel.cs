using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Aesir.Common.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

/// The AssistantMessageViewModel class represents the assistant's message in a conversation.
/// It is responsible for handling the logic specific to assistant messages, including commands,
/// content normalization, and link interactions.
public partial class AssistantMessageViewModel(
    ILogger<AssistantMessageViewModel> logger,
    IMarkdownService markdownService,
    IContentProcessingService contentProcessingService)
    : MessageViewModel(logger, markdownService)
{
    /// <summary>
    /// A readonly instance of the <see cref="IContentProcessingService"/> used for processing and handling
    /// content-related logic within the ViewModel.
    /// </summary>
    /// <remarks>
    /// This service performs operations such as processing content for a thinking model and handling
    /// user interactions, such as link clicks, within the assistant's functionalities.
    /// </remarks>
    private readonly IContentProcessingService _contentProcessingService = contentProcessingService ??
                                                                           throw new System.ArgumentNullException(
                                                                               nameof(contentProcessingService));
    
    private string _thoughtsContent = string.Empty;

    [ObservableProperty] 
    private string _thoughtsMessage = string.Empty;
    
    [ObservableProperty] 
    private bool _isThinking;
    
    /// <summary>
    /// Gets the role associated with the message represented by the view model.
    /// This property defines the type of the message, distinguishing it as belonging
    /// to a specific role in the system, such as "assistant" or "system". The default
    /// role is defined in the base class as "Unknown".
    /// </summary>
    public override string Role => "assistant";
    
    /// Creates the command responsible for triggering the regeneration of a message.
    /// This method overrides the base implementation to provide a specific command
    /// for handling the regenerate message operation, typically tied to an assistant's behavior.
    /// <returns>
    /// An instance of ICommand that encapsulates the logic for regenerating a message.
    /// The returned command will invoke the RegenerateMessage method when executed.
    /// </returns>
    protected override ICommand CreateRegenerateMessageCommand()
    {
        return new RelayCommand(RegenerateMessage);
    }

    /// Regenerates the assistant message by sending a `RegenerateMessageMessage`
    /// containing the current instance of the `AssistantMessageViewModel`.
    /// This method is invoked internally by the `RegenerateMessageCommand`
    /// to handle requests for re-generating or refreshing the assistant's response.
    /// Typically used in cases where the previous message needs to be refreshed
    /// or recalculated.
    private void RegenerateMessage()
    {
        WeakReferenceMessenger.Default.Send(new RegenerateMessageMessage(this));
    }
    
    /// Handles the event of a link being clicked within an assistant message.
    /// <param name="link">The URL of the link that was clicked.</param>
    /// <param name="attributes">A dictionary containing additional attributes associated with the link.</param>
    public void LinkClicked(string link, Dictionary<string, string> attributes)
    {
        _contentProcessingService.HandleLinkClick(link, attributes);
    }
    
    public override async Task SetMessage(AesirChatMessage message)
    {
        _thoughtsContent = message.ThoughtsContent;

        if (!string.IsNullOrWhiteSpace(_thoughtsContent))
        {
            var htmlMessage = await markdownService.RenderMarkdownAsHtmlAsync(NormalizeInput(message.ThoughtsContent));
            ThoughtsMessage = htmlMessage;
        
            IsThinking = true;
        }
        
        await base.SetMessage(message);
    }
    
    public override async Task<string> SetStreamedMessageAsync(IAsyncEnumerable<AesirChatStreamedResult?> message)
    {
        return await Task.Run(async () =>
        {
            var title = string.Empty;
            var hasReceivedTitle = false;
            Content = string.Empty;
            await foreach (var result in message)
            {
                if (result is null)
                {
                    continue;
                }
                
                //_logger.LogDebug("Received streamed message: {Result}", JsonSerializer.Serialize(result));

                // Only capture the first non-empty title we receive
                if (!hasReceivedTitle && !string.IsNullOrWhiteSpace(result.Title) && 
                    result.Title != "Chat Session (Server)" && result.Title != "Chat Session (Client)")
                {
                    title = result.Title;
                    hasReceivedTitle = true;
                }
                
                var isThinking = result.IsThinking;
                
                if (isThinking)
                {
                    IsThinking = isThinking;

                    _thoughtsContent += result.Delta.Content;
                    
                    _thoughtsContent = _thoughtsContent.TrimStart();
                    
                    var htmlMessage = await markdownService.RenderMarkdownAsHtmlAsync(_thoughtsContent);
                    
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ThoughtsMessage = htmlMessage;
                    });
                }
                else
                {
                    Content += result.Delta.Content;
                
                    Content = Content.TrimStart();
                    
                    var htmlMessage = await markdownService.RenderMarkdownAsHtmlAsync(Content);
                
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Message = htmlMessage;
                        IsLoaded = true;
                    });
                }
            }
            
            return title;
        });
    }
}