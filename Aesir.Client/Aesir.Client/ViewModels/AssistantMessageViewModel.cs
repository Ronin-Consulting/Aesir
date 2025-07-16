using System;
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

/// Represents the assistant's message in a conversation and extends functionality from the base MessageViewModel class.
/// This class manages operations specific to assistant messages, including handling of commands, processing content,
/// and reacting to user interactions such as link clicks. It utilizes the logging system and services for markdown
/// and content processing during its operations.
public partial class AssistantMessageViewModel(
    ILogger<AssistantMessageViewModel> logger,
    IMarkdownService markdownService,
    IContentProcessingService contentProcessingService)
    : MessageViewModel(logger, markdownService)
{
    /// <summary>
    /// A readonly instance of the <see cref="IContentProcessingService"/> utilized for managing
    /// and processing content-related operations specific to the assistant's message functionality.
    /// </summary>
    /// <remarks>
    /// This service is responsible for tasks such as processing content models and managing
    /// user interactions, including handling link clicks within the assistant's message handling flow.
    /// </remarks>
    private readonly IContentProcessingService _contentProcessingService = contentProcessingService ??
                                                                           throw new System.ArgumentNullException(
                                                                               nameof(contentProcessingService));

    /// <summary>
    /// A private field that holds the raw content of the assistant's internal thoughts
    /// extracted from the corresponding <see cref="AesirChatMessage"/> instance.
    /// </summary>
    /// <remarks>
    /// This property is dynamically updated as the assistant's streamed message content evolves or
    /// whenever a new message is set. The content represents the unprocessed or intermediate state of
    /// the assistant's reasoning or thinking, which may later be rendered as HTML and displayed in the UI.
    /// </remarks>
    private string _thoughtsContent = string.Empty;

    /// <summary>
    /// A private, observable string property used to store the assistant's internal thoughts or reasoning,
    /// associated with the message being processed or displayed in the ViewModel.
    /// </summary>
    /// <remarks>
    /// This property is intended to capture additional contextual information or rationale behind
    /// the assistant's response. It may be updated as part of the message generation process
    /// to provide insights during the interaction.
    /// </remarks>
    [ObservableProperty] 
    private string _thoughtsMessage = string.Empty;

    /// <summary>
    /// A private field indicating whether the assistant is currently engaged in a thinking or processing state.
    /// </summary>
    /// <remarks>
    /// This property reflects the assistant's ongoing computation or decision-making status,
    /// and can be used to update UI elements or trigger behaviors that depend on the assistant's activity state.
    /// </remarks>
    [ObservableProperty] 
    private bool _isThinking;

    [ObservableProperty] 
    private bool _isCollectingThoughts;

    /// <summary>
    /// Represents the role of the message, indicating its origin or purpose within the conversation context.
    /// </summary>
    /// <remarks>
    /// This property is overridden to specify the role as "assistant," identifying messages that originate
    /// from the assistant in the conversation flow.
    /// </remarks>
    public override string Role => "assistant";

    /// Generates the command responsible for initiating the regeneration of a message.
    /// This method provides a tailored implementation specific to the assistant's operation,
    /// ensuring the correct behavior for handling message regeneration scenarios.
    /// <returns>
    /// A command represented by an instance of ICommand that encapsulates the operational
    /// details required to invoke the RegenerateMessage method when executed.
    /// </returns>
    protected override ICommand CreateRegenerateMessageCommand()
    {
        return new RelayCommand(RegenerateMessage);
    }

    /// Regenerates the assistant's message by sending a regeneration request encapsulated
    /// in a RegenerateMessageMessage. The method communicates this instance as the context
    /// for regeneration, ensuring that the assistant's response can be recalculated or updated
    /// effectively. It is designed to handle operations where a new or revised response
    /// is needed for the assistant's previous output.
    private void RegenerateMessage()
    {
        WeakReferenceMessenger.Default.Send(new RegenerateMessageMessage(this));
    }

    /// Handles the event where a link within an assistant message is clicked.
    /// This method utilizes the content processing service to manage the interaction
    /// with the clicked link, ensuring appropriate handling based on the attributes provided.
    /// <param name="link">The URL of the link that was clicked.</param>
    /// <param name="attributes">A dictionary containing additional metadata associated with the link.</param>
    public void LinkClicked(string link, Dictionary<string, string> attributes)
    {
        _contentProcessingService.HandleLinkClick(link, attributes);
    }

    /// Sets the provided message into the view model while updating related properties.
    /// This method processes the input message to determine if its content contains specific
    /// thoughts to be rendered. If so, it uses the Markdown service to convert the content
    /// to HTML for display, and adjusts the thinking state accordingly. It then invokes the base
    /// implementation to complete the process of setting the message.
    /// <param name="message">
    /// An instance of <see cref="AesirChatMessage"/> which represents the message data
    /// to be displayed or processed by the assistant.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation for setting the message.
    /// </returns>
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

    /// Asynchronously processes a streamed message and updates the state of the view model based on the message content.
    /// This method handles the logic for extracting metadata such as the title
    /// and determining specific processing rules based on the streamed message's state.
    /// <param name="message">
    /// An asynchronous stream of <c>AesirChatStreamedResult?</c> instances, representing segments of the streamed message
    /// with associated metadata and status.
    /// </param>
    /// <returns>
    /// A <c>Task</c> representing the asynchronous operation. The task result contains the title string
    /// extracted from the first valid streamed message, if available.
    /// </returns>
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
                    _thoughtsContent += result.Delta.ThoughtsContent;
                    
                    _thoughtsContent = _thoughtsContent.TrimStart();
                    
                    var htmlMessage = await markdownService.RenderMarkdownAsHtmlAsync(_thoughtsContent);
                    
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        IsThinking = isThinking;
                        IsCollectingThoughts = true;
                        
                        ThoughtsMessage = htmlMessage;
                        IsLoaded = true;
                    });
                }
                else
                {
                    Content += result.Delta.Content;
                
                    Content = Content.TrimStart();
                    
                    var htmlMessage = await markdownService.RenderMarkdownAsHtmlAsync(Content);
                
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        IsCollectingThoughts = false;
                        
                        Message = htmlMessage;
                        IsLoaded = true;
                    });
                }
                
                // let ui catch up
                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }
            
            return title;
        });
    }
}