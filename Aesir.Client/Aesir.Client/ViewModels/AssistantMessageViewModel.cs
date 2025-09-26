using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Aesir.Common;
using Aesir.Common.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

/// Represents a specialized message model for the assistant in a conversation, inheriting from MessageViewModel.
/// This class focuses on assistant-specific tasks such as processing responses, handling streaming data, and reacting
/// to user-initiated events like link interactions. It integrates services for logging, markdown rendering, speech
/// synthesis, and content processing to provide a dynamic and feature-rich experience tailored for assistant interactions.
public partial class AssistantMessageViewModel(
    ILogger<AssistantMessageViewModel> logger,
    IMarkdownService markdownService,
    IContentProcessingService contentProcessingService,
    ISpeechService speechService,
    IKernelLogService kernelLogService)
    : MessageViewModel(logger, markdownService, kernelLogService)
{
    /// <summary>
    /// A readonly instance of the <see cref="IContentProcessingService"/> used for handling
    /// content-related processing functionalities specific to the assistant message view model.
    /// </summary>
    /// <remarks>
    /// This member is responsible for managing operations such as behavior processing
    /// tied to content interactions, including event handling for user actions like link clicks.
    /// </remarks>
    private readonly IContentProcessingService _contentProcessingService = contentProcessingService ??
                                                                           throw new System.ArgumentNullException(
                                                                               nameof(contentProcessingService));

    /// <summary>
    /// A private field that holds raw content representing the assistant's internal thoughts,
    /// extracted from incoming messages such as <see cref="AesirChatMessage"/>.
    /// </summary>
    /// <remarks>
    /// This field is dynamically updated either during the assignment of new message content
    /// or while streaming partial content updates. It represents the assistant's reasoning
    /// in a pre-rendered format prior to being processed into a user-facing view.
    /// </remarks>
    private string _thoughtsContent = string.Empty;

    /// <summary>
    /// A private observable string property storing the assistant's internal
    /// thoughts or reasoning associated with the current message being displayed
    /// or processed within the assistant message view model.
    /// </summary>
    /// <remarks>
    /// This property helps provide contextual insights or supplementary information
    /// about the assistant's reasoning processes. It enables better understanding
    /// of the assistant's decision-making or response-generation behavior during
    /// an interaction.
    /// </remarks>
    [ObservableProperty] private string _thoughtsMessage = string.Empty;

    /// <summary>
    /// A private field representing the state of whether the assistant is currently in a thinking or processing mode.
    /// </summary>
    /// <remarks>
    /// Used to signal the assistant's computation status, this field allows for dynamic adjustments
    /// in the user interface or other dependent functionalities based on the assistant's active state.
    /// </remarks>
    [ObservableProperty] private bool _isThinking;

    /// <summary>
    /// A private, observable field indicating whether the assistant is actively engaged in
    /// the process of formulating or generating its response during an interaction cycle.
    /// </summary>
    /// <remarks>
    /// This field reflects the assistant's state of thought collection, which can be used to
    /// update UI elements or control interaction flow based on the assistant's activity status.
    /// </remarks>
    [ObservableProperty] private bool _isCollectingThoughts;

    /// <summary>
    /// Represents the role of the message in the context of the assistant functionality.
    /// </summary>
    /// <remarks>
    /// This property is a string that identifies the message's role as "assistant,"
    /// distinguishing it from other potential roles within the system.
    /// </remarks>
    public override string Role => "assistant";

    /// Creates a command responsible for initiating the regeneration of a message.
    /// This method defines the mechanism required to enable a message regeneration
    /// process within the assistant's context.
    /// <returns>
    /// An ICommand instance that encapsulates the execution logic for regenerating a
    /// message when executed.
    /// </returns>
    protected override ICommand CreateRegenerateMessageCommand()
    {
        return new RelayCommand(RegenerateMessage);
    }

    /// Creates a command responsible for triggering the playback of a message.
    /// This method enables the functionality to play the content of the message
    /// using the underlying speech or audio processing services.
    /// <returns>
    /// An ICommand instance that executes the logic required to play the
    /// message content when invoked.
    /// </returns>
    protected override ICommand CreatePlayMessageCommand()
    {
        return new AsyncRelayCommand(PlayMessageAsync);
    }

    /// Plays the current assistant message as speech using the speech service.
    /// This method converts the message content from markdown to plain text
    /// before passing it to the speech service for playback.
    /// <returns>
    /// A Task that represents the asynchronous operation of rendering the
    /// message and initiating the speech playback.
    /// </returns>
    private async Task PlayMessageAsync()
    {
        var plainText = await markdownService.RenderMarkdownAsPlainTextAsync(Content);
        await speechService.SpeakAsync(plainText);
    }

    /// Triggers the regeneration process for the assistant's message by sending a
    /// `RegenerateMessageMessage` associated with the current message instance through
    /// the messaging framework. This method allows for updating or recalculating the
    /// assistant's response dynamically within the conversation context.
    private void RegenerateMessage()
    {
        WeakReferenceMessenger.Default.Send(new RegenerateMessageMessage(this));
    }

    /// Handles the event triggered when a hyperlink within the assistant's message is clicked.
    /// This method processes the specified link and additional attributes using the content processing service.
    /// <param name="link">The URL of the clicked hyperlink to be processed.</param>
    /// <param name="attributes">A dictionary containing metadata associated with the hyperlink, represented as key-value pairs.</param>
    public void LinkClicked(string link, Dictionary<string, string> attributes)
    {
        _contentProcessingService.HandleLinkClick(link, attributes);
    }

    /// Updates the view model with the specified message, applying necessary formatting,
    /// updating internal states, and invoking any relevant processing logic. This method
    /// also ensures the base implementation is executed as part of the operation.
    /// <param name="message">
    /// An instance of <see cref="AesirChatMessage"/> containing the information to be
    /// processed and displayed within the view model.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation of updating the view model
    /// and applying the relevant message-processing workflow.
    /// </returns>
    public override async Task SetMessage(AesirChatMessage message)
    {
        _thoughtsContent = message.ThoughtsContent?.TrimStart().NormalizeLineEndings() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(_thoughtsContent))
        {
            // Move markdown rendering to background thread to avoid UI blocking
            var htmlMessage = await Task.Run(() => markdownService.RenderMarkdownAsHtmlAsync(_thoughtsContent));
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ThoughtsMessage = htmlMessage;
                IsThinking = true;
            });
        }

        await base.SetMessage(message);
    }

    /// Asynchronously processes and sets a streamed message in the view model state.
    /// This method processes incoming message chunks and updates the view with relevant
    /// information such as deriving a title from the message data.
    /// <param name="message">
    /// An asynchronous stream of <c>AesirChatStreamedResult?</c> objects representing
    /// streamed message segments and associated metadata.
    /// </param>
    /// <returns>
    /// A <c>Task</c> representing the asynchronous operation. The result is a string
    /// containing the derived title from the relevant streamed message chunks, if found.
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

                    _thoughtsContent = _thoughtsContent.TrimStart().NormalizeLineEndings();

                    // Render markdown on background thread to avoid UI blocking
                    var htmlMessage = await Task.Run(() => markdownService.RenderMarkdownAsHtmlAsync(_thoughtsContent));

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

                    Content = Content.TrimStart().NormalizeLineEndings();

                    // Render markdown on background thread to avoid UI blocking
                    var htmlMessage = await Task.Run(() => markdownService.RenderMarkdownAsHtmlAsync(Content));

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        IsCollectingThoughts = false;

                        Message = htmlMessage;
                        IsLoaded = true;
                    });
                }

                // Brief yield to allow UI updates without fixed delays
                await Task.Yield();
            }

            // Final render with syntax highlighting on background thread
            if(!string.IsNullOrWhiteSpace(Content))
            {
                var finalMessage = await Task.Run(() => markdownService.RenderMarkdownAsHtmlAsync(Content, shouldRenderFencedCodeBlocks: true));
                await Dispatcher.UIThread.InvokeAsync(() => Message = finalMessage);
            }
            
            return title;
        });
    }
}